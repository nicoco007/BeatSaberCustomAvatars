using System;
using System.Collections;
using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using UnityEngine;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar
{
    internal class AvatarTailor
    {
        public const float kDefaultPlayerArmSpan = 1.7f;

        private readonly ILogger _logger;
        private readonly MainSettingsModelSO _mainSettingsModel;
        private readonly PlayerDataModel _playerDataModel;
        private readonly Settings _settings;

        private Vector3? _initialPlatformPosition;

        private Vector3 _roomCenter => _mainSettingsModel.roomCenter.value;
        private float _playerEyeHeight => _playerDataModel.playerData.playerSpecificSettings.playerHeight - MainSettingsModelSO.kHeadPosToPlayerHeightOffset;

        private AvatarTailor(ILoggerProvider loggerProvider, MainSettingsModelSO mainSettingsModel, PlayerDataModel playerDataModel, Settings settings)
        {
            _logger = loggerProvider.CreateLogger<AvatarTailor>();
            _mainSettingsModel = mainSettingsModel;
            _playerDataModel = playerDataModel;
            _settings = settings;
        }

        public void ResizeAvatar(SpawnedAvatar avatar)
        {
            if (!avatar.avatar.descriptor.allowHeightCalibration || !avatar.isIKAvatar) return;

            // compute scale
            float scale;
            AvatarResizeMode resizeMode = _settings.resizeMode;

            switch (resizeMode)
            {
                case AvatarResizeMode.ArmSpan:
                    float avatarArmLength = avatar.armSpan;

                    if (avatarArmLength > 0)
                    {
                        scale = _settings.playerArmSpan / avatarArmLength;
                    }
                    else
                    {
                        scale = 1.0f;
                    }

                    break;

                case AvatarResizeMode.Height:
                    float avatarEyeHeight = avatar.eyeHeight;

                    if (avatarEyeHeight > 0)
                    {
                        scale = _playerEyeHeight / avatarEyeHeight;
                    }
                    else
                    {
                        scale = 1.0f;
                    }

                    break;

                default:
                    scale = 1.0f;
                    break;
            }

            if (scale <= 0)
            {
                _logger.Warning("Calculated scale is <= 0; reverting to 1");
                scale = 1.0f;
            }

            // apply scale
            avatar.scale = scale;

            SharedCoroutineStarter.instance.StartCoroutine(FloorMendingWithDelay(avatar));
        }

        private IEnumerator FloorMendingWithDelay(SpawnedAvatar avatar)
        {
            yield return new WaitForEndOfFrame(); // wait for CustomFloorPlugin:PlatformManager:Start to hide original platform

            if (!avatar) yield break;

            float floorOffset = 0f;

            if (_settings.enableFloorAdjust && avatar.isIKAvatar)
            {
                float playerEyeHeight = _playerEyeHeight;
                float avatarEyeHeight = avatar.eyeHeight;

                floorOffset = playerEyeHeight - (avatarEyeHeight * avatar.scale);

                if (_settings.moveFloorWithRoomAdjust)
                {
                    floorOffset += _roomCenter.y;
                }
            }

            floorOffset = (float) Math.Round(floorOffset, 3); // round to millimeter

            // apply offset
            avatar.verticalPosition = floorOffset;

            // ReSharper disable Unity.PerformanceCriticalCodeInvocation
            GameObject menuPlayersPlace = GameObject.Find("MenuPlayersPlace");
            GameObject originalFloor = GameObject.Find("Environment/PlayersPlace");
            GameObject customFloor = GameObject.Find("Platform Loader");
            // ReSharper disable restore Unity.PerformanceCriticalCodeInvocation

            if (menuPlayersPlace)
            {
                _logger.Info($"Moving MenuPlayersPlace floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");
                menuPlayersPlace.transform.position = new Vector3(0, floorOffset, 0);
            }

            if (originalFloor)
            {
                _logger.Info($"Moving PlayersPlace {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");
                originalFloor.transform.position = new Vector3(0, floorOffset, 0);
            }

            if (customFloor)
            {
                _logger.Info($"Moving Custom Platforms floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");

                _initialPlatformPosition = _initialPlatformPosition ?? customFloor.transform.position;
                customFloor.transform.position = (Vector3.up * floorOffset) + _initialPlatformPosition ?? Vector3.zero;
            }
        }

        public void CalibrateFullBodyTrackingManual(SpawnedAvatar spawnedAvatar)
        {
            Settings.ManualFullBodyCalibration fullBodyCalibration = _settings.GetAvatarSettings(spawnedAvatar.avatar.fullPath).fullBodyCalibration;

            if (spawnedAvatar.input.TryGetWaistPose(out Pose pelvis))
            {
                Vector3 positionOffset = Quaternion.Inverse(spawnedAvatar.tracking.pelvis.rotation) * (spawnedAvatar.tracking.pelvis.position - pelvis.position);
                Quaternion rotationOffset = Quaternion.Inverse(pelvis.rotation) * spawnedAvatar.tracking.pelvis.rotation;

                fullBodyCalibration.pelvis = new Pose(positionOffset, rotationOffset);
                _logger.Info("Saved pelvis pose correction " + fullBodyCalibration.pelvis);
            }

            if (spawnedAvatar.input.TryGetLeftFootPose(out Pose leftFoot))
            {
                Vector3 positionOffset = Quaternion.Inverse(spawnedAvatar.tracking.leftLeg.rotation) * (spawnedAvatar.tracking.leftLeg.position - leftFoot.position);
                Quaternion rotationOffset = Quaternion.Inverse(leftFoot.rotation) * spawnedAvatar.tracking.leftLeg.rotation;

                fullBodyCalibration.leftLeg = new Pose(positionOffset, rotationOffset);
                _logger.Info("Saved left foot pose correction " + fullBodyCalibration.leftLeg);
            }

            if (spawnedAvatar.input.TryGetRightFootPose(out Pose rightFoot))
            {
                Vector3 positionOffset = Quaternion.Inverse(spawnedAvatar.tracking.rightLeg.rotation) * (spawnedAvatar.tracking.rightLeg.position - rightFoot.position);
                Quaternion rotationOffset = Quaternion.Inverse(rightFoot.rotation) * spawnedAvatar.tracking.rightLeg.rotation;

                fullBodyCalibration.rightLeg = new Pose(positionOffset, rotationOffset);
                _logger.Info("Saved right foot pose correction " + fullBodyCalibration.rightLeg);
            }
        }

        public void CalibrateFullBodyTrackingAuto(SpawnedAvatar spawnedAvatar)
        {
            _logger.Info("Calibrating full body tracking");

            Settings.AutomaticFullBodyCalibration fullBodyCalibration = _settings.automaticCalibration;

            Vector3 floorNormal = Vector3.up;
            float floorPosition = _settings.moveFloorWithRoomAdjust ? _roomCenter.y : 0;

            if (spawnedAvatar.input.TryGetLeftFootPose(out Pose leftFoot))
            {
                Vector3 leftFootForward = leftFoot.rotation * Vector3.up; // forward on feet trackers is y (up)
                Vector3 leftFootStraightForward = Vector3.ProjectOnPlane(leftFootForward, floorNormal); // get projection of forward vector on xz plane (floor)
                Quaternion leftRotationCorrection = Quaternion.Inverse(leftFoot.rotation) * Quaternion.LookRotation(Vector3.up, leftFootStraightForward); // get difference between world rotation and flat forward rotation
                fullBodyCalibration.leftLeg = new Pose((leftFoot.position.y - floorPosition) * Vector3.back, leftRotationCorrection);
                _logger.Info("Saved left foot pose correction " + fullBodyCalibration.leftLeg);
            }

            if (spawnedAvatar.input.TryGetRightFootPose(out Pose rightFoot))
            {
                Vector3 rightFootForward = rightFoot.rotation * Vector3.up;
                Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, floorNormal);
                Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
                fullBodyCalibration.rightLeg = new Pose((rightFoot.position.y - floorPosition) * Vector3.back, rightRotationCorrection);
                _logger.Info("Saved right foot pose correction " + fullBodyCalibration.rightLeg);
            }

            if (spawnedAvatar.input.TryGetHeadPose(out Pose head) && spawnedAvatar.input.TryGetWaistPose(out Pose pelvis))
            {
                // using "standard" 8 head high body proportions w/ eyes at 1/2 head height
                // reference: https://miro.medium.com/max/3200/1*cqTRyEGl26l4CImEmWz68Q.jpeg
                float eyeHeight = head.position.y - floorPosition;

                Vector3 wantedPelvisPosition = new Vector3(0, eyeHeight / 22.5f * 14f, 0);
                Vector3 pelvisPositionCorrection = wantedPelvisPosition - (Vector3.up * (pelvis.position.y - floorPosition));

                Vector3 pelvisForward = pelvis.rotation * Vector3.forward;
                Vector3 pelvisStraightForward = Vector3.ProjectOnPlane(pelvisForward, floorNormal);
                Quaternion pelvisRotationCorrection = Quaternion.Inverse(pelvis.rotation) * Quaternion.LookRotation(pelvisStraightForward, Vector3.up);

                fullBodyCalibration.pelvis = new Pose(pelvisPositionCorrection, pelvisRotationCorrection);
                _logger.Info("Saved pelvis pose correction " + fullBodyCalibration.pelvis);
            }
        }

        public void ClearManualFullBodyTrackingData(SpawnedAvatar spawnedAvatar)
        {
            Settings.ManualFullBodyCalibration fullBodyCalibration = _settings.GetAvatarSettings(spawnedAvatar.avatar.fullPath).fullBodyCalibration;

            fullBodyCalibration.leftLeg = Pose.identity;
            fullBodyCalibration.rightLeg = Pose.identity;
            fullBodyCalibration.pelvis = Pose.identity;
        }

        public void ClearAutomaticFullBodyTrackingData()
        {
            Settings.AutomaticFullBodyCalibration fullBodyCalibration = _settings.automaticCalibration;

            fullBodyCalibration.leftLeg = Pose.identity;
            fullBodyCalibration.rightLeg = Pose.identity;
            fullBodyCalibration.pelvis = Pose.identity;
        }
    }
}
