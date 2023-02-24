//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System.Collections.Generic;
using BeatSaberFinalIK::RootMotion.FinalIK;
using CustomAvatar.Logging;
using CustomAvatar.Scripts;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    [DisallowMultipleComponent]
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

        internal VRIKManager vrikManager { get; private set; }

        private VRIK _vrik;

        private readonly List<BeatSaberDynamicBone::DynamicBone> _dynamicBones = new();
        private TwistRelaxer[] _twistRelaxers;
        private UpperArmRelaxer[] _upperArmRelaxers;

        private IAvatarInput _input;
        private SpawnedAvatar _avatar;
        private ILogger<AvatarIK> _logger;
        private IKHelper _ikHelper;

        private bool _isLocomotionEnabled = false;
        private Pose _defaultRootPose;
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

            _twistRelaxers = GetComponentsInChildren<TwistRelaxer>();
            _upperArmRelaxers = GetComponentsInChildren<UpperArmRelaxer>();
        }

        private void OnEnable()
        {
            if (_vrik != null)
            {
                _vrik.enabled = true;
            }

            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.OnEnable();
            }
        }

        [Inject]
        [UsedImplicitly]
        private void Construct(IAvatarInput input, SpawnedAvatar avatar, ILoggerFactory loggerFactory, IKHelper ikHelper)
        {
            _input = input;
            _avatar = avatar;
            _logger = loggerFactory.CreateLogger<AvatarIK>(_avatar.prefab.descriptor.name);
            _ikHelper = ikHelper;
        }

        private void Start()
        {
            vrikManager = GetComponentInChildren<VRIKManager>();
            _defaultRootPose = new Pose(vrikManager.references_root.localPosition, vrikManager.references_root.localRotation);

            _vrik = _ikHelper.InitializeVRIK(vrikManager, transform);
            IKSolver solver = _vrik.GetIKSolver();

            foreach (TwistRelaxer twistRelaxer in _twistRelaxers)
            {
                twistRelaxer.ik = _vrik;

                if (twistRelaxer.enabled)
                {
                    solver.OnPostUpdate += twistRelaxer.OnPostUpdate;
                }
            }

            foreach (UpperArmRelaxer upperArmRelaxer in _upperArmRelaxers)
            {
                upperArmRelaxer.ik = _vrik;

                if (upperArmRelaxer.enabled)
                {
                    solver.OnPreUpdate += upperArmRelaxer.OnPreUpdate;
                    solver.OnPostUpdate += upperArmRelaxer.OnPostUpdate;
                }
            }

            solver.OnPostUpdate += OnPostUpdate;

            if (vrikManager.solver_spine_maintainPelvisPosition > 0 && !_input.allowMaintainPelvisPosition)
            {
                _logger.LogWarning("solver.spine.maintainPelvisPosition > 0 is not recommended because it can cause strange pelvis rotation issues. To allow maintainPelvisPosition > 0, please set allowMaintainPelvisPosition to true for your avatar in the configuration file.");
                _vrik.solver.spine.maintainPelvisPosition = 0;
            }

            _input.inputChanged += OnInputChanged;

            UpdateLocomotion();
            UpdateSolverTargets();

            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.OnEnable();
                dynamicBone.Start();
            }
        }

        private void OnDisable()
        {
            _vrik.enabled = false;
            _vrik.references.root.localPosition = _defaultRootPose.position;
            _vrik.references.root.localRotation = _defaultRootPose.rotation;
            _vrik.solver.FixTransforms();

            foreach (UpperArmRelaxer relaxer in _upperArmRelaxers)
            {
                relaxer.FixTransforms();
            }

            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.OnDisable();
            }
        }

        private void OnDestroy()
        {
            IKSolver solver = _vrik.GetIKSolver();
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

        private void Update()
        {
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.Update();
            }

            ApplyPlatformMotion();
        }

        private void OnPostUpdate()
        {
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.LateUpdate();
            }
        }

        private void UpdateLocomotion()
        {
            if (!_vrik || !vrikManager) return;

            _vrik.solver.locomotion.weight = _isLocomotionEnabled ? vrikManager.solver_locomotion_weight : 0;

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
            if (_vrik == null || vrikManager == null) return;

            _logger.LogTrace("Updating solver targets");

            UpdateSolverTarget(DeviceUse.Head, ref _vrik.solver.spine.headTarget, ref _vrik.solver.spine.positionWeight, ref _vrik.solver.spine.rotationWeight);
            UpdateSolverTarget(DeviceUse.LeftHand, ref _vrik.solver.leftArm.target, ref _vrik.solver.leftArm.positionWeight, ref _vrik.solver.leftArm.rotationWeight);
            UpdateSolverTarget(DeviceUse.RightHand, ref _vrik.solver.rightArm.target, ref _vrik.solver.rightArm.positionWeight, ref _vrik.solver.rightArm.rotationWeight);
            UpdateSolverTarget(DeviceUse.LeftFoot, ref _vrik.solver.leftLeg.target, ref _vrik.solver.leftLeg.positionWeight, ref _vrik.solver.leftLeg.rotationWeight);
            UpdateSolverTarget(DeviceUse.RightFoot, ref _vrik.solver.rightLeg.target, ref _vrik.solver.rightLeg.positionWeight, ref _vrik.solver.rightLeg.rotationWeight);

            if (UpdateSolverTarget(DeviceUse.Waist, ref _vrik.solver.spine.pelvisTarget, ref _vrik.solver.spine.pelvisPositionWeight, ref _vrik.solver.spine.pelvisRotationWeight))
            {
                _vrik.solver.plantFeet = false;
            }
            else
            {
                _vrik.solver.plantFeet = vrikManager.solver_plantFeet;
            }
        }

        private bool UpdateSolverTarget(DeviceUse deviceUse, ref Transform target, ref float positionWeight, ref float rotationWeight)
        {
            if (_input.TryGetTransform(deviceUse, out Transform transform))
            {
                target = transform;
                positionWeight = 1;
                rotationWeight = 1;
                return true;
            }
            else
            {
                target = null;
                positionWeight = 0;
                rotationWeight = 0;
                return false;
            }
        }
    }
}
