using System;
using System.Collections;
using CustomAvatar.Avatar;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar
{
    internal class AvatarTailor
    {
        public const float kDefaultPlayerArmSpan = 1.7f;

        private Vector3? _initialPlatformPosition;

        public void ResizeAvatar(SpawnedAvatar avatar)
        {
            if (!avatar.avatar.descriptor.allowHeightCalibration || !avatar.avatar.isIKAvatar) return;

            // compute scale
            float scale;
            AvatarResizeMode resizeMode = SettingsManager.settings.resizeMode;

            switch (resizeMode)
            {
                case AvatarResizeMode.ArmSpan:
                    float avatarArmLength = avatar.avatar.armSpan;

                    if (avatarArmLength > 0)
                    {
                        scale = SettingsManager.settings.playerArmSpan / avatarArmLength;
                    }
                    else
                    {
                        scale = 1.0f;
                    }

                    break;

                case AvatarResizeMode.Height:
                    float avatarEyeHeight = avatar.avatar.eyeHeight;

                    if (avatarEyeHeight > 0)
                    {
                        scale = BeatSaberUtil.GetPlayerEyeHeight() / avatarEyeHeight;
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
                Plugin.logger.Warn("Calculated scale is <= 0; reverting to 1");
                scale = 1.0f;
            }

            // apply scale
            avatar.tracking.scale = scale;

            SharedCoroutineStarter.instance.StartCoroutine(FloorMendingWithDelay(avatar));
        }

        private IEnumerator FloorMendingWithDelay(SpawnedAvatar avatar)
        {
            yield return new WaitForEndOfFrame(); // wait for CustomFloorPlugin:PlatformManager:Start to hide original platform

            float floorOffset = 0f;

            if (SettingsManager.settings.enableFloorAdjust && avatar.avatar.isIKAvatar)
            {
                float playerEyeHeight = BeatSaberUtil.GetPlayerEyeHeight();
                float avatarEyeHeight = avatar.avatar.eyeHeight;

                floorOffset = playerEyeHeight - avatarEyeHeight * avatar.tracking.scale;

                if (SettingsManager.settings.moveFloorWithRoomAdjust)
                {
                    floorOffset += BeatSaberUtil.GetRoomCenter().y;
                }
            }

            floorOffset = (float) Math.Round(floorOffset, 3); // round to millimeter

            // apply offset
			avatar.tracking.verticalPosition = floorOffset;
            
            // ReSharper disable Unity.PerformanceCriticalCodeInvocation
            GameObject menuPlayersPlace = GameObject.Find("MenuPlayersPlace");
            GameObject originalFloor = GameObject.Find("Environment/PlayersPlace");
            GameObject customFloor = GameObject.Find("Platform Loader");
            // ReSharper disable restore Unity.PerformanceCriticalCodeInvocation

            if (menuPlayersPlace)
            {
                Plugin.logger.Info($"Moving MenuPlayersPlace floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");
                menuPlayersPlace.transform.position = new Vector3(0, floorOffset, 0);
            }

            if (originalFloor)
            {
                Plugin.logger.Info($"Moving PlayersPlace {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");
                originalFloor.transform.position = new Vector3(0, floorOffset, 0);
            }

            if (customFloor)
            {
                Plugin.logger.Info($"Moving Custom Platforms floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");

                _initialPlatformPosition = _initialPlatformPosition ?? customFloor.transform.position;
                customFloor.transform.position = (Vector3.up * floorOffset) + _initialPlatformPosition ?? Vector3.zero;
            }
        }
        
        public void CalibrateFullBodyTrackingManual(SpawnedAvatar spawnedAvatar)
        {
            TrackedDeviceManager input = PersistentSingleton<TrackedDeviceManager>.instance;

            TrackedDeviceState leftFoot = input.leftFoot;
            TrackedDeviceState rightFoot = input.rightFoot;
            TrackedDeviceState pelvis = input.waist;

            Settings.FullBodyCalibration fullBodyCalibration = SettingsManager.settings.GetAvatarSettings(spawnedAvatar.avatar.fullPath).fullBodyCalibration;

            if (pelvis.tracked)
            {
                Vector3 positionOffset = spawnedAvatar.tracking.pelvis.position - pelvis.position;
                Quaternion rotationOffset = Quaternion.Inverse(pelvis.rotation) * spawnedAvatar.tracking.pelvis.rotation;

                fullBodyCalibration.pelvis = new Pose(positionOffset, rotationOffset);
                Plugin.logger.Info("Saved pelvis pose correction " + fullBodyCalibration.pelvis);
            }

            if (leftFoot.tracked)
            {
                Vector3 positionOffset = spawnedAvatar.tracking.leftLeg.position - leftFoot.position;
                Quaternion rotationOffset = Quaternion.Inverse(leftFoot.rotation) * spawnedAvatar.tracking.leftLeg.rotation;

                fullBodyCalibration.leftLeg = new Pose(positionOffset, rotationOffset);
                Plugin.logger.Info("Saved left foot pose correction " + fullBodyCalibration.leftLeg);
            }

            if (rightFoot.tracked)
            {
                Vector3 positionOffset = spawnedAvatar.tracking.rightLeg.position - rightFoot.position;
                Quaternion rotationOffset = Quaternion.Inverse(rightFoot.rotation) * spawnedAvatar.tracking.rightLeg.rotation;

                fullBodyCalibration.rightLeg = new Pose(positionOffset, rotationOffset);
                Plugin.logger.Info("Saved right foot pose correction " + fullBodyCalibration.rightLeg);
            }
        }

        public void CalibrateFullBodyTrackingAuto(SpawnedAvatar spawnedAvatar)
        {
            Plugin.logger.Info("Calibrating full body tracking");

            TrackedDeviceManager input = PersistentSingleton<TrackedDeviceManager>.instance;

            TrackedDeviceState head = input.head;
            TrackedDeviceState leftFoot = input.leftFoot;
            TrackedDeviceState rightFoot = input.rightFoot;
            TrackedDeviceState pelvis = input.waist;

            Settings.FullBodyCalibration fullBodyCalibration = SettingsManager.settings.GetAvatarSettings(spawnedAvatar.avatar.fullPath).fullBodyCalibration;

            Vector3 floorNormal = Vector3.up;
            float floorPosition = SettingsManager.settings.moveFloorWithRoomAdjust ? BeatSaberUtil.GetRoomCenter().y : 0;

            if (leftFoot.tracked)
            {
                Vector3 leftFootForward = leftFoot.rotation * Vector3.up; // forward on feet trackers is y (up)
                Vector3 leftFootStraightForward = Vector3.ProjectOnPlane(leftFootForward, floorNormal); // get projection of forward vector on xz plane (floor)
                Quaternion leftRotationCorrection = Quaternion.Inverse(leftFoot.rotation) * Quaternion.LookRotation(Vector3.up, leftFootStraightForward); // get difference between world rotation and flat forward rotation
                fullBodyCalibration.leftLeg = new Pose((leftFoot.position.y - floorPosition) * Vector3.down, leftRotationCorrection);
                Plugin.logger.Info("Saved left foot pose correction " + fullBodyCalibration.leftLeg);
            }

            if (rightFoot.tracked)
            {
                Vector3 rightFootForward = rightFoot.rotation * Vector3.up;
                Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, floorNormal);
                Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
                fullBodyCalibration.rightLeg = new Pose((rightFoot.position.y - floorPosition) * Vector3.down, rightRotationCorrection);
                Plugin.logger.Info("Saved right foot pose correction " + fullBodyCalibration.rightLeg);
            }

            if (head.tracked && pelvis.tracked)
            {
                // using "standard" 8 head high body proportions w/ eyes at 1/2 head height
                // reference: https://miro.medium.com/max/3200/1*cqTRyEGl26l4CImEmWz68Q.jpeg
                float eyeHeight = head.position.y - floorPosition;

                Vector3 wantedPelvisPosition = new Vector3(0, eyeHeight / 22.5f * 14f, 0);
                Vector3 pelvisPositionCorrection = wantedPelvisPosition - Vector3.up * (pelvis.position.y - floorPosition);

                Vector3 pelvisForward = pelvis.rotation * Vector3.forward;
                Vector3 pelvisStraightForward = Vector3.ProjectOnPlane(pelvisForward, floorNormal);
                Quaternion pelvisRotationCorrection = Quaternion.Inverse(pelvis.rotation) * Quaternion.LookRotation(pelvisStraightForward, Vector3.up);

                fullBodyCalibration.pelvis = new Pose(pelvisPositionCorrection, pelvisRotationCorrection);
                Plugin.logger.Info("Saved pelvis pose correction " + fullBodyCalibration.pelvis);
            }
        }

        public void ClearFullBodyTrackingData(SpawnedAvatar spawnedAvatar)
        {
            Settings.FullBodyCalibration fullBodyCalibration = SettingsManager.settings.GetAvatarSettings(spawnedAvatar.avatar.fullPath).fullBodyCalibration;

            fullBodyCalibration.leftLeg = Pose.identity;
            fullBodyCalibration.rightLeg = Pose.identity;
            fullBodyCalibration.pelvis = Pose.identity;
        }
    }
}
