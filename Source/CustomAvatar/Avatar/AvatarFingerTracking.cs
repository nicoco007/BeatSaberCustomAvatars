//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    public class AvatarFingerTracking : MonoBehaviour
    {
        private Animator _animator;
        private PoseManager _poseManager;
        private IAvatarInput _input;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        [Inject]
        private void Inject(IAvatarInput input)
        {
            _input = input;
        }

        private void Start()
        {
            _animator = GetComponentInChildren<Animator>();
            _poseManager = GetComponentInChildren<PoseManager>();
        }

        private void Update()
        {
            ApplyFingerTracking();
        }

        // ReSharper restore UnusedMember.Local
        #pragma warning restore IDE0051
        #endregion

        private void ApplyFingerTracking()
        {
            if (_input.TryGetLeftHandFingerCurl(out FingerCurl leftFingerCurl))
            {
                ApplyLeftHandFingerPoses(leftFingerCurl.thumb, leftFingerCurl.index, leftFingerCurl.middle, leftFingerCurl.ring, leftFingerCurl.little);
            }
            else
            {
                ApplyLeftHandFingerPoses(1, 1, 1, 1, 1);
            }

            if (_input.TryGetRightHandFingerCurl(out FingerCurl rightFingerCurl))
            {
                ApplyRightHandFingerPoses(rightFingerCurl.thumb, rightFingerCurl.index, rightFingerCurl.middle, rightFingerCurl.ring, rightFingerCurl.little);
            }
            else
            {
                ApplyRightHandFingerPoses(1, 1, 1, 1, 1);
            }
        }

        private void ApplyLeftHandFingerPoses(float thumbCurl, float indexCurl, float middleCurl, float ringCurl, float littleCurl)
        {
            ApplyBodyBonePose(HumanBodyBones.LeftThumbProximal,       _poseManager.openHand_LeftThumbProximal,       _poseManager.closedHand_LeftThumbProximal,       thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftThumbIntermediate,   _poseManager.openHand_LeftThumbIntermediate,   _poseManager.closedHand_LeftThumbIntermediate,   thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftThumbDistal,         _poseManager.openHand_LeftThumbDistal,         _poseManager.closedHand_LeftThumbDistal,         thumbCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftIndexProximal,       _poseManager.openHand_LeftIndexProximal,       _poseManager.closedHand_LeftIndexProximal,       indexCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftIndexIntermediate,   _poseManager.openHand_LeftIndexIntermediate,   _poseManager.closedHand_LeftIndexIntermediate,   indexCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftIndexDistal,         _poseManager.openHand_LeftIndexDistal,         _poseManager.closedHand_LeftIndexDistal,         indexCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftMiddleProximal,      _poseManager.openHand_LeftMiddleProximal,      _poseManager.closedHand_LeftMiddleProximal,      middleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftMiddleIntermediate,  _poseManager.openHand_LeftMiddleIntermediate,  _poseManager.closedHand_LeftMiddleIntermediate,  middleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftMiddleDistal,        _poseManager.openHand_LeftMiddleDistal,        _poseManager.closedHand_LeftMiddleDistal,        middleCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftRingProximal,        _poseManager.openHand_LeftRingProximal,        _poseManager.closedHand_LeftRingProximal,        ringCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftRingIntermediate,    _poseManager.openHand_LeftRingIntermediate,    _poseManager.closedHand_LeftRingIntermediate,    ringCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftRingDistal,          _poseManager.openHand_LeftRingDistal,          _poseManager.closedHand_LeftRingDistal,          ringCurl);

            ApplyBodyBonePose(HumanBodyBones.LeftLittleProximal,      _poseManager.openHand_LeftLittleProximal,      _poseManager.closedHand_LeftLittleProximal,      littleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftLittleIntermediate,  _poseManager.openHand_LeftLittleIntermediate,  _poseManager.closedHand_LeftLittleIntermediate,  littleCurl);
            ApplyBodyBonePose(HumanBodyBones.LeftLittleDistal,        _poseManager.openHand_LeftLittleDistal,        _poseManager.closedHand_LeftLittleDistal,        littleCurl);
        }

        private void ApplyRightHandFingerPoses(float thumbCurl, float indexCurl, float middleCurl, float ringCurl, float littleCurl)
        {
            ApplyBodyBonePose(HumanBodyBones.RightThumbProximal,       _poseManager.openHand_RightThumbProximal,       _poseManager.closedHand_RightThumbProximal,       thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.RightThumbIntermediate,   _poseManager.openHand_RightThumbIntermediate,   _poseManager.closedHand_RightThumbIntermediate,   thumbCurl);
            ApplyBodyBonePose(HumanBodyBones.RightThumbDistal,         _poseManager.openHand_RightThumbDistal,         _poseManager.closedHand_RightThumbDistal,         thumbCurl);

            ApplyBodyBonePose(HumanBodyBones.RightIndexProximal,       _poseManager.openHand_RightIndexProximal,       _poseManager.closedHand_RightIndexProximal,       indexCurl);
            ApplyBodyBonePose(HumanBodyBones.RightIndexIntermediate,   _poseManager.openHand_RightIndexIntermediate,   _poseManager.closedHand_RightIndexIntermediate,   indexCurl);
            ApplyBodyBonePose(HumanBodyBones.RightIndexDistal,         _poseManager.openHand_RightIndexDistal,         _poseManager.closedHand_RightIndexDistal,         indexCurl);

            ApplyBodyBonePose(HumanBodyBones.RightMiddleProximal,      _poseManager.openHand_RightMiddleProximal,      _poseManager.closedHand_RightMiddleProximal,      middleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightMiddleIntermediate,  _poseManager.openHand_RightMiddleIntermediate,  _poseManager.closedHand_RightMiddleIntermediate,  middleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightMiddleDistal,        _poseManager.openHand_RightMiddleDistal,        _poseManager.closedHand_RightMiddleDistal,        middleCurl);

            ApplyBodyBonePose(HumanBodyBones.RightRingProximal,        _poseManager.openHand_RightRingProximal,        _poseManager.closedHand_RightRingProximal,        ringCurl);
            ApplyBodyBonePose(HumanBodyBones.RightRingIntermediate,    _poseManager.openHand_RightRingIntermediate,    _poseManager.closedHand_RightRingIntermediate,    ringCurl);
            ApplyBodyBonePose(HumanBodyBones.RightRingDistal,          _poseManager.openHand_RightRingDistal,          _poseManager.closedHand_RightRingDistal,          ringCurl);

            ApplyBodyBonePose(HumanBodyBones.RightLittleProximal,      _poseManager.openHand_RightLittleProximal,      _poseManager.closedHand_RightLittleProximal,      littleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightLittleIntermediate,  _poseManager.openHand_RightLittleIntermediate,  _poseManager.closedHand_RightLittleIntermediate,  littleCurl);
            ApplyBodyBonePose(HumanBodyBones.RightLittleDistal,        _poseManager.openHand_RightLittleDistal,        _poseManager.closedHand_RightLittleDistal,        littleCurl);
        }

        private void ApplyBodyBonePose(HumanBodyBones bodyBone, Pose open, Pose closed, float fade)
        {
            Transform boneTransform = _animator.GetBoneTransform(bodyBone);

            if (!boneTransform) return;

            boneTransform.localPosition = Vector3.Lerp(open.position, closed.position, fade);
            boneTransform.localRotation = Quaternion.Slerp(open.rotation, closed.rotation, fade);
        }
    }
}
