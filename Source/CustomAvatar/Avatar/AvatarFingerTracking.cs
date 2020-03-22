using DynamicOpenVR.IO;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    internal class AvatarFingerTracking : MonoBehaviour
    {
        private SkeletalInput _leftHandAnimAction;
        private SkeletalInput _rightHandAnimAction;

        private Animator _animator;
        private PoseManager _poseManager;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        private void Start()
        {
            _animator = GetComponentInChildren<Animator>();
            _poseManager = GetComponentInChildren<PoseManager>();

            _leftHandAnimAction  = new SkeletalInput("/actions/customavatars/in/lefthandanim");
            _rightHandAnimAction = new SkeletalInput("/actions/customavatars/in/righthandanim");
        }

        private void Update()
        {
            ApplyFingerTracking();
        }

        private void OnDestroy()
        {
            _leftHandAnimAction?.Dispose();
            _rightHandAnimAction?.Dispose();
        }

        // ReSharper restore UnusedMember.Local
        #pragma warning restore IDE0051
        #endregion

        public void ApplyFingerTracking()
        {
            SkeletalSummaryData leftHandAnim = _leftHandAnimAction.summaryData;
            SkeletalSummaryData rightHandAnim = _rightHandAnimAction.summaryData;

            if (_leftHandAnimAction.isActive && leftHandAnim != null)
            {
                ApplyLeftHandFingerPoses(leftHandAnim.thumbCurl, leftHandAnim.indexCurl, leftHandAnim.middleCurl, leftHandAnim.ringCurl, leftHandAnim.littleCurl);
            }
            else
            {
                ApplyLeftHandFingerPoses(1, 1, 1, 1, 1);
            }

            if (_rightHandAnimAction.isActive && rightHandAnim != null)
            {
                ApplyRightHandFingerPoses(rightHandAnim.thumbCurl, rightHandAnim.indexCurl, rightHandAnim.middleCurl, rightHandAnim.ringCurl, rightHandAnim.littleCurl);
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
