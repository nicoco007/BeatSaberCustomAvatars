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

extern alias BeatSaberFinalIK;

using AvatarScriptPack;
using CustomAvatar.Exceptions;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;
using VRIK = BeatSaberFinalIK::RootMotion.FinalIK.VRIK;

namespace CustomAvatar.Avatar
{
    /// <summary>
    /// Contains static information about an avatar. 
    /// </summary>
    public class LoadedAvatar : IDisposable
    {
        /// <summary>
        /// The name of the file from which the avatar was loaded.
        /// </summary>
        public readonly string fileName;

        /// <summary>
        /// The full path of the file from which the avatar was loaded.
        /// </summary>
        public readonly string fullPath;

        /// <summary>
        /// The avatar prefab.
        /// </summary>
        public readonly GameObject prefab;

        /// <summary>
        /// The <see cref="AvatarDescriptor"/> retrieved from the root object on the prefab.
        /// </summary>
        public readonly AvatarDescriptor descriptor;

        /// <summary>
        /// Whether or not this avatar has IK.
        /// </summary>
        public readonly bool isIKAvatar;

        /// <summary>
        /// Whether or not this avatar has one or more full body (pelvis/feet) tracking points
        /// </summary>
        public readonly bool supportsFullBodyTracking;

        /// <summary>
        /// Whether or not this avatar supports finger tracking.
        /// </summary>
        public readonly bool supportsFingerTracking;

        /// <summary>
        /// The avatar's eye height.
        /// </summary>
        public readonly float eyeHeight;

        /// <summary>
        /// The avatar's estimated arm span.
        /// </summary>
        public readonly float armSpan;

        internal readonly Transform head;
        internal readonly Transform leftHand;
        internal readonly Transform rightHand;
        internal readonly Transform leftLeg;
        internal readonly Transform rightLeg;
        internal readonly Transform pelvis;

        private readonly ILogger<LoadedAvatar> _logger;

        internal LoadedAvatar(string fullPath, GameObject avatarGameObject, ILoggerProvider loggerProvider, DiContainer container)
        {
            this.fullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            prefab = avatarGameObject ? avatarGameObject : throw new ArgumentNullException(nameof(avatarGameObject));
            descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");

            fileName = Path.GetFileName(fullPath);

            prefab.name = $"LoadedAvatar({descriptor.name})";

            _logger = loggerProvider.CreateLogger<LoadedAvatar>(descriptor.name);

            head      = prefab.transform.Find("Head");
            leftHand  = prefab.transform.Find("LeftHand");
            rightHand = prefab.transform.Find("RightHand");
            pelvis    = prefab.transform.Find("Pelvis");
            leftLeg   = prefab.transform.Find("LeftLeg");
            rightLeg  = prefab.transform.Find("RightLeg");

            #pragma warning disable CS0618
            VRIKManager vrikManager = prefab.GetComponentInChildren<VRIKManager>();
            IKManager ikManager = prefab.GetComponentInChildren<IKManager>();
            #pragma warning restore CS0618

            // migrate IKManager/IKManagerAdvanced to VRIKManager
            if (ikManager)
            {
                if (!vrikManager) vrikManager = container.InstantiateComponent<VRIKManager>(prefab);

                _logger.Warning("IKManager and IKManagerAdvanced are deprecated; please migrate to VRIKManager");

                ApplyIKManagerFields(vrikManager, ikManager);
                Object.Destroy(ikManager);
            }

            if (vrikManager)
            {
                if (!vrikManager.areReferencesFilled)
                {
                    _logger.Warning($"References are not filled on '{vrikManager.name}'; detecting references automatically");
                    vrikManager.AutoDetectReferences();
                }
            }

            // remove any existing VRIK instances
            foreach (VRIK existingVrik in prefab.GetComponentsInChildren<VRIK>())
            {
                _logger.Warning($"Found VRIK on '{existingVrik.name}'; manually adding VRIK to an avatar is no longer needed, please remove it");

                if (existingVrik && vrikManager && existingVrik.references.isFilled && !vrikManager.areReferencesFilled)
                {
                    _logger.Warning($"Copying references from VRIK on '{existingVrik.name}'; this is deprecated behaviour and will be removed in a future release");
                    CopyReferencesFromExistingVrik(vrikManager, existingVrik.references);
                }

                Object.Destroy(existingVrik);
            }

            if (vrikManager)
            {
                if (vrikManager.references_root != vrikManager.transform)
                {
                    _logger.Warning("VRIKManager is not on the root reference transform; this may cause unexpected issues");
                }

                FixTrackingReferences(vrikManager);
            }

            if (prefab.transform.localPosition.sqrMagnitude > 0)
            {
                _logger.Warning("Avatar root position is not at origin; this may cause unexpected issues");
            }

            var poseManager = prefab.GetComponentInChildren<PoseManager>();

            isIKAvatar = vrikManager;
            supportsFullBodyTracking = pelvis || leftLeg || rightLeg;
            supportsFingerTracking = poseManager && poseManager.isValid;

            eyeHeight = GetEyeHeight();
            armSpan = GetArmSpan(vrikManager);
        }

        public void Dispose()
        {
            Object.Destroy(prefab);
        }

        private float GetEyeHeight()
        {
            if (!head)
            {
                _logger.Warning("Avatar does not have a head tracking reference");
                return BeatSaberUtilities.kDefaultPlayerEyeHeight;
            }

            if (head.position.y <= 0)
            {
                return BeatSaberUtilities.kDefaultPlayerEyeHeight;
            }

            // many avatars rely on this being global because their root position isn't at (0, 0, 0)
            return head.position.y;
        }

        private void FixTrackingReferences(VRIKManager vrikManager)
        {
            FixTrackingReference("Head",       head,      vrikManager.references_head,                                          vrikManager.solver_spine_headTarget);
            FixTrackingReference("Left Hand",  leftHand,  vrikManager.references_leftHand,                                      vrikManager.solver_leftArm_target);
            FixTrackingReference("Right Hand", rightHand, vrikManager.references_rightHand,                                     vrikManager.solver_rightArm_target);
            FixTrackingReference("Waist",      pelvis,    vrikManager.references_pelvis,                                        vrikManager.solver_spine_pelvisTarget);
            FixTrackingReference("Left Foot",  leftLeg,   vrikManager.references_leftToes  ?? vrikManager.references_leftFoot,  vrikManager.solver_leftLeg_target);
            FixTrackingReference("Right Foot", rightLeg,  vrikManager.references_rightToes ?? vrikManager.references_rightFoot, vrikManager.solver_rightLeg_target);
        }

        private void FixTrackingReference(string name, Transform tracker, Transform reference, Transform target)
        {
            if (!reference)
            {
                _logger.Warning($"Could not find {name} reference");
                return;
            }

            if (!target)
            {
                // target will be added automatically, no need to adjust
                return;
            }

            Vector3 offset = target.position - reference.position;

            // only warn if offset is larger than 1 mm
            if (offset.magnitude > 0.001f)
            {
                // manually putting each coordinate gives more resolution
                _logger.Warning($"{name} bone and target are not at the same position; moving '{tracker.name}' by ({offset.x:0.000}, {offset.y:0.000}, {offset.z:0.000})");
                tracker.position -= offset;
            }
        }

        /// <summary>
        /// Measure avatar arm span. Since the player's measured arm span is actually from palm to palm
        /// (approximately) due to the way the controllers are held, this isn't "true" arm span.
        /// </summary>
        private float GetArmSpan(VRIKManager vrikManager)
        {
            if (!vrikManager) return BeatSaberUtilities.kDefaultPlayerArmSpan;

            Transform leftShoulder = vrikManager.references_leftShoulder;
            Transform leftUpperArm = vrikManager.references_leftUpperArm;
            Transform leftLowerArm = vrikManager.references_leftForearm;

            Transform rightShoulder = vrikManager.references_rightShoulder;
            Transform rightUpperArm = vrikManager.references_rightUpperArm;
            Transform rightLowerArm = vrikManager.references_rightForearm;

            if (!leftShoulder || !leftUpperArm || !leftLowerArm || !rightShoulder || !rightUpperArm || !rightLowerArm)
            {
                _logger.Warning("Could not calculate avatar arm span due to missing bones");
                return BeatSaberUtilities.kDefaultPlayerArmSpan;
            }

            if (!leftHand || !rightHand)
            {
                _logger.Warning("Could not calculate avatar arm span due to missing tracking references");
                return BeatSaberUtilities.kDefaultPlayerArmSpan;
            }

            float leftArmLength = Vector3.Distance(leftShoulder.position, leftUpperArm.position) + Vector3.Distance(leftUpperArm.position, leftLowerArm.position) + Vector3.Distance(leftLowerArm.position, leftHand.position);
            float rightArmLength = Vector3.Distance(rightShoulder.position, rightUpperArm.position) + Vector3.Distance(rightUpperArm.position, rightLowerArm.position) + Vector3.Distance(rightLowerArm.position, rightHand.position);
            float shoulderToShoulderDistance = Vector3.Distance(leftShoulder.position, rightShoulder.position);

            float totalLength = leftArmLength + shoulderToShoulderDistance + rightArmLength;

            return totalLength;
        }

        private void CopyReferencesFromExistingVrik(VRIKManager vrikManager, VRIK.References references)
        {
            vrikManager.references_root          = references.root;
            vrikManager.references_pelvis        = references.pelvis;
            vrikManager.references_spine         = references.spine;
            vrikManager.references_chest         = references.chest;
            vrikManager.references_neck          = references.neck;
            vrikManager.references_head          = references.head;
            vrikManager.references_leftShoulder  = references.leftShoulder;
            vrikManager.references_leftUpperArm  = references.leftUpperArm;
            vrikManager.references_leftForearm   = references.leftForearm;
            vrikManager.references_leftHand      = references.leftHand;
            vrikManager.references_rightShoulder = references.rightShoulder;
            vrikManager.references_rightUpperArm = references.rightUpperArm;
            vrikManager.references_rightForearm  = references.rightForearm;
            vrikManager.references_rightHand     = references.rightHand;
            vrikManager.references_leftThigh     = references.leftThigh;
            vrikManager.references_leftCalf      = references.leftCalf;
            vrikManager.references_leftFoot      = references.leftFoot;
            vrikManager.references_leftToes      = references.leftToes;
            vrikManager.references_rightThigh    = references.rightThigh;
            vrikManager.references_rightCalf     = references.rightCalf;
            vrikManager.references_rightFoot     = references.rightFoot;
            vrikManager.references_rightToes     = references.rightToes;
        }

        #pragma warning disable CS0618
        private void ApplyIKManagerFields(VRIKManager vrikManager, IKManager ikManager)
        {
            vrikManager.solver_spine_headTarget = ikManager.HeadTarget;
            vrikManager.solver_leftArm_target = ikManager.LeftHandTarget;
            vrikManager.solver_rightArm_target = ikManager.RightHandTarget;

            if (!(ikManager is IKManagerAdvanced ikManagerAdvanced)) return;

            vrikManager.solver_spine_pelvisTarget           = ikManagerAdvanced.Spine_pelvisTarget;
            vrikManager.solver_spine_pelvisPositionWeight   = ikManagerAdvanced.Spine_pelvisPositionWeight;
            vrikManager.solver_spine_pelvisRotationWeight   = ikManagerAdvanced.Spine_pelvisRotationWeight;
            vrikManager.solver_spine_positionWeight         = ikManagerAdvanced.Head_positionWeight;
            vrikManager.solver_spine_rotationWeight         = ikManagerAdvanced.Head_rotationWeight;
            vrikManager.solver_spine_chestGoal              = ikManagerAdvanced.Spine_chestGoal;
            vrikManager.solver_spine_chestGoalWeight        = ikManagerAdvanced.Spine_chestGoalWeight;
            vrikManager.solver_spine_minHeadHeight          = ikManagerAdvanced.Spine_minHeadHeight;
            vrikManager.solver_spine_bodyPosStiffness       = ikManagerAdvanced.Spine_bodyPosStiffness;
            vrikManager.solver_spine_bodyRotStiffness       = ikManagerAdvanced.Spine_bodyRotStiffness;
            vrikManager.solver_spine_neckStiffness          = ikManagerAdvanced.Spine_neckStiffness;
            vrikManager.solver_spine_chestClampWeight       = ikManagerAdvanced.Spine_chestClampWeight;
            vrikManager.solver_spine_headClampWeight        = ikManagerAdvanced.Spine_headClampWeight;
            vrikManager.solver_spine_maintainPelvisPosition = ikManagerAdvanced.Spine_maintainPelvisPosition;
            vrikManager.solver_spine_maxRootAngle           = ikManagerAdvanced.Spine_maxRootAngle;

            vrikManager.solver_leftArm_bendGoal               = ikManagerAdvanced.LeftArm_bendGoal;
            vrikManager.solver_leftArm_positionWeight         = ikManagerAdvanced.LeftArm_positionWeight;
            vrikManager.solver_leftArm_rotationWeight         = ikManagerAdvanced.LeftArm_rotationWeight;
            vrikManager.solver_leftArm_shoulderRotationMode   = ikManagerAdvanced.LeftArm_shoulderRotationMode;
            vrikManager.solver_leftArm_shoulderRotationWeight = ikManagerAdvanced.LeftArm_shoulderRotationWeight;
            vrikManager.solver_leftArm_bendGoalWeight         = ikManagerAdvanced.LeftArm_bendGoalWeight;
            vrikManager.solver_leftArm_swivelOffset           = ikManagerAdvanced.LeftArm_swivelOffset;
            vrikManager.solver_leftArm_wristToPalmAxis        = ikManagerAdvanced.LeftArm_wristToPalmAxis;
            vrikManager.solver_leftArm_palmToThumbAxis        = ikManagerAdvanced.LeftArm_palmToThumbAxis;

            vrikManager.solver_rightArm_bendGoal               = ikManagerAdvanced.RightArm_bendGoal;
            vrikManager.solver_rightArm_positionWeight         = ikManagerAdvanced.RightArm_positionWeight;
            vrikManager.solver_rightArm_rotationWeight         = ikManagerAdvanced.RightArm_rotationWeight;
            vrikManager.solver_rightArm_shoulderRotationMode   = ikManagerAdvanced.RightArm_shoulderRotationMode;
            vrikManager.solver_rightArm_shoulderRotationWeight = ikManagerAdvanced.RightArm_shoulderRotationWeight;
            vrikManager.solver_rightArm_bendGoalWeight         = ikManagerAdvanced.RightArm_bendGoalWeight;
            vrikManager.solver_rightArm_swivelOffset           = ikManagerAdvanced.RightArm_swivelOffset;
            vrikManager.solver_rightArm_wristToPalmAxis        = ikManagerAdvanced.RightArm_wristToPalmAxis;
            vrikManager.solver_rightArm_palmToThumbAxis        = ikManagerAdvanced.RightArm_palmToThumbAxis;

            vrikManager.solver_leftLeg_target         = ikManagerAdvanced.LeftLeg_target;
            vrikManager.solver_leftLeg_positionWeight = ikManagerAdvanced.LeftLeg_positionWeight;
            vrikManager.solver_leftLeg_rotationWeight = ikManagerAdvanced.LeftLeg_rotationWeight;
            vrikManager.solver_leftLeg_bendGoal       = ikManagerAdvanced.LeftLeg_bendGoal;
            vrikManager.solver_leftLeg_bendGoalWeight = ikManagerAdvanced.LeftLeg_bendGoalWeight;
            vrikManager.solver_leftLeg_swivelOffset = ikManagerAdvanced.LeftLeg_swivelOffset;

            vrikManager.solver_rightLeg_target         = ikManagerAdvanced.RightLeg_target;
            vrikManager.solver_rightLeg_positionWeight = ikManagerAdvanced.RightLeg_positionWeight;
            vrikManager.solver_rightLeg_rotationWeight = ikManagerAdvanced.RightLeg_rotationWeight;
            vrikManager.solver_rightLeg_bendGoal       = ikManagerAdvanced.RightLeg_bendGoal;
            vrikManager.solver_rightLeg_bendGoalWeight = ikManagerAdvanced.RightLeg_bendGoalWeight;
            vrikManager.solver_rightLeg_swivelOffset   = ikManagerAdvanced.RightLeg_swivelOffset;

            vrikManager.solver_locomotion_weight                = ikManagerAdvanced.Locomotion_weight;
            vrikManager.solver_locomotion_footDistance          = ikManagerAdvanced.Locomotion_footDistance;
            vrikManager.solver_locomotion_stepThreshold         = ikManagerAdvanced.Locomotion_stepThreshold;
            vrikManager.solver_locomotion_angleThreshold        = ikManagerAdvanced.Locomotion_angleThreshold;
            vrikManager.solver_locomotion_comAngleMlp           = ikManagerAdvanced.Locomotion_comAngleMlp;
            vrikManager.solver_locomotion_maxVelocity           = ikManagerAdvanced.Locomotion_maxVelocity;
            vrikManager.solver_locomotion_velocityFactor        = ikManagerAdvanced.Locomotion_velocityFactor;
            vrikManager.solver_locomotion_maxLegStretch         = ikManagerAdvanced.Locomotion_maxLegStretch;
            vrikManager.solver_locomotion_rootSpeed             = ikManagerAdvanced.Locomotion_rootSpeed;
            vrikManager.solver_locomotion_stepSpeed             = ikManagerAdvanced.Locomotion_stepSpeed;
            vrikManager.solver_locomotion_stepHeight            = ikManagerAdvanced.Locomotion_stepHeight;
            vrikManager.solver_locomotion_heelHeight            = ikManagerAdvanced.Locomotion_heelHeight;
            vrikManager.solver_locomotion_relaxLegTwistMinAngle = ikManagerAdvanced.Locomotion_relaxLegTwistMinAngle;
            vrikManager.solver_locomotion_relaxLegTwistSpeed    = ikManagerAdvanced.Locomotion_relaxLegTwistSpeed;
            vrikManager.solver_locomotion_stepInterpolation     = ikManagerAdvanced.Locomotion_stepInterpolation;
            vrikManager.solver_locomotion_offset                = ikManagerAdvanced.Locomotion_offset;
            vrikManager.solver_locomotion_onLeftFootstep        = ikManagerAdvanced.Locomotion_onLeftFootstep;
            vrikManager.solver_locomotion_onRightFootstep       = ikManagerAdvanced.Locomotion_onRightFootstep;
        }
        #pragma warning restore CS0618
    }
}
