using System;
using System.Collections;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar
{
    internal class AvatarTailor
    {
        private Vector3? _initialPlatformPosition;

        public void ResizeAvatar(SpawnedAvatar avatar, float playerHeight)
        {
            if (!avatar.customAvatar.descriptor.allowHeightCalibration || !avatar.customAvatar.isIKAvatar) return;

            // compute scale
            float scale;
            AvatarResizeMode resizeMode = SettingsManager.settings.resizeMode;

            switch (resizeMode)
            {
                case AvatarResizeMode.ArmSpan:
                    float avatarArmLength = avatar.customAvatar.GetArmSpan();

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
                    float avatarEyeHeight = avatar.customAvatar.eyeHeight;

                    if (avatarEyeHeight > 0)
                    {
                        scale = playerHeight / avatarEyeHeight;
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
            avatar.behaviour.scale = scale;

            SharedCoroutineStarter.instance.StartCoroutine(FloorMendingWithDelay(avatar));
        }

        private IEnumerator FloorMendingWithDelay(SpawnedAvatar avatar)
        {
            yield return new WaitForEndOfFrame(); // wait for CustomFloorPlugin:PlatformManager:Start to hide original platform

            float floorOffset = 0f;

            if (SettingsManager.settings.enableFloorAdjust && avatar.customAvatar.isIKAvatar)
            {
                float playerViewPointHeight = BeatSaberUtil.GetPlayerEyeHeight();
                float avatarViewPointHeight = avatar.customAvatar.viewPoint.position.y;

                floorOffset = playerViewPointHeight - avatarViewPointHeight * avatar.behaviour.scale;
            }

            // apply offset
			avatar.behaviour.position = new Vector3(0, floorOffset, 0);
            
            // ReSharper disable Unity.PerformanceCriticalCodeInvocation
            var originalFloor = GameObject.Find("MenuPlayersPlace") ?? GameObject.Find("Static/PlayersPlace");
            var customFloor = GameObject.Find("Platform Loader");
            // ReSharper disable restore Unity.PerformanceCriticalCodeInvocation

            if (originalFloor)
            {
                Plugin.logger.Info($"Moving original floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");
                originalFloor.transform.position = new Vector3(0, floorOffset, 0);
            }

            if (customFloor)
            {
                Plugin.logger.Info($"Moving Custom Platforms floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");

                _initialPlatformPosition = _initialPlatformPosition ?? customFloor.transform.position;
                customFloor.transform.position = (Vector3.up * floorOffset) + _initialPlatformPosition ?? Vector3.zero;
            }
        }

        public void CalibrateFullBodyTracking()
        {
            Plugin.logger.Info("Calibrating full body tracking");

            TrackedDeviceManager input = PersistentSingleton<TrackedDeviceManager>.instance;

            TrackedDeviceState head = input.head;
            TrackedDeviceState leftFoot = input.leftFoot;
            TrackedDeviceState rightFoot = input.rightFoot;
            TrackedDeviceState pelvis = input.waist;

            var normal = Vector3.up;

            if (leftFoot.found)
            {
                Vector3 leftFootForward = leftFoot.rotation * Vector3.up; // forward on feet trackers is y (up)
                Vector3 leftFootStraightForward = Vector3.ProjectOnPlane(leftFootForward, normal); // get projection of forward vector on xz plane (floor)
                Quaternion leftRotationCorrection = Quaternion.Inverse(leftFoot.rotation) * Quaternion.LookRotation(Vector3.up, leftFootStraightForward); // get difference between world rotation and flat forward rotation
                SettingsManager.settings.fullBodyCalibration.leftLeg = new Pose(leftFoot.position.y * Vector3.down, leftRotationCorrection);
                Plugin.logger.Info("Saved left foot pose correction " + SettingsManager.settings.fullBodyCalibration.leftLeg);
            }

            if (rightFoot.found)
            {
                Vector3 rightFootForward = rightFoot.rotation * Vector3.up;
                Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, normal);
                Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
                SettingsManager.settings.fullBodyCalibration.rightLeg = new Pose(rightFoot.position.y * Vector3.down, rightRotationCorrection);
                Plugin.logger.Info("Saved right foot pose correction " + SettingsManager.settings.fullBodyCalibration.rightLeg);
            }

            if (head.found && pelvis.found)
            {
                // using "standard" 8 head high body proportions w/ eyes at 1/2 head height
                // reference: https://miro.medium.com/max/3200/1*cqTRyEGl26l4CImEmWz68Q.jpeg
                var eyeHeight = head.position.y;
                Vector3 wantedPelvisPosition = new Vector3(0, eyeHeight / 15f * 10f, 0);
                Vector3 pelvisPositionCorrection = wantedPelvisPosition - Vector3.up * pelvis.position.y;
                SettingsManager.settings.fullBodyCalibration.pelvis = new Pose(pelvisPositionCorrection, Quaternion.identity);
                Plugin.logger.Info("Saved pelvis pose correction " + SettingsManager.settings.fullBodyCalibration.pelvis);
            }
        }

        public void ClearFullBodyTrackingData()
        {
            SettingsManager.settings.fullBodyCalibration.leftLeg = Pose.identity;
            SettingsManager.settings.fullBodyCalibration.rightLeg = Pose.identity;
            SettingsManager.settings.fullBodyCalibration.pelvis = Pose.identity;
        }
    }
}
