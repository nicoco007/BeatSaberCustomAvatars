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
extern alias BeatSaberDynamicBone;

using System;
using System.Collections.Generic;
using System.Reflection;
using BeatSaberFinalIK::RootMotion.FinalIK;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    public class AvatarIK : MonoBehaviour
    {
        private VRIK _vrik;
        private VRIKManager _vrikManager;

        private bool _fixTransforms;

        private List<TwistRelaxer> _twistRelaxers = new List<TwistRelaxer>();

        private BeatSaberDynamicBone::DynamicBone[] _dynamicBones;

        private Action<BeatSaberDynamicBone::DynamicBone> _preUpdateDelegate;
        private Action<BeatSaberDynamicBone::DynamicBone, float> _updateDynamicBonesDelegate;
        private FieldInfo _weightField;
        
        private IAvatarInput _input;
        private SpawnedAvatar _avatar;
        private ILogger<AvatarIK> _logger;
        private IKHelper _ikHelper;

        private bool _isCalibrationModeEnabled = false;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        private void Awake()
        {
            // create delegates for dynamic bones private methods (more efficient than continuously calling Invoke)
            _preUpdateDelegate = typeof(BeatSaberDynamicBone::DynamicBone).CreatePrivateMethodDelegate<Action<BeatSaberDynamicBone::DynamicBone>>("PreUpdate");
            _updateDynamicBonesDelegate = typeof(BeatSaberDynamicBone::DynamicBone).CreatePrivateMethodDelegate<Action<BeatSaberDynamicBone::DynamicBone, float>>("UpdateDynamicBones");
            _weightField = typeof(BeatSaberDynamicBone::DynamicBone).GetField("m_Weight", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (TwistRelaxer twistRelaxer in GetComponentsInChildren<TwistRelaxer>())
            {
                if (twistRelaxer.enabled)
                {
                    twistRelaxer.enabled = false;
                    _twistRelaxers.Add(twistRelaxer);
                }
            }
        }

        [Inject]
        private void Inject(IAvatarInput input, SpawnedAvatar avatar, ILoggerProvider loggerProvider, IKHelper ikHelper)
        {
            _input = input;
            _avatar = avatar;
            _logger = loggerProvider.CreateLogger<AvatarIK>(_avatar.avatar.descriptor.name);
            _ikHelper = ikHelper;
        }

        private void Start()
        {
            _vrikManager = GetComponentInChildren<VRIKManager>();
            _dynamicBones = GetComponentsInChildren<BeatSaberDynamicBone::DynamicBone>();

            _vrik = _ikHelper.InitializeVRIK(_vrikManager, transform);

            _fixTransforms = _vrikManager.fixTransforms;
            _vrik.fixTransforms = false; // FixTransforms is manually called in Update

            foreach (TwistRelaxer twistRelaxer in _twistRelaxers)
            {
                twistRelaxer.ik = _vrik;
                twistRelaxer.enabled = true;
            }

            if (_vrikManager.solver_spine_maintainPelvisPosition > 0 && !_input.allowMaintainPelvisPosition)
            {
                _logger.Warning("solver.spine.maintainPelvisPosition > 0 is not recommended because it can cause strange pelvis rotation issues. To allow maintainPelvisPosition > 0, please set allowMaintainPelvisPosition to true for your avatar in the configuration file.");
                _vrik.solver.spine.maintainPelvisPosition = 0;
            }

            _input.inputChanged += OnInputChanged;

            UpdateSolverTargets();
            SetLocomotionEnabled(_avatar.isLocomotionEnabled);
        }

        private void Update()
        {
            if (_fixTransforms)
            {
                _vrik.solver.FixTransforms();
            }

            // DynamicBones PreUpdate
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                if (!dynamicBone.enabled) continue;

                // setting m_Weight prevents the integrated calls to PreUpdate and UpdateDynamicBones from taking effect
                _weightField.SetValue(dynamicBone, 1);
                _preUpdateDelegate(dynamicBone);
                _weightField.SetValue(dynamicBone, 0);
            }
        }

        private void LateUpdate()
        {
            // VRIK must run before dynamic bones
            _vrik.UpdateSolverExternal();

            // apply dynamic bones
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                if (!dynamicBone.enabled) continue;

                // setting m_Weight prevents the integrated calls to PreUpdate and UpdateDynamicBones from taking effect
                _weightField.SetValue(dynamicBone, 1);
                _updateDynamicBonesDelegate(dynamicBone, Time.deltaTime);
                _weightField.SetValue(dynamicBone, 0);
            }
        }

        private void OnDestroy()
        {
            _input.inputChanged -= OnInputChanged;
        }

        #pragma warning restore IDE0051
        #endregion

        internal void SetLocomotionEnabled(bool enabled)
        {
            if (enabled)
            {
                _vrik.solver.locomotion.weight = _vrikManager.solver_locomotion_weight;
            }
            else
            {
                _vrik.solver.locomotion.weight = 0;
            }
        }

        internal void SetCalibrationModeEnabled(bool enabled)
        {
            _isCalibrationModeEnabled = enabled;
            UpdateSolverTargets();
        }

        private void OnInputChanged()
        {
            UpdateSolverTargets();
        }

        private void UpdateSolverTargets()
        {
            _logger.Info("Updating solver targets");

            _vrik.solver.spine.headTarget  = _vrikManager.solver_spine_headTarget;
            _vrik.solver.leftArm.target    = _vrikManager.solver_leftArm_target;
            _vrik.solver.rightArm.target   = _vrikManager.solver_rightArm_target;

            if (_input.TryGetLeftFootPose(out _) || _isCalibrationModeEnabled)
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

            if (_input.TryGetRightFootPose(out _) || _isCalibrationModeEnabled)
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

            if (_input.TryGetWaistPose(out _) || _isCalibrationModeEnabled)
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
