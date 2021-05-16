//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
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
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal) || !openHand_LeftThumbProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate) || !openHand_LeftThumbIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal) || !openHand_LeftThumbDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal) || !openHand_LeftIndexProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate) || !openHand_LeftIndexIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal) || !openHand_LeftIndexDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal) || !openHand_LeftMiddleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate) || !openHand_LeftMiddleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal) || !openHand_LeftMiddleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingProximal) || !openHand_LeftRingProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate) || !openHand_LeftRingIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingDistal) || !openHand_LeftRingDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal) || !openHand_LeftLittleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate) || !openHand_LeftLittleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal) || !openHand_LeftLittleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbProximal) || !openHand_RightThumbProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate) || !openHand_RightThumbIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbDistal) || !openHand_RightThumbDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexProximal) || !openHand_RightIndexProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate) || !openHand_RightIndexIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexDistal) || !openHand_RightIndexDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal) || !openHand_RightMiddleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate) || !openHand_RightMiddleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal) || !openHand_RightMiddleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingProximal) || !openHand_RightRingProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate) || !openHand_RightRingIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingDistal) || !openHand_RightRingDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleProximal) || !openHand_RightLittleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate) || !openHand_RightLittleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleDistal) || !openHand_RightLittleDistal.Equals(Pose.identity));

        public bool closedHandIsValid =>
            animator && animator.isHuman &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal) || !closedHand_LeftThumbProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate) || !closedHand_LeftThumbIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal) || !closedHand_LeftThumbDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal) || !closedHand_LeftIndexProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate) || !closedHand_LeftIndexIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal) || !closedHand_LeftIndexDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal) || !closedHand_LeftMiddleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate) || !closedHand_LeftMiddleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal) || !closedHand_LeftMiddleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingProximal) || !closedHand_LeftRingProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate) || !closedHand_LeftRingIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingDistal) || !closedHand_LeftRingDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal) || !closedHand_LeftLittleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate) || !closedHand_LeftLittleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal) || !closedHand_LeftLittleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbProximal) || !closedHand_RightThumbProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate) || !closedHand_RightThumbIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbDistal) || !closedHand_RightThumbDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexProximal) || !closedHand_RightIndexProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate) || !closedHand_RightIndexIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexDistal) || !closedHand_RightIndexDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal) || !closedHand_RightMiddleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate) || !closedHand_RightMiddleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal) || !closedHand_RightMiddleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingProximal) || !closedHand_RightRingProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate) || !closedHand_RightRingIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingDistal) || !closedHand_RightRingDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleProximal) || !closedHand_RightLittleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate) || !closedHand_RightLittleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleDistal) || !closedHand_RightLittleDistal.Equals(Pose.identity));

        public bool isValid => closedHandIsValid && openHandIsValid;

        public void OnEnable()
        {
            animator = GetComponent<Animator>();
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
            if (openPose.Equals(Pose.identity) || closedPose.Equals(Pose.identity)) return;

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
            if (!animator.isHuman) return;

            openHand_LeftThumbProximal = Pose.identity;
            openHand_LeftThumbIntermediate = Pose.identity;
            openHand_LeftThumbDistal = Pose.identity;
            openHand_LeftIndexProximal = Pose.identity;
            openHand_LeftIndexIntermediate = Pose.identity;
            openHand_LeftIndexDistal = Pose.identity;
            openHand_LeftMiddleProximal = Pose.identity;
            openHand_LeftMiddleIntermediate = Pose.identity;
            openHand_LeftMiddleDistal = Pose.identity;
            openHand_LeftRingProximal = Pose.identity;
            openHand_LeftRingIntermediate = Pose.identity;
            openHand_LeftRingDistal = Pose.identity;
            openHand_LeftLittleProximal = Pose.identity;
            openHand_LeftLittleIntermediate = Pose.identity;
            openHand_LeftLittleDistal = Pose.identity;

            openHand_RightThumbProximal = Pose.identity;
            openHand_RightThumbIntermediate = Pose.identity;
            openHand_RightThumbDistal = Pose.identity;
            openHand_RightIndexProximal = Pose.identity;
            openHand_RightIndexIntermediate = Pose.identity;
            openHand_RightIndexDistal = Pose.identity;
            openHand_RightMiddleProximal = Pose.identity;
            openHand_RightMiddleIntermediate = Pose.identity;
            openHand_RightMiddleDistal = Pose.identity;
            openHand_RightRingProximal = Pose.identity;
            openHand_RightRingIntermediate = Pose.identity;
            openHand_RightRingDistal = Pose.identity;
            openHand_RightLittleProximal = Pose.identity;
            openHand_RightLittleIntermediate = Pose.identity;
            openHand_RightLittleDistal = Pose.identity;
        }

        public void ClearClosedHandPoses()
        {
            if (!animator.isHuman) return;

            closedHand_LeftThumbProximal = Pose.identity;
            closedHand_LeftThumbIntermediate = Pose.identity;
            closedHand_LeftThumbDistal = Pose.identity;
            closedHand_LeftIndexProximal = Pose.identity;
            closedHand_LeftIndexIntermediate = Pose.identity;
            closedHand_LeftIndexDistal = Pose.identity;
            closedHand_LeftMiddleProximal = Pose.identity;
            closedHand_LeftMiddleIntermediate = Pose.identity;
            closedHand_LeftMiddleDistal = Pose.identity;
            closedHand_LeftRingProximal = Pose.identity;
            closedHand_LeftRingIntermediate = Pose.identity;
            closedHand_LeftRingDistal = Pose.identity;
            closedHand_LeftLittleProximal = Pose.identity;
            closedHand_LeftLittleIntermediate = Pose.identity;
            closedHand_LeftLittleDistal = Pose.identity;

            closedHand_RightThumbProximal = Pose.identity;
            closedHand_RightThumbIntermediate = Pose.identity;
            closedHand_RightThumbDistal = Pose.identity;
            closedHand_RightIndexProximal = Pose.identity;
            closedHand_RightIndexIntermediate = Pose.identity;
            closedHand_RightIndexDistal = Pose.identity;
            closedHand_RightMiddleProximal = Pose.identity;
            closedHand_RightMiddleIntermediate = Pose.identity;
            closedHand_RightMiddleDistal = Pose.identity;
            closedHand_RightRingProximal = Pose.identity;
            closedHand_RightRingIntermediate = Pose.identity;
            closedHand_RightRingDistal = Pose.identity;
            closedHand_RightLittleProximal = Pose.identity;
            closedHand_RightLittleIntermediate = Pose.identity;
            closedHand_RightLittleDistal = Pose.identity;
        }

        private Pose GetPose(HumanBodyBones bone)
        {
            return TransformToLocalPose(animator.GetBoneTransform(bone));
        }

        private Pose TransformToLocalPose(Transform transform)
        {
            return transform ? new Pose(transform.localPosition, transform.localRotation) : Pose.identity;
        }
    }
}
