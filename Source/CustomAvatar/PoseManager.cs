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

using System;
using System.Linq;
using System.Reflection;
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
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal)       || !openHand_LeftThumbProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate)   || !openHand_LeftThumbIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal)         || !openHand_LeftThumbDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal)       || !openHand_LeftIndexProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate)   || !openHand_LeftIndexIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal)         || !openHand_LeftIndexDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal)      || !openHand_LeftMiddleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate)  || !openHand_LeftMiddleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal)        || !openHand_LeftMiddleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingProximal)        || !openHand_LeftRingProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate)    || !openHand_LeftRingIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingDistal)          || !openHand_LeftRingDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal)      || !openHand_LeftLittleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate)  || !openHand_LeftLittleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal)        || !openHand_LeftLittleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbProximal)      || !openHand_RightThumbProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate)  || !openHand_RightThumbIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbDistal)        || !openHand_RightThumbDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexProximal)      || !openHand_RightIndexProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate)  || !openHand_RightIndexIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexDistal)        || !openHand_RightIndexDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal)     || !openHand_RightMiddleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate) || !openHand_RightMiddleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal)       || !openHand_RightMiddleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingProximal)       || !openHand_RightRingProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate)   || !openHand_RightRingIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingDistal)         || !openHand_RightRingDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleProximal)     || !openHand_RightLittleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate) || !openHand_RightLittleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleDistal)       || !openHand_RightLittleDistal.Equals(Pose.identity));

        public bool closedHandIsValid =>
            animator && animator.isHuman &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal)       || !closedHand_LeftThumbProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate)   || !closedHand_LeftThumbIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal)         || !closedHand_LeftThumbDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal)       || !closedHand_LeftIndexProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate)   || !closedHand_LeftIndexIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal)         || !closedHand_LeftIndexDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal)      || !closedHand_LeftMiddleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate)  || !closedHand_LeftMiddleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal)        || !closedHand_LeftMiddleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingProximal)        || !closedHand_LeftRingProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate)    || !closedHand_LeftRingIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftRingDistal)          || !closedHand_LeftRingDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal)      || !closedHand_LeftLittleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate)  || !closedHand_LeftLittleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal)        || !closedHand_LeftLittleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbProximal)      || !closedHand_RightThumbProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate)  || !closedHand_RightThumbIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightThumbDistal)        || !closedHand_RightThumbDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexProximal)      || !closedHand_RightIndexProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate)  || !closedHand_RightIndexIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightIndexDistal)        || !closedHand_RightIndexDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal)     || !closedHand_RightMiddleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate) || !closedHand_RightMiddleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal)       || !closedHand_RightMiddleDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingProximal)       || !closedHand_RightRingProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate)   || !closedHand_RightRingIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightRingDistal)         || !closedHand_RightRingDistal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleProximal)     || !closedHand_RightLittleProximal.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate) || !closedHand_RightLittleIntermediate.Equals(Pose.identity)) &&
            (!animator.GetBoneTransform(HumanBodyBones.RightLittleDistal)       || !closedHand_RightLittleDistal.Equals(Pose.identity));

        public bool isValid => closedHandIsValid && openHandIsValid;

        public void OnEnable()
        {
            animator = GetComponent<Animator>();
        }

        public void SaveOpenHandPoses()   => SaveValues("openHand");
        public void SaveClosedHandPoses() => SaveValues("closedHand");

        public void ClearOpenHandPoses()   => ClearValues("openHand");
        public void ClearClosedHandPoses() => ClearValues("closedHand");

        public void InterpolateHandPoses(float t)
        {
            if (!animator.isHuman) return;

            foreach (FieldInfo field in typeof(PoseManager).GetFields().Where(f => f.Name.StartsWith("openHand")))
            {
                string boneName = field.Name.Split('_')[1];

                FieldInfo closed = typeof(PoseManager).GetField("closedHand_" + boneName);

                HumanBodyBones bone = (HumanBodyBones) Enum.Parse(typeof(HumanBodyBones), boneName);

                Pose openPose = (Pose)field.GetValue(this);
                Pose closedPose = (Pose)closed.GetValue(this);

                Transform boneTransform = animator.GetBoneTransform(bone);
		
                if (!boneTransform) continue;
                if (openPose.Equals(Pose.identity) || closedPose.Equals(Pose.identity)) return;

                boneTransform.localPosition = Vector3.Lerp(openPose.position, closedPose.position, t);
                boneTransform.localRotation = Quaternion.Slerp(openPose.rotation, closedPose.rotation, t);
            }
        }

        private void SaveValues(string prefix)
        {
            if (!animator.isHuman) return;

            foreach (FieldInfo field in typeof(PoseManager).GetFields().Where(f => f.Name.StartsWith(prefix)))
            {
                string boneName = field.Name.Split('_')[1];
                HumanBodyBones bone = (HumanBodyBones) Enum.Parse(typeof(HumanBodyBones), boneName);
                field.SetValue(this, TransformToLocalPose(animator.GetBoneTransform(bone)));
            }
        }

        private void ClearValues(string prefix)
        {
            if (!animator.isHuman) return;

            foreach (FieldInfo field in typeof(PoseManager).GetFields().Where(f => f.Name.StartsWith(prefix)))
            {
                field.SetValue(this, Pose.identity);
            }
        }

        private Pose TransformToLocalPose(Transform transform)
        {
            if (transform == null) return Pose.identity;

            return new Pose(transform.localPosition, transform.localRotation);
        }
    }
}
