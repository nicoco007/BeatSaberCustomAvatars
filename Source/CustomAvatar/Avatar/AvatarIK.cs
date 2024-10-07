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
        private TwistRelaxerV2[] _twistRelaxersV2;

        private IAvatarInput _input;
        private SpawnedAvatar _avatar;
        private ILogger<AvatarIK> _logger;
        private IKHelper _ikHelper;

        private bool _isLocomotionEnabled = false;
        private Pose _defaultRootPose;
        private Pose _previousParentPose;

        private bool _hasPelvisTarget;
        private bool _hasBothLegTargets;

        #region Behaviour Lifecycle

        protected void Awake()
        {
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in GetComponentsInChildren<BeatSaberDynamicBone::DynamicBone>())
            {
                if (!dynamicBone.enabled) continue;

                dynamicBone.enabled = false;

                _dynamicBones.Add(dynamicBone);
            }

            _twistRelaxers = GetComponentsInChildren<TwistRelaxer>();
            _twistRelaxersV2 = GetComponentsInChildren<TwistRelaxerV2>();
        }

        protected void OnEnable()
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

        protected void Start()
        {
            vrikManager = GetComponentInChildren<VRIKManager>();
            _defaultRootPose = new Pose(vrikManager.references_root.localPosition, vrikManager.references_root.localRotation);

            _vrik = _ikHelper.InitializeVRIK(vrikManager, transform);
            IKSolverVR solver = _vrik.solver;

            foreach (TwistRelaxer twistRelaxer in _twistRelaxers)
            {
                twistRelaxer.ik = _vrik;

                if (twistRelaxer.enabled)
                {
                    solver.OnPostUpdate += twistRelaxer.OnPostUpdate;
                }
            }

            foreach (TwistRelaxerV2 twistRelaxer in _twistRelaxersV2)
            {
                twistRelaxer.ik = _vrik;

                if (twistRelaxer.enabled)
                {
                    solver.OnPreUpdate += twistRelaxer.OnPreUpdate;
                    solver.OnPostUpdate += twistRelaxer.OnPostUpdate;
                }
            }

            solver.OnPreUpdate += OnPreUpdate;
            solver.OnPostUpdate += OnPostUpdate;

            if (vrikManager.solver_spine_maintainPelvisPosition > 0 && !_input.allowMaintainPelvisPosition)
            {
                _logger.LogWarning("solver.spine.maintainPelvisPosition > 0 is not recommended because it can cause strange pelvis rotation issues. To allow maintainPelvisPosition > 0, please set allowMaintainPelvisPosition to true for your avatar in the configuration file.");
                _vrik.solver.spine.maintainPelvisPosition = 0;
            }

            _input.inputChanged += OnInputChanged;

            UpdateSolverTargets();

            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.OnEnable();
                dynamicBone.Start();
            }
        }

        protected void OnDisable()
        {
            _vrik.enabled = false;
            _vrik.references.root.SetLocalPose(_defaultRootPose);
            _vrik.solver.FixTransforms();

            foreach (TwistRelaxerV2 twistRelaxer in _twistRelaxersV2)
            {
                twistRelaxer.FixTransforms();
            }

            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                dynamicBone.OnDisable();
            }
        }

        protected void OnDestroy()
        {
            IKSolverVR solver = _vrik.solver;
            solver.OnPreUpdate -= OnPreUpdate;
            solver.OnPostUpdate -= OnPostUpdate;

            _input.inputChanged -= OnInputChanged;
        }

        #endregion

        private void OnPreUpdate()
        {
            ApplyRootPose();

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

        // adapted from VRIKRootController to work when IK is rotated by parent
        private void ApplyRootPose()
        {
            // don't move the root if locomotion is disabled and both feet aren't being tracked
            // (i.e. keep previous behaviour of sticking to the origin when locomotion is disabled and we're missing one or more FBT trackers)
            if (!_isLocomotionEnabled && !_hasBothLegTargets)
            {
                return;
            }

            if (!_hasPelvisTarget)
            {
                return;
            }

            Transform pelvisTarget = _vrik.solver.spine.pelvisTarget;
            Transform root = _vrik.references.root;
            Transform parent = root.parent;
            bool hasParent = parent != null;

            Vector3 up = hasParent ? parent.up : Vector3.up;
            root.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(pelvisTarget.rotation * _avatar.prefab.pelvisRootForward, up), up);

            var position = Vector3.ProjectOnPlane(pelvisTarget.position - root.TransformVector(_avatar.prefab.pelvisRootOffset), up);

            if (hasParent)
            {
                position = parent.InverseTransformPoint(position);
            }

            root.localPosition = new Vector3(position.x, root.localPosition.y, position.z);
        }

        private void ApplyPlatformMotion()
        {
            Transform parent = _vrik.references.root.parent;

            if (parent == null)
            {
                return;
            }

            parent.GetPositionAndRotation(out Vector3 parentPosition, out Quaternion parentRotation);

            Vector3 deltaPosition = parentPosition - _previousParentPose.position;
            Quaternion deltaRotation = Quaternion.Inverse(_previousParentPose.rotation) * parentRotation;

            _vrik.solver.AddPlatformMotion(deltaPosition, deltaRotation, parentPosition);

            _previousParentPose = new Pose(parentPosition, parentRotation);
        }

        private void UpdateLocomotion()
        {
            if (_vrik == null || vrikManager == null)
            {
                return;
            }

            // don't enable locomotion if FBT is applied
            bool shouldEnableLocomotion = _isLocomotionEnabled && !(_hasPelvisTarget && _hasBothLegTargets);

            if (shouldEnableLocomotion)
            {
                _vrik.solver.locomotion.weight = vrikManager.solver_locomotion_weight;
            }
            else
            {
                _vrik.solver.locomotion.weight = 0;
                _vrik.references.root.SetLocalPose(_defaultRootPose);
            }
        }

        private void OnInputChanged()
        {
            UpdateSolverTargets();
        }

        private void UpdateSolverTargets()
        {
            if (_vrik == null || vrikManager == null)
            {
                return;
            }

            _logger.LogTrace("Updating solver targets");

            UpdateSolverTarget(DeviceUse.Head, ref _vrik.solver.spine.headTarget, ref _vrik.solver.spine.positionWeight, ref _vrik.solver.spine.rotationWeight);
            UpdateSolverTarget(DeviceUse.LeftHand, ref _vrik.solver.leftArm.target, ref _vrik.solver.leftArm.positionWeight, ref _vrik.solver.leftArm.rotationWeight);
            UpdateSolverTarget(DeviceUse.RightHand, ref _vrik.solver.rightArm.target, ref _vrik.solver.rightArm.positionWeight, ref _vrik.solver.rightArm.rotationWeight);

            _hasBothLegTargets = true;
            _hasBothLegTargets &= UpdateSolverTarget(DeviceUse.LeftFoot, ref _vrik.solver.leftLeg.target, ref _vrik.solver.leftLeg.positionWeight, ref _vrik.solver.leftLeg.rotationWeight);
            _hasBothLegTargets &= UpdateSolverTarget(DeviceUse.RightFoot, ref _vrik.solver.rightLeg.target, ref _vrik.solver.rightLeg.positionWeight, ref _vrik.solver.rightLeg.rotationWeight);

            if (_hasPelvisTarget = UpdateSolverTarget(DeviceUse.Waist, ref _vrik.solver.spine.pelvisTarget, ref _vrik.solver.spine.pelvisPositionWeight, ref _vrik.solver.spine.pelvisRotationWeight))
            {
                _vrik.solver.plantFeet = false;
            }
            else
            {
                _vrik.solver.plantFeet = vrikManager.solver_plantFeet;
            }

            UpdateLocomotion();
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
