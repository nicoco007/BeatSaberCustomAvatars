using System;
using System.Collections;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar
{
    public class AvatarTailor
    {
        private Vector3? initialPlatformPosition = null;

        public void ResizeAvatar(SpawnedAvatar avatar)
        {
            // compute scale
            float scale;
            AvatarResizeMode resizeMode = SettingsManager.settings.resizeMode;

            switch (resizeMode)
            {
                case AvatarResizeMode.ArmSpan:
                    float playerArmLength = SettingsManager.settings.playerArmSpan;
                    var avatarArmLength = avatar.customAvatar.GetArmSpan();

                    scale = playerArmLength / avatarArmLength;
                    break;

                case AvatarResizeMode.Height:
                    scale = BeatSaberUtil.GetPlayerEyeHeight() / avatar.customAvatar.eyeHeight;
                    break;

                default:
                    scale = 1.0f;
                    break;
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
            
            var originalFloor = GameObject.Find("MenuPlayersPlace") ?? GameObject.Find("Static/PlayersPlace");
            var customFloor = GameObject.Find("Platform Loader");

            Plugin.logger.Info("originalFloor " + originalFloor);

            if (originalFloor != null)
            {
                Plugin.logger.Info($"Moving original floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");
                originalFloor.transform.position = new Vector3(0, floorOffset, 0);
            }

            if (customFloor != null)
            {
                Plugin.logger.Info($"Moving Custom Platforms floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");

                initialPlatformPosition = initialPlatformPosition ?? customFloor.transform.position;
                customFloor.transform.position = (Vector3.up * floorOffset) + initialPlatformPosition ?? Vector3.zero;
            }
        }

        public void CalibrateFullBodyTracking()
        {
            Plugin.logger.Info("Calibrating full body tracking");

            TrackedDeviceManager input = PersistentSingleton<TrackedDeviceManager>.instance;

            TrackedDeviceState head = input.Head;
            TrackedDeviceState leftFoot = input.LeftFoot;
            TrackedDeviceState rightFoot = input.RightFoot;
            TrackedDeviceState pelvis = input.Waist;

            var normal = Vector3.up;

            if (leftFoot.Found)
            {
                Vector3 leftFootForward = leftFoot.Rotation * Vector3.up; // forward on feet trackers is y (up)
                Vector3 leftFootStraightForward = Vector3.ProjectOnPlane(leftFootForward, normal); // get projection of forward vector on xz plane (floor)
                Quaternion leftRotationCorrection = Quaternion.Inverse(leftFoot.Rotation) * Quaternion.LookRotation(Vector3.up, leftFootStraightForward); // get difference between world rotation and flat forward rotation
                SettingsManager.settings.fullBodyCalibration.leftLeg = new Pose(leftFoot.Position.y * Vector3.down, leftRotationCorrection);
                Plugin.logger.Info("Saved left foot pose correction " + SettingsManager.settings.fullBodyCalibration.leftLeg);
            }

            if (rightFoot.Found)
            {
                Vector3 rightFootForward = rightFoot.Rotation * Vector3.up;
                Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, normal);
                Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.Rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
                SettingsManager.settings.fullBodyCalibration.rightLeg = new Pose(rightFoot.Position.y * Vector3.down, rightRotationCorrection);
                Plugin.logger.Info("Saved right foot pose correction " + SettingsManager.settings.fullBodyCalibration.rightLeg);
            }

            if (head.Found && pelvis.Found)
            {
                // using "standard" 8 head high body proportions w/ eyes at 1/2 head height
                // reference: https://miro.medium.com/max/3200/1*cqTRyEGl26l4CImEmWz68Q.jpeg
                var eyeHeight = head.Position.y;
                Vector3 wantedPelvisPosition = new Vector3(0, eyeHeight / 15f * 10f, 0);
                Vector3 pelvisPositionCorrection = wantedPelvisPosition - Vector3.up * pelvis.Position.y;
                SettingsManager.settings.fullBodyCalibration.pelvis = new Pose(pelvisPositionCorrection, Quaternion.identity);
                Plugin.logger.Info("Saved pelvis pose correction " + SettingsManager.settings.fullBodyCalibration.pelvis);
            }
        }

        public void ClearFullBodyTrackingData()
        {
            SettingsManager.settings.fullBodyCalibration.leftLeg = default;
            SettingsManager.settings.fullBodyCalibration.rightLeg = default;
            SettingsManager.settings.fullBodyCalibration.pelvis = default;
        }
    }
}
