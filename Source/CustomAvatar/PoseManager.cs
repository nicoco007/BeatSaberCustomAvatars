//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using UnityEngine;

namespace CustomAvatar
{
    [ExecuteAlways]
    [RequireComponent(typeof(Animator))]
    public class PoseManager : MonoBehaviour
    {
        public Pose openHand_LeftThumbProximal;
        public Pose openHand_LeftThumbIntermediate;
        public Pose openHand_LeftThumbDistal;

        public Pose openHand_LeftIndexProximal;
        public Pose openHand_LeftIndexIntermediate;
        public Pose openHand_LeftIndexDistal;

        public Pose openHand_LeftMiddleProximal;
        public Pose openHand_LeftMiddleIntermediate;
        public Pose openHand_LeftMiddleDistal;

        public Pose openHand_LeftRingProximal;
        public Pose openHand_LeftRingIntermediate;
        public Pose openHand_LeftRingDistal;

        public Pose openHand_LeftLittleProximal;
        public Pose openHand_LeftLittleIntermediate;
        public Pose openHand_LeftLittleDistal;

        public Pose openHand_RightThumbProximal;
        public Pose openHand_RightThumbIntermediate;
        public Pose openHand_RightThumbDistal;

        public Pose openHand_RightIndexProximal;
        public Pose openHand_RightIndexIntermediate;
        public Pose openHand_RightIndexDistal;

        public Pose openHand_RightMiddleProximal;
        public Pose openHand_RightMiddleIntermediate;
        public Pose openHand_RightMiddleDistal;

        public Pose openHand_RightRingProximal;
        public Pose openHand_RightRingIntermediate;
        public Pose openHand_RightRingDistal;

        public Pose openHand_RightLittleProximal;
        public Pose openHand_RightLittleIntermediate;
        public Pose openHand_RightLittleDistal;

        public Pose closedHand_LeftThumbProximal;
        public Pose closedHand_LeftThumbIntermediate;
        public Pose closedHand_LeftThumbDistal;

        public Pose closedHand_LeftIndexProximal;
        public Pose closedHand_LeftIndexIntermediate;
        public Pose closedHand_LeftIndexDistal;

        public Pose closedHand_LeftMiddleProximal;
        public Pose closedHand_LeftMiddleIntermediate;
        public Pose closedHand_LeftMiddleDistal;

        public Pose closedHand_LeftRingProximal;
        public Pose closedHand_LeftRingIntermediate;
        public Pose closedHand_LeftRingDistal;

        public Pose closedHand_LeftLittleProximal;
        public Pose closedHand_LeftLittleIntermediate;
        public Pose closedHand_LeftLittleDistal;

        public Pose closedHand_RightThumbProximal;
        public Pose closedHand_RightThumbIntermediate;
        public Pose closedHand_RightThumbDistal;

        public Pose closedHand_RightIndexProximal;
        public Pose closedHand_RightIndexIntermediate;
        public Pose closedHand_RightIndexDistal;

        public Pose closedHand_RightMiddleProximal;
        public Pose closedHand_RightMiddleIntermediate;
        public Pose closedHand_RightMiddleDistal;

        public Pose closedHand_RightRingProximal;
        public Pose closedHand_RightRingIntermediate;
        public Pose closedHand_RightRingDistal;

        public Pose closedHand_RightLittleProximal;
        public Pose closedHand_RightLittleIntermediate;
        public Pose closedHand_RightLittleDistal;

        public Animator animator;

        public bool openHandIsValid =>
            animator && animator.isHuman &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal) || IsValidPose(openHand_LeftThumbProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate) || IsValidPose(openHand_LeftThumbIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal) || IsValidPose(openHand_LeftThumbDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal) || IsValidPose(openHand_LeftIndexProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate) || IsValidPose(openHand_LeftIndexIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal) || IsValidPose(openHand_LeftIndexDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal) || IsValidPose(openHand_LeftMiddleProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate) || IsValidPose(openHand_LeftMiddleIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal) || IsValidPose(openHand_LeftMiddleDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingProximal) || IsValidPose(openHand_LeftRingProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate) || IsValidPose(openHand_LeftRingIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingDistal) || IsValidPose(openHand_LeftRingDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal) || IsValidPose(openHand_LeftLittleProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate) || IsValidPose(openHand_LeftLittleIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal) || IsValidPose(openHand_LeftLittleDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbProximal) || IsValidPose(openHand_RightThumbProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate) || IsValidPose(openHand_RightThumbIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbDistal) || IsValidPose(openHand_RightThumbDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexProximal) || IsValidPose(openHand_RightIndexProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate) || IsValidPose(openHand_RightIndexIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexDistal) || IsValidPose(openHand_RightIndexDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal) || IsValidPose(openHand_RightMiddleProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate) || IsValidPose(openHand_RightMiddleIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal) || IsValidPose(openHand_RightMiddleDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingProximal) || IsValidPose(openHand_RightRingProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate) || IsValidPose(openHand_RightRingIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingDistal) || IsValidPose(openHand_RightRingDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleProximal) || IsValidPose(openHand_RightLittleProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate) || IsValidPose(openHand_RightLittleIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleDistal) || IsValidPose(openHand_RightLittleDistal));

        public bool closedHandIsValid =>
            animator && animator.isHuman &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal) || IsValidPose(closedHand_LeftThumbProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate) || IsValidPose(closedHand_LeftThumbIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal) || IsValidPose(closedHand_LeftThumbDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal) || IsValidPose(closedHand_LeftIndexProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate) || IsValidPose(closedHand_LeftIndexIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal) || IsValidPose(closedHand_LeftIndexDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal) || IsValidPose(closedHand_LeftMiddleProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate) || IsValidPose(closedHand_LeftMiddleIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal) || IsValidPose(closedHand_LeftMiddleDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingProximal) || IsValidPose(closedHand_LeftRingProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate) || IsValidPose(closedHand_LeftRingIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingDistal) || IsValidPose(closedHand_LeftRingDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal) || IsValidPose(closedHand_LeftLittleProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate) || IsValidPose(closedHand_LeftLittleIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal) || IsValidPose(closedHand_LeftLittleDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbProximal) || IsValidPose(closedHand_RightThumbProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate) || IsValidPose(closedHand_RightThumbIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbDistal) || IsValidPose(closedHand_RightThumbDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexProximal) || IsValidPose(closedHand_RightIndexProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate) || IsValidPose(closedHand_RightIndexIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexDistal) || IsValidPose(closedHand_RightIndexDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal) || IsValidPose(closedHand_RightMiddleProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate) || IsValidPose(closedHand_RightMiddleIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal) || IsValidPose(closedHand_RightMiddleDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingProximal) || IsValidPose(closedHand_RightRingProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate) || IsValidPose(closedHand_RightRingIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingDistal) || IsValidPose(closedHand_RightRingDistal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleProximal) || IsValidPose(closedHand_RightLittleProximal)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate) || IsValidPose(closedHand_RightLittleIntermediate)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleDistal) || IsValidPose(closedHand_RightLittleDistal));

        public bool isValid => closedHandIsValid && openHandIsValid;

        public void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void Reset()
        {
            Awake();
        }

        public void InterpolateHandPoses(float t)
        {
            ApplyLeftHandFingerPoses(t, t, t, t, t);
            ApplyRightHandFingerPoses(t, t, t, t, t);
        }

        internal void ApplyLeftHandFingerPoses(float thumbCurl, float indexCurl, float middleCurl, float ringCurl, float littleCurl)
        {
            if (!animator.isHuman) return;

            ApplyBodyBonePose(HumanBodyBones.LeftThumbProximal, openHand_LeftThumbProximal, closedHand_LeftThumbProximal, thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftThumbIntermediate, openHand_LeftThumbIntermediate, closedHand_LeftThumbIntermediate, thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftThumbDistal, openHand_LeftThumbDistal, closedHand_LeftThumbDistal, thumbCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftIndexProximal, openHand_LeftIndexProximal, closedHand_LeftIndexProximal, indexCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftIndexIntermediate, openHand_LeftIndexIntermediate, closedHand_LeftIndexIntermediate, indexCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftIndexDistal, openHand_LeftIndexDistal, closedHand_LeftIndexDistal, indexCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftMiddleProximal, openHand_LeftMiddleProximal, closedHand_LeftMiddleProximal, middleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftMiddleIntermediate, openHand_LeftMiddleIntermediate, closedHand_LeftMiddleIntermediate, middleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftMiddleDistal, openHand_LeftMiddleDistal, closedHand_LeftMiddleDistal, middleCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftRingProximal, openHand_LeftRingProximal, closedHand_LeftRingProximal, ringCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftRingIntermediate, openHand_LeftRingIntermediate, closedHand_LeftRingIntermediate, ringCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftRingDistal, openHand_LeftRingDistal, closedHand_LeftRingDistal, ringCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftLittleProximal, openHand_LeftLittleProximal, closedHand_LeftLittleProximal, littleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftLittleIntermediate, openHand_LeftLittleIntermediate, closedHand_LeftLittleIntermediate, littleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftLittleDistal, openHand_LeftLittleDistal, closedHand_LeftLittleDistal, littleCurl);
        }

        internal void ApplyRightHandFingerPoses(float thumbCurl, float indexCurl, float middleCurl, float ringCurl, float littleCurl)
        {
            if (!animator.isHuman) return;

            ApplyBodyBonePose(HumanBodyBones.RightThumbProximal, openHand_RightThumbProximal, closedHand_RightThumbProximal, thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.RightThumbIntermediate, openHand_RightThumbIntermediate, closedHand_RightThumbIntermediate, thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.RightThumbDistal, openHand_RightThumbDistal, closedHand_RightThumbDistal, thumbCurl);

            ApplyBodyBonePose(HumanBodyBones.RightIndexProximal, openHand_RightIndexProximal, closedHand_RightIndexProximal, indexCurl);
            ApplyBodyBonePose(HumanBodyBones.RightIndexIntermediate, openHand_RightIndexIntermediate, closedHand_RightIndexIntermediate, indexCurl);
            ApplyBodyBonePose(HumanBodyBones.RightIndexDistal, openHand_RightIndexDistal, closedHand_RightIndexDistal, indexCurl);

            ApplyBodyBonePose(HumanBodyBones.RightMiddleProximal, openHand_RightMiddleProximal, closedHand_RightMiddleProximal, middleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightMiddleIntermediate, openHand_RightMiddleIntermediate, closedHand_RightMiddleIntermediate, middleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightMiddleDistal, openHand_RightMiddleDistal, closedHand_RightMiddleDistal, middleCurl);

            ApplyBodyBonePose(HumanBodyBones.RightRingProximal, openHand_RightRingProximal, closedHand_RightRingProximal, ringCurl);
            ApplyBodyBonePose(HumanBodyBones.RightRingIntermediate, openHand_RightRingIntermediate, closedHand_RightRingIntermediate, ringCurl);
            ApplyBodyBonePose(HumanBodyBones.RightRingDistal, openHand_RightRingDistal, closedHand_RightRingDistal, ringCurl);

            ApplyBodyBonePose(HumanBodyBones.RightLittleProximal, openHand_RightLittleProximal, closedHand_RightLittleProximal, littleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightLittleIntermediate, openHand_RightLittleIntermediate, closedHand_RightLittleIntermediate, littleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightLittleDistal, openHand_RightLittleDistal, closedHand_RightLittleDistal, littleCurl);
        }

        private void ApplyBodyBonePose(HumanBodyBones bodyBone, Pose openPose, Pose closedPose, float fade)
        {
            Transform boneTransform = animator.GetBoneTransform(bodyBone);

            if (!boneTransform) return;
            if (openPose.Equals(default) || closedPose.Equals(default)) return;

            boneTransform.localPosition = Vector3.Lerp(openPose.position, closedPose.position, fade);
            boneTransform.localRotation = Quaternion.Slerp(openPose.rotation, closedPose.rotation, fade);
        }

        public void SaveOpenHandPoses()
        {
            if (!animator.isHuman) return;

            openHand_LeftThumbProximal = GetPose(HumanBodyBones.LeftThumbProximal);
            openHand_LeftThumbIntermediate = GetPose(HumanBodyBones.LeftThumbIntermediate);
            openHand_LeftThumbDistal = GetPose(HumanBodyBones.LeftThumbDistal);
            openHand_LeftIndexProximal = GetPose(HumanBodyBones.LeftIndexProximal);
            openHand_LeftIndexIntermediate = GetPose(HumanBodyBones.LeftIndexIntermediate);
            openHand_LeftIndexDistal = GetPose(HumanBodyBones.LeftIndexDistal);
            openHand_LeftMiddleProximal = GetPose(HumanBodyBones.LeftMiddleProximal);
            openHand_LeftMiddleIntermediate = GetPose(HumanBodyBones.LeftMiddleIntermediate);
            openHand_LeftMiddleDistal = GetPose(HumanBodyBones.LeftMiddleDistal);
            openHand_LeftRingProximal = GetPose(HumanBodyBones.LeftRingProximal);
            openHand_LeftRingIntermediate = GetPose(HumanBodyBones.LeftRingIntermediate);
            openHand_LeftRingDistal = GetPose(HumanBodyBones.LeftRingDistal);
            openHand_LeftLittleProximal = GetPose(HumanBodyBones.LeftLittleProximal);
            openHand_LeftLittleIntermediate = GetPose(HumanBodyBones.LeftLittleIntermediate);
            openHand_LeftLittleDistal = GetPose(HumanBodyBones.LeftLittleDistal);

            openHand_RightThumbProximal = GetPose(HumanBodyBones.RightThumbProximal);
            openHand_RightThumbIntermediate = GetPose(HumanBodyBones.RightThumbIntermediate);
            openHand_RightThumbDistal = GetPose(HumanBodyBones.RightThumbDistal);
            openHand_RightIndexProximal = GetPose(HumanBodyBones.RightIndexProximal);
            openHand_RightIndexIntermediate = GetPose(HumanBodyBones.RightIndexIntermediate);
            openHand_RightIndexDistal = GetPose(HumanBodyBones.RightIndexDistal);
            openHand_RightMiddleProximal = GetPose(HumanBodyBones.RightMiddleProximal);
            openHand_RightMiddleIntermediate = GetPose(HumanBodyBones.RightMiddleIntermediate);
            openHand_RightMiddleDistal = GetPose(HumanBodyBones.RightMiddleDistal);
            openHand_RightRingProximal = GetPose(HumanBodyBones.RightRingProximal);
            openHand_RightRingIntermediate = GetPose(HumanBodyBones.RightRingIntermediate);
            openHand_RightRingDistal = GetPose(HumanBodyBones.RightRingDistal);
            openHand_RightLittleProximal = GetPose(HumanBodyBones.RightLittleProximal);
            openHand_RightLittleIntermediate = GetPose(HumanBodyBones.RightLittleIntermediate);
            openHand_RightLittleDistal = GetPose(HumanBodyBones.RightLittleDistal);
        }

        public void SaveClosedHandPoses()
        {
            if (!animator.isHuman) return;

            closedHand_LeftThumbProximal = GetPose(HumanBodyBones.LeftThumbProximal);
            closedHand_LeftThumbIntermediate = GetPose(HumanBodyBones.LeftThumbIntermediate);
            closedHand_LeftThumbDistal = GetPose(HumanBodyBones.LeftThumbDistal);
            closedHand_LeftIndexProximal = GetPose(HumanBodyBones.LeftIndexProximal);
            closedHand_LeftIndexIntermediate = GetPose(HumanBodyBones.LeftIndexIntermediate);
            closedHand_LeftIndexDistal = GetPose(HumanBodyBones.LeftIndexDistal);
            closedHand_LeftMiddleProximal = GetPose(HumanBodyBones.LeftMiddleProximal);
            closedHand_LeftMiddleIntermediate = GetPose(HumanBodyBones.LeftMiddleIntermediate);
            closedHand_LeftMiddleDistal = GetPose(HumanBodyBones.LeftMiddleDistal);
            closedHand_LeftRingProximal = GetPose(HumanBodyBones.LeftRingProximal);
            closedHand_LeftRingIntermediate = GetPose(HumanBodyBones.LeftRingIntermediate);
            closedHand_LeftRingDistal = GetPose(HumanBodyBones.LeftRingDistal);
            closedHand_LeftLittleProximal = GetPose(HumanBodyBones.LeftLittleProximal);
            closedHand_LeftLittleIntermediate = GetPose(HumanBodyBones.LeftLittleIntermediate);
            closedHand_LeftLittleDistal = GetPose(HumanBodyBones.LeftLittleDistal);

            closedHand_RightThumbProximal = GetPose(HumanBodyBones.RightThumbProximal);
            closedHand_RightThumbIntermediate = GetPose(HumanBodyBones.RightThumbIntermediate);
            closedHand_RightThumbDistal = GetPose(HumanBodyBones.RightThumbDistal);
            closedHand_RightIndexProximal = GetPose(HumanBodyBones.RightIndexProximal);
            closedHand_RightIndexIntermediate = GetPose(HumanBodyBones.RightIndexIntermediate);
            closedHand_RightIndexDistal = GetPose(HumanBodyBones.RightIndexDistal);
            closedHand_RightMiddleProximal = GetPose(HumanBodyBones.RightMiddleProximal);
            closedHand_RightMiddleIntermediate = GetPose(HumanBodyBones.RightMiddleIntermediate);
            closedHand_RightMiddleDistal = GetPose(HumanBodyBones.RightMiddleDistal);
            closedHand_RightRingProximal = GetPose(HumanBodyBones.RightRingProximal);
            closedHand_RightRingIntermediate = GetPose(HumanBodyBones.RightRingIntermediate);
            closedHand_RightRingDistal = GetPose(HumanBodyBones.RightRingDistal);
            closedHand_RightLittleProximal = GetPose(HumanBodyBones.RightLittleProximal);
            closedHand_RightLittleIntermediate = GetPose(HumanBodyBones.RightLittleIntermediate);
            closedHand_RightLittleDistal = GetPose(HumanBodyBones.RightLittleDistal);
        }

        public void ClearOpenHandPoses()
        {
            openHand_LeftThumbProximal = default;
            openHand_LeftThumbIntermediate = default;
            openHand_LeftThumbDistal = default;
            openHand_LeftIndexProximal = default;
            openHand_LeftIndexIntermediate = default;
            openHand_LeftIndexDistal = default;
            openHand_LeftMiddleProximal = default;
            openHand_LeftMiddleIntermediate = default;
            openHand_LeftMiddleDistal = default;
            openHand_LeftRingProximal = default;
            openHand_LeftRingIntermediate = default;
            openHand_LeftRingDistal = default;
            openHand_LeftLittleProximal = default;
            openHand_LeftLittleIntermediate = default;
            openHand_LeftLittleDistal = default;

            openHand_RightThumbProximal = default;
            openHand_RightThumbIntermediate = default;
            openHand_RightThumbDistal = default;
            openHand_RightIndexProximal = default;
            openHand_RightIndexIntermediate = default;
            openHand_RightIndexDistal = default;
            openHand_RightMiddleProximal = default;
            openHand_RightMiddleIntermediate = default;
            openHand_RightMiddleDistal = default;
            openHand_RightRingProximal = default;
            openHand_RightRingIntermediate = default;
            openHand_RightRingDistal = default;
            openHand_RightLittleProximal = default;
            openHand_RightLittleIntermediate = default;
            openHand_RightLittleDistal = default;
        }

        public void ClearClosedHandPoses()
        {
            closedHand_LeftThumbProximal = default;
            closedHand_LeftThumbIntermediate = default;
            closedHand_LeftThumbDistal = default;
            closedHand_LeftIndexProximal = default;
            closedHand_LeftIndexIntermediate = default;
            closedHand_LeftIndexDistal = default;
            closedHand_LeftMiddleProximal = default;
            closedHand_LeftMiddleIntermediate = default;
            closedHand_LeftMiddleDistal = default;
            closedHand_LeftRingProximal = default;
            closedHand_LeftRingIntermediate = default;
            closedHand_LeftRingDistal = default;
            closedHand_LeftLittleProximal = default;
            closedHand_LeftLittleIntermediate = default;
            closedHand_LeftLittleDistal = default;

            closedHand_RightThumbProximal = default;
            closedHand_RightThumbIntermediate = default;
            closedHand_RightThumbDistal = default;
            closedHand_RightIndexProximal = default;
            closedHand_RightIndexIntermediate = default;
            closedHand_RightIndexDistal = default;
            closedHand_RightMiddleProximal = default;
            closedHand_RightMiddleIntermediate = default;
            closedHand_RightMiddleDistal = default;
            closedHand_RightRingProximal = default;
            closedHand_RightRingIntermediate = default;
            closedHand_RightRingDistal = default;
            closedHand_RightLittleProximal = default;
            closedHand_RightLittleIntermediate = default;
            closedHand_RightLittleDistal = default;
        }

        private Pose GetPose(HumanBodyBones bone)
        {
            return TransformToLocalPose(animator.GetBoneTransform(bone));
        }

        private Pose TransformToLocalPose(Transform transform)
        {
            return transform ? new Pose(transform.localPosition, transform.localRotation) : Pose.identity;
        }

        private bool IsValidPose(Pose pose)
        {
            // pose.Equals(other) does
            //   pose.position == other.position && pose.rotation == other.rotation
            // instead of
            //   position.Equals(other.position) && rotation.Equals(other.rotation)
            // thanks Unity
            return !pose.position.Equals(default) && !pose.rotation.Equals(default) && (pose.rotation.x != 0 || pose.rotation.y != 0 || pose.rotation.z != 0 || pose.rotation.w != 0);
        }
    }
}
