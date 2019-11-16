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

        private Animator FindAvatarAnimator(GameObject gameObject)
        {
            var vrik = gameObject.GetComponentInChildren<AvatarScriptPack.VRIK>();
            if (vrik == null) return null;
            var animator = vrik.gameObject.GetComponentInChildren<Animator>();
            if (animator.avatar == null || !animator.isHuman) return null;
            return animator;
        }

        public void ResizeAvatar(SpawnedAvatar avatar)
        {
            // compute scale
            float scale;
            AvatarResizeMode resizeMode = SettingsManager.Settings.ResizeMode;

            switch (resizeMode)
            {
                case AvatarResizeMode.ArmSpan:
                    float playerArmLength = SettingsManager.Settings.PlayerArmSpan;
                    var avatarArmLength = avatar.CustomAvatar.GetArmSpan();

                    scale = playerArmLength / avatarArmLength;
                    break;

                case AvatarResizeMode.Height:
                    scale = BeatSaberUtil.GetPlayerEyeHeight() / avatar.CustomAvatar.EyeHeight;
                    break;

                default:
                    scale = 1.0f;
                    break;
            }

            // apply scale
            avatar.Behaviour.Scale = scale;

            SharedCoroutineStarter.instance.StartCoroutine(FloorMendingWithDelay(avatar));
        }

        private IEnumerator FloorMendingWithDelay(SpawnedAvatar avatar)
        {
            yield return new WaitForEndOfFrame(); // wait for CustomFloorPlugin:PlatformManager:Start to hide original platform

            float floorOffset = 0f;

            if (SettingsManager.Settings.EnableFloorAdjust)
            {
                float playerViewPointHeight = BeatSaberUtil.GetPlayerEyeHeight();
                float avatarViewPointHeight = avatar.CustomAvatar.ViewPoint.position.y;

                floorOffset = playerViewPointHeight - avatarViewPointHeight * avatar.Behaviour.Scale;
            }

            // apply offset
			avatar.Behaviour.Position = new Vector3(0, floorOffset, 0);
            
            var originalFloor = GameObject.Find("MenuPlayersPlace") ?? GameObject.Find("Static/PlayersPlace");
            var customFloor = GameObject.Find("Platform Loader");

            Plugin.Logger.Info("originalFloor " + originalFloor);

            if (originalFloor != null)
            {
                Plugin.Logger.Info($"Moving original floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");
                originalFloor.transform.position = new Vector3(0, floorOffset, 0);
            }

            if (customFloor != null)
            {
                Plugin.Logger.Info($"Moving Custom Platforms floor {Math.Abs(floorOffset)} m {(floorOffset >= 0 ? "up" : "down")}");

                initialPlatformPosition = initialPlatformPosition ?? customFloor.transform.position;
                customFloor.transform.position = (Vector3.up * floorOffset) + initialPlatformPosition ?? Vector3.zero;
            }
        }

        public void CalibrateFullBodyTracking()
        {
            Plugin.Logger.Info("Calibrating full body tracking");

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
                AvatarBehaviour.LeftLegCorrection = new Pose(leftFoot.Position.y * Vector3.down, leftRotationCorrection);
            }

            if (rightFoot.Found)
            {
                Vector3 rightFootForward = rightFoot.Rotation * Vector3.up;
                Vector3 rightFootStraightForward = Vector3.ProjectOnPlane(rightFootForward, normal);
                Quaternion rightRotationCorrection = Quaternion.Inverse(rightFoot.Rotation) * Quaternion.LookRotation(Vector3.up, rightFootStraightForward);
                AvatarBehaviour.RightLegCorrection = new Pose(rightFoot.Position.y * Vector3.down, rightRotationCorrection);
            }

            if (head.Found && pelvis.Found)
            {
                // using "standard" 8 head high body proportions w/ eyes at 1/2 head height
                // reference: https://miro.medium.com/max/3200/1*cqTRyEGl26l4CImEmWz68Q.jpeg
                var eyeHeight = head.Position.y;
                Vector3 wantedPelvisPosition = new Vector3(0, eyeHeight / 15f * 10f, 0);
                Vector3 pelvisPositionCorrection = wantedPelvisPosition - Vector3.up * pelvis.Position.y;
                AvatarBehaviour.PelvisCorrection = new Pose(pelvisPositionCorrection, Quaternion.identity);
            }
        }

        public void ClearFullBodyTrackingData()
        {
            AvatarBehaviour.LeftLegCorrection = default;
            AvatarBehaviour.RightLegCorrection = default;
            AvatarBehaviour.PelvisCorrection = default;
        }
    }
}
