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
        private Vector3? _initialPlatformPosition;

        public void ResizeAvatar(SpawnedAvatar avatar)
        {
            if (!avatar.customAvatar.descriptor.allowHeightCalibration || !avatar.customAvatar.isIKAvatar) return;

            // compute scale
            float scale;
            AvatarResizeMode resizeMode = Plugin.settings.resizeMode;

            switch (resizeMode)
            {
                case AvatarResizeMode.ArmSpan:
                    float avatarArmLength = avatar.customAvatar.GetArmSpan();

                    if (avatarArmLength > 0)
                    {
                        scale = Plugin.settings.playerArmSpan / avatarArmLength;
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

            if (Plugin.settings.enableFloorAdjust && avatar.customAvatar.isIKAvatar)
            {
                float playerEyeHeight = BeatSaberUtil.GetPlayerEyeHeight();
                float avatarEyeHeight = avatar.customAvatar.eyeHeight;

                floorOffset = playerEyeHeight - avatarEyeHeight * avatar.tracking.scale;

                if (Plugin.settings.moveFloorWithRoomAdjust)
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

        public void CalibrateFullBodyTracking()
        {
            Plugin.logger.Info("Calibrating full body tracking");

            TrackedDeviceManager input = PersistentSingleton<TrackedDeviceManager>.instance;

            TrackedDeviceState head = input.head;
            TrackedDeviceState leftFoot = input.leftFoot;
            TrackedDeviceState rightFoot = input.rightFoot;
            TrackedDeviceState pelvis = input.waist;

            Vector3 floorNormal = Vector3.up;
            float floorPosition = Plugin.settings.moveFloorWithRoomAdjust ? BeatSaberUtil.GetRoomCenter().y : 0;

            if (leftFoot.found)
            {
                Vector3 leftFootForward = leftFoot.rotation * Vector3.up; // forward on feet trackers is y (up)
                Vector3 leftFootStraightForward = Vector3.ProjectOnPlane(leftFootForward, floorNormal); // get projection of forward vector on xz plane (floor)
                Quaternion leftRotationCorrection = Quaternion.Inverse(leftFoot.rotation) * Quaternion.LookRotation(Vector3.up, leftFootStraightForward); // get difference between world rotation and flat forward rotation
                Plugin.settings.fullBodyCalibration.leftLeg = new Pose((leftFoot.position.y - floorPosition) * Vector3.down, leftRotationCorrection);
                Plugin.logger.Info("Saved left foot pose correction " + Plugin.settings.fullBodyCalibration.leftLeg);
            }

            if (rightFoot.found)
            {
                Vector3 rightFootForward = rightFoot.rotation * Vector3.up;
                Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, floorNormal);
                Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
                Plugin.settings.fullBodyCalibration.rightLeg = new Pose((rightFoot.position.y - floorPosition) * Vector3.down, rightRotationCorrection);
                Plugin.logger.Info("Saved right foot pose correction " + Plugin.settings.fullBodyCalibration.rightLeg);
            }

            if (head.found && pelvis.found)
            {
                // using "standard" 8 head high body proportions w/ eyes at 1/2 head height
                // reference: https://miro.medium.com/max/3200/1*cqTRyEGl26l4CImEmWz68Q.jpeg
                float eyeHeight = head.position.y - floorPosition;

                Vector3 wantedPelvisPosition = new Vector3(0, eyeHeight / 15f * 10f, 0);
                Vector3 pelvisPositionCorrection = wantedPelvisPosition - Vector3.up * (pelvis.position.y - floorPosition);

                Vector3 pelvisForward = pelvis.rotation * Vector3.forward;
                Vector3 pelvisStraightForward = Vector3.ProjectOnPlane(pelvisForward, floorNormal);
                Quaternion pelvisRotationCorrection = Quaternion.Inverse(pelvis.rotation) * Quaternion.LookRotation(pelvisStraightForward, Vector3.up);

                Plugin.settings.fullBodyCalibration.pelvis = new Pose(pelvisPositionCorrection, pelvisRotationCorrection);
                Plugin.logger.Info("Saved pelvis pose correction " + Plugin.settings.fullBodyCalibration.pelvis);
            }
        }

        public void ClearFullBodyTrackingData()
        {
            Plugin.settings.fullBodyCalibration.leftLeg = Pose.identity;
            Plugin.settings.fullBodyCalibration.rightLeg = Pose.identity;
            Plugin.settings.fullBodyCalibration.pelvis = Pose.identity;
        }
    }
}
