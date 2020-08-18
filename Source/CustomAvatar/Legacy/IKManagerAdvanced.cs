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

using System;
using BeatSaberFinalIK::RootMotion;
using BeatSaberFinalIK::RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

#pragma warning disable CS0649
namespace AvatarScriptPack
{
    [Obsolete("Use VRIKManager")]
    internal class IKManagerAdvanced : IKManager
    {
        [Space(5)]
        [Header("Full Body Tracking")]
        [Tooltip("The pelvis target, useful with seated rigs.")]
        public Transform Spine_pelvisTarget;
        [Range(0f, 1f), Tooltip("Positional weight of the pelvis target.")]
        public float Spine_pelvisPositionWeight;
        [Range(0f, 1f), Tooltip("Rotational weight of the pelvis target.")]
        public float Spine_pelvisRotationWeight;

        [Tooltip("The toe/foot target.")]
        public Transform LeftLeg_target;
        [Range(0f, 1f), Tooltip("Positional weight of the toe/foot target.")]
        public float LeftLeg_positionWeight;
        [Range(0f, 1f), Tooltip("Rotational weight of the toe/foot target.")]
        public float LeftLeg_rotationWeight;

        [Tooltip("The toe/foot target.")]
        public Transform RightLeg_target;
        [Range(0f, 1f), Tooltip("Positional weight of the toe/foot target.")]
        public float RightLeg_positionWeight;
        [Range(0f, 1f), Tooltip("Rotational weight of the toe/foot target.")]
        public float RightLeg_rotationWeight;

        [Space(20)]

        /*******************
         * Spine
         ******************/

        [Range(0f, 1f), Tooltip("Positional weight of the head target.")]
        public float Head_positionWeight = 1f;

        [Range(0f, 1f), Tooltip("Rotational weight of the head target.")]
        public float Head_rotationWeight = 1f;

        [Tooltip("If 'Chest Goal Weight' is greater than 0, the chest will be turned towards this Transform.")]
        public Transform Spine_chestGoal;

        [Range(0f, 1f), Tooltip("Rotational weight of the chest target.")]
        public float Spine_chestGoalWeight;

        [Tooltip("Minimum height of the head from the root of the character.")]
        public float Spine_minHeadHeight = 0.8f;

        [Range(0f, 1f), Tooltip("Determines how much the body will follow the position of the head.")]
        public float Spine_bodyPosStiffness = 0.55f;

        [Range(0f, 1f), Tooltip("Determines how much the body will follow the rotation of the head.")]
        public float Spine_bodyRotStiffness = 0.1f;

        [Range(0f, 1f), FormerlySerializedAs("chestRotationWeight"), Tooltip("Determines how much the chest will rotate to the rotation of the head.")]
        public float Spine_neckStiffness = 0.2f;

        [Range(0f, 1f), Tooltip("Clamps chest rotation.")]
        public float Spine_chestClampWeight = 0.5f;

        [Range(0f, 1f), Tooltip("Clamps head rotation.")]
        public float Spine_headClampWeight = 0.6f;

        [Range(0f, 1f), Tooltip("How much will the pelvis maintain it's animated position?")]
        public float Spine_maintainPelvisPosition = 0.2f;

        [Range(0f, 180f), Tooltip("Will automatically rotate the root of the character if the head target has turned past this angle.")]
        public float Spine_maxRootAngle = 25f;


        [Space(20)]

        /*******************
         * Left Arm
         ******************/

        [Tooltip("The elbow will be bent towards this Transform if 'Bend Goal Weight' > 0.")]
        public Transform LeftArm_bendGoal;

        [Range(0f, 1f), Tooltip("Positional weight of the hand target.")]
        public float LeftArm_positionWeight = 1f;

        [Range(0f, 1f), Tooltip("Rotational weight of the hand target")]
        public float LeftArm_rotationWeight = 1f;

        [Tooltip("Different techniques for shoulder bone rotation.")]
        public IKSolverVR.Arm.ShoulderRotationMode LeftArm_shoulderRotationMode;

        [Range(0f, 1f), Tooltip("The weight of shoulder rotation")]
        public float LeftArm_shoulderRotationWeight = 1f;

        [Range(0f, 1f), Tooltip("If greater than 0, will bend the elbow towards the 'Bend Goal' Transform.")]
        public float LeftArm_bendGoalWeight;

        [Range(-180f, 180f), Tooltip("Angular offset of the elbow bending direction.")]
        public float LeftArm_swivelOffset;

        [Tooltip("Local axis of the hand bone that points from the wrist towards the palm. Used for defining hand bone orientation.")]
        public Vector3 LeftArm_wristToPalmAxis = Vector3.zero;

        [Tooltip("Local axis of the hand bone that points from the palm towards the thumb. Used for defining hand bone orientation.")]
        public Vector3 LeftArm_palmToThumbAxis = Vector3.zero;


        [Space(20)]

        /*******************
         * Right Arm
         ******************/

        [Tooltip("The elbow will be bent towards this Transform if 'Bend Goal Weight' > 0.")]
        public Transform RightArm_bendGoal;

        [Range(0f, 1f), Tooltip("Positional weight of the hand target.")]
        public float RightArm_positionWeight = 1f;

        [Range(0f, 1f), Tooltip("Rotational weight of the hand target")]
        public float RightArm_rotationWeight = 1f;

        [Tooltip("Different techniques for shoulder bone rotation.")]
        public IKSolverVR.Arm.ShoulderRotationMode RightArm_shoulderRotationMode;

        [Range(0f, 1f), Tooltip("The weight of shoulder rotation")]
        public float RightArm_shoulderRotationWeight = 1f;

        [Range(0f, 1f), Tooltip("If greater than 0, will bend the elbow towards the 'Bend Goal' Transform.")]
        public float RightArm_bendGoalWeight;

        [Range(-180f, 180f), Tooltip("Angular offset of the elbow bending direction.")]
        public float RightArm_swivelOffset;

        [Tooltip("Local axis of the hand bone that points from the wrist towards the palm. Used for defining hand bone orientation.")]
        public Vector3 RightArm_wristToPalmAxis = Vector3.zero;

        [Tooltip("Local axis of the hand bone that points from the palm towards the thumb. Used for defining hand bone orientation.")]
        public Vector3 RightArm_palmToThumbAxis = Vector3.zero;


        [Space(20)]

        /*******************
         * Left Leg
         ******************/

        [Tooltip("The knee will be bent towards this Transform if 'Bend Goal Weight' > 0.")]
        public Transform LeftLeg_bendGoal;

        [Range(0f, 1f), Tooltip("If greater than 0, will bend the knee towards the 'Bend Goal' Transform.")]
        public float LeftLeg_bendGoalWeight;

        [Range(-180f, 180f), Tooltip("Angular offset of the knee bending direction.")]
        public float LeftLeg_swivelOffset;


        [Space(20)]

        /*******************
         * Right Leg
         ******************/

        [Tooltip("The knee will be bent towards this Transform if 'Bend Goal Weight' > 0.")]
        public Transform RightLeg_bendGoal;

        [Range(0f, 1f), Tooltip("If greater than 0, will bend the knee towards the 'Bend Goal' Transform.")]
        public float RightLeg_bendGoalWeight;

        [Range(-180f, 180f), Tooltip("Angular offset of the knee bending direction.")]
        public float RightLeg_swivelOffset;


        [Space(20)]

        /*******************
         * Locomotion
         ******************/

        [Range(0f, 1f), Tooltip("Used for blending in/out of procedural locomotion.")]
        public float Locomotion_weight = 1f;

        [Tooltip("Tries to maintain this distance between the legs.")]
        public float Locomotion_footDistance = 0.3f;

        [Tooltip("Makes a step only if step target position is at least this far from the current footstep or the foot does not reach the current footstep anymore or footstep angle is past the 'Angle Threshold'.")]
        public float Locomotion_stepThreshold = 0.4f;

        [Tooltip("Makes a step only if step target position is at least 'Step Threshold' far from the current footstep or the foot does not reach the current footstep anymore or footstep angle is past this value.")]
        public float Locomotion_angleThreshold = 60f;

        [Tooltip("Multiplies angle of the center of mass - center of pressure vector. Larger value makes the character step sooner if losing balance.")]
        public float Locomotion_comAngleMlp = 1f;

        [Tooltip("Maximum magnitude of head/hand target velocity used in prediction.")]
        public float Locomotion_maxVelocity = 0.4f;

        [Tooltip("The amount of head/hand target velocity prediction.")]
        public float Locomotion_velocityFactor = 0.4f;

        [Range(0.9f, 1f), Tooltip("How much can a leg be extended before it is forced to step to another position? 1 means fully stretched.")]
        public float Locomotion_maxLegStretch = 1f;

        [Tooltip("The speed of lerping the root of the character towards the horizontal mid-point of the footsteps.")]
        public float Locomotion_rootSpeed = 20f;

        [Tooltip("The speed of steps.")]
        public float Locomotion_stepSpeed = 3f;

        [Tooltip("The height of the foot by normalized step progress (0 - 1).")]
        public AnimationCurve Locomotion_stepHeight;

        [Tooltip("The height offset of the heel by normalized step progress (0 - 1).")]
        public AnimationCurve Locomotion_heelHeight;

        [Range(0f, 180f), Tooltip("Rotates the foot while the leg is not stepping to relax the twist rotation of the leg if ideal rotation is past this angle.")]
        public float Locomotion_relaxLegTwistMinAngle = 20f;

        [Tooltip("The speed of rotating the foot while the leg is not stepping to relax the twist rotation of the leg.")]
        public float Locomotion_relaxLegTwistSpeed = 400f;

        [Tooltip("Interpolation mode of the step.")]
        public InterpolationMode Locomotion_stepInterpolation = InterpolationMode.InOutSine;

        [Tooltip("Offset for the approximated center of mass.")]
        public Vector3 Locomotion_offset;

        [Tooltip("Called when the left foot has finished a step.")]
        public UnityEvent Locomotion_onLeftFootstep = new UnityEvent();

        [Tooltip("Called when the right foot has finished a step")]
        public UnityEvent Locomotion_onRightFootstep = new UnityEvent();
    }
}
