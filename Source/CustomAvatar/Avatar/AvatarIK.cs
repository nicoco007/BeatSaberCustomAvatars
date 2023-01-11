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

extern alias BeatSaberDynamicBone;
extern alias BeatSaberFinalIK;

using System;
using System.Collections.Generic;
using System.Reflection;
using BeatSaberFinalIK::RootMotion.FinalIK;
using CustomAvatar.Logging;
using CustomAvatar.Scripts;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using IPA.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    public class AvatarIK : MonoBehaviour
    {
        public bool isLocomotionEnabled
        {
            get => _isLocomotionEnabled;
            set
            {
                _isLocomotionEnabled = value;
                UpdateLocomotion();
            }
        }

        [Obsolete]
        public bool isCalibrationModeEnabled
        {
            get => _isCalibrationModeEnabled;
            set
            {
                _isCalibrationModeEnabled = value;
                UpdateSolverTargets();
            }
        }

        private VRIK _vrik;
        private VRIKManager _vrikManager;

        private readonly List<BeatSaberDynamicBone::DynamicBone> _dynamicBones = new List<BeatSaberDynamicBone::DynamicBone>();

        // create delegates for dynamic bones private methods (more efficient than continuously calling Invoke)
        private static readonly Action<BeatSaberDynamicBone::DynamicBone> kDynamicBoneOnEnableDelegate = MethodAccessor<BeatSaberDynamicBone::DynamicBone, Action<BeatSaberDynamicBone::DynamicBone>>.GetDelegate("OnEnable");
        private static readonly Action<BeatSaberDynamicBone::DynamicBone> kDynamicBoneStartDelegate = MethodAccessor<BeatSaberDynamicBone::DynamicBone, Action<BeatSaberDynamicBone::DynamicBone>>.GetDelegate("Start");
        private static readonly Action<BeatSaberDynamicBone::DynamicBone> kDynamicBonePreUpdateDelegate = MethodAccessor<BeatSaberDynamicBone::DynamicBone, Action<BeatSaberDynamicBone::DynamicBone>>.GetDelegate("PreUpdate");
        private static readonly Action<BeatSaberDynamicBone::DynamicBone> kDynamicBoneLateUpdateDelegate = MethodAccessor<BeatSaberDynamicBone::DynamicBone, Action<BeatSaberDynamicBone::DynamicBone>>.GetDelegate("LateUpdate");

        private static readonly MethodInfo kTwistRelaxerOnPostUpdate = typeof(TwistRelaxer).GetMethod("OnPostUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

        private IAvatarInput _input;
        private SpawnedAvatar _avatar;
        private ILogger<AvatarIK> _logger;
        private IKHelper _ikHelper;

        private bool _isCalibrationModeEnabled = false;
        private bool _isLocomotionEnabled = false;
        private Pose _previousParentPose;

        #region Behaviour Lifecycle

        private void Awake()
        {
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in GetComponentsInChildren<BeatSaberDynamicBone::DynamicBone>())
            {
                if (!dynamicBone.enabled) continue;

                dynamicBone.enabled = false;

                _dynamicBones.Add(dynamicBone);
            }
        }

        [Inject]
        [UsedImplicitly]
        private void Construct(IAvatarInput input, SpawnedAvatar avatar, ILogger<AvatarIK> logger, IKHelper ikHelper)
        {
            _input = input;
            _avatar = avatar;
            _logger = logger;
            _ikHelper = ikHelper;

            _logger.name = _avatar.prefab.descriptor.name;
        }

        private void Start()
        {
            _vrikManager = GetComponentInChildren<VRIKManager>();

            _vrik = _ikHelper.InitializeVRIK(_vrikManager, transform);
            IKSolver solver = _vrik.GetIKSolver();

            foreach (TwistRelaxer twistRelaxer in GetComponentsInChildren<TwistRelaxer>())
            {
                twistRelaxer.ik = _vrik;
                solver.OnPostUpdate += (IKSolver.UpdateDelegate)kTwistRelaxerOnPostUpdate.CreateDelegate(typeof(IKSolver.UpdateDelegate), twistRelaxer);
            }

            foreach (UpperArmRelaxer upperArmRelaxer in GetComponentsInChildren<UpperArmRelaxer>())
            {
                upperArmRelaxer.ik = _vrik;
                solver.OnPreUpdate += upperArmRelaxer.OnPreUpdate;
                solver.OnPostUpdate += upperArmRelaxer.OnPostUpdate;
            }

            solver.OnPreUpdate += OnPreUpdate;
            solver.OnPostUpdate += OnPostUpdate;

            if (_vrikManager.solver_spine_maintainPelvisPosition > 0 && !_input.allowMaintainPelvisPosition)
            {
                _logger.LogWarning("solver.spine.maintainPelvisPosition > 0 is not recommended because it can cause strange pelvis rotation issues. To allow maintainPelvisPosition > 0, please set allowMaintainPelvisPosition to true for your avatar in the configuration file.");
                _vrik.solver.spine.maintainPelvisPosition = 0;
            }

            _input.inputChanged += OnInputChanged;

            UpdateLocomotion();
            UpdateSolverTargets();

            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                kDynamicBoneOnEnableDelegate(dynamicBone);
                kDynamicBoneStartDelegate(dynamicBone);
            }
        }

        private void OnDestroy()
        {
            IKSolver solver = _vrik.GetIKSolver();
            solver.OnPreUpdate -= OnPreUpdate;
            solver.OnPostUpdate -= OnPostUpdate;

            _input.inputChanged -= OnInputChanged;
        }

        #endregion

        private void ApplyPlatformMotion()
        {
            Transform parent = transform.parent;

            if (!parent) return;

            Vector3 deltaPosition = parent.position - _previousParentPose.position;
            Quaternion deltaRotation = parent.rotation * Quaternion.Inverse(_previousParentPose.rotation);

            _vrik.solver.AddPlatformMotion(deltaPosition, deltaRotation, parent.position);
            _previousParentPose = new Pose(parent.position, parent.rotation);
        }

        private void OnPreUpdate()
        {
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                kDynamicBonePreUpdateDelegate(dynamicBone);
            }

            ApplyPlatformMotion();
        }

        private void OnPostUpdate()
        {
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                kDynamicBoneLateUpdateDelegate(dynamicBone);
            }
        }

        private void UpdateLocomotion()
        {
            if (!_vrik || !_vrikManager) return;

            _vrik.solver.locomotion.weight = _isLocomotionEnabled ? _vrikManager.solver_locomotion_weight : 0;

            if (_vrik.references.root)
            {
                _vrik.references.root.transform.localPosition = Vector3.zero;
            }
        }

        private void OnInputChanged()
        {
            UpdateSolverTargets();
        }

        private void UpdateSolverTargets()
        {
            if (!_vrik || !_vrikManager) return;

            _logger.LogInformation("Updating solver targets");

            _vrik.solver.spine.headTarget = _vrikManager.solver_spine_headTarget;
            _vrik.solver.leftArm.target = _vrikManager.solver_leftArm_target;
            _vrik.solver.rightArm.target = _vrikManager.solver_rightArm_target;

            if (_input.TryGetPose(DeviceUse.LeftFoot, out _) || _isCalibrationModeEnabled)
            {
                _vrik.solver.leftLeg.target = _vrikManager.solver_leftLeg_target;
                _vrik.solver.leftLeg.positionWeight = _vrikManager.solver_leftLeg_positionWeight;
                _vrik.solver.leftLeg.rotationWeight = _vrikManager.solver_leftLeg_rotationWeight;
            }
            else
            {
                _vrik.solver.leftLeg.target = null;
                _vrik.solver.leftLeg.positionWeight = 0;
                _vrik.solver.leftLeg.rotationWeight = 0;
            }

            if (_input.TryGetPose(DeviceUse.RightFoot, out _) || _isCalibrationModeEnabled)
            {
                _vrik.solver.rightLeg.target = _vrikManager.solver_rightLeg_target;
                _vrik.solver.rightLeg.positionWeight = _vrikManager.solver_rightLeg_positionWeight;
                _vrik.solver.rightLeg.rotationWeight = _vrikManager.solver_rightLeg_rotationWeight;
            }
            else
            {
                _vrik.solver.rightLeg.target = null;
                _vrik.solver.rightLeg.positionWeight = 0;
                _vrik.solver.rightLeg.rotationWeight = 0;
            }

            if (_input.TryGetPose(DeviceUse.Waist, out _) || _isCalibrationModeEnabled)
            {
                _vrik.solver.spine.pelvisTarget = _vrikManager.solver_spine_pelvisTarget;
                _vrik.solver.spine.pelvisPositionWeight = _vrikManager.solver_spine_pelvisPositionWeight;
                _vrik.solver.spine.pelvisRotationWeight = _vrikManager.solver_spine_pelvisRotationWeight;
                _vrik.solver.plantFeet = false;
            }
            else
            {
                _vrik.solver.spine.pelvisTarget = null;
                _vrik.solver.spine.pelvisPositionWeight = 0;
                _vrik.solver.spine.pelvisRotationWeight = 0;
                _vrik.solver.plantFeet = _vrikManager.solver_plantFeet;
            }
        }
    }
}
