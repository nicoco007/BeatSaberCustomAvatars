extern alias BeatSaberFinalIK;
extern alias BeatSaberDynamicBone;

using System;
using System.Collections.Generic;
using BeatSaberFinalIK::RootMotion.FinalIK;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    internal class AvatarIK : BodyAwareBehaviour
    {
        public AvatarInput input;
        public LoadedAvatar avatar;

        private VRIK _vrik;
        private VRIKManager _vrikManager;
        private Settings.AvatarSpecificSettings _avatarSettings;

        private bool _fixTransforms;

        private List<TwistRelaxer> _twistRelaxers = new List<TwistRelaxer>();

        private BeatSaberDynamicBone::DynamicBone[] _dynamicBones;

        private Action<BeatSaberDynamicBone::DynamicBone> _preUpdateDelegate;
        private Action<BeatSaberDynamicBone::DynamicBone, float> _updateDynamicBonesDelegate;
        
        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local
        
        private void Awake()
        {
            // create delegates for dynamic bones private methods (more efficient than continuously calling Invoke)
            _preUpdateDelegate = typeof(BeatSaberDynamicBone::DynamicBone).CreatePrivateMethodDelegate<Action<BeatSaberDynamicBone::DynamicBone>>("PreUpdate");
            _updateDynamicBonesDelegate = typeof(BeatSaberDynamicBone::DynamicBone).CreatePrivateMethodDelegate<Action<BeatSaberDynamicBone::DynamicBone, float>>("UpdateDynamicBones");

            foreach (TwistRelaxer twistRelaxer in GetComponentsInChildren<TwistRelaxer>())
            {
                if (twistRelaxer.enabled)
                {
                    twistRelaxer.enabled = false;
                    _twistRelaxers.Add(twistRelaxer);
                }
            }
        }

        protected override void Start()
        {
            if (input == null)
            {
                Destroy(this);
                throw new ArgumentNullException(nameof(input));
            }

            if (avatar == null)
            {
                Destroy(this);
                throw new ArgumentNullException(nameof(avatar));
            }

            base.Start();

            _avatarSettings = SettingsManager.settings.GetAvatarSettings(avatar.fullPath);

            if (_avatarSettings == null)
            {

            }

            _vrikManager = GetComponentInChildren<VRIKManager>();
            _dynamicBones = GetComponentsInChildren<BeatSaberDynamicBone::DynamicBone>();

            _vrik = _vrikManager.vrik;
            _vrik.fixTransforms = false;

            _fixTransforms = _vrikManager.fixTransforms;

            _vrikManager.referencesUpdated += OnReferencesUpdated;
            
            OnReferencesUpdated();
            
            foreach (TwistRelaxer twistRelaxer in _twistRelaxers)
            {
                twistRelaxer.ik = _vrik;
                twistRelaxer.enabled = true;
            }
            
            input.inputChanged += OnInputChanged;
        }

        private void Update()
        {
            if (_vrik && _fixTransforms)
            {
                _vrik.solver.FixTransforms();
            }

            // DynamicBones PreUpdate
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                if (!dynamicBone.enabled) continue;

                // setting m_Weight prevents the integrated calls to PreUpdate and UpdateDynamicBones from taking effect
                dynamicBone.SetPrivateField("m_Weight", 1);
                _preUpdateDelegate(dynamicBone);
                dynamicBone.SetPrivateField("m_Weight", 0);
            }
        }

        private void LateUpdate()
        {
            // VRIK must run before dynamic bones
            if (_vrik)
            {
                _vrik.UpdateSolverExternal();
            }

            // apply dynamic bones
            foreach (BeatSaberDynamicBone::DynamicBone dynamicBone in _dynamicBones)
            {
                if (!dynamicBone.enabled) continue;

                // setting m_Weight prevents the integrated calls to PreUpdate and UpdateDynamicBones from taking effect
                dynamicBone.SetPrivateField("m_Weight", 1);
                _updateDynamicBonesDelegate(dynamicBone, Time.deltaTime);
                dynamicBone.SetPrivateField("m_Weight", 0);
            }
        }

        private void OnDestroy()
        {
            input.inputChanged -= OnInputChanged;
        }

        // ReSharper restore UnusedMember.Local
        #pragma warning restore IDE0051
        #endregion

        public void EnableCalibrationMode()
        {
            UpdateSolverTargets(true);
        }

        public void DisableCalibrationMode()
        {
            UpdateSolverTargets();
        }

        private void OnInputChanged()
        {
            Plugin.logger.Info("Tracking device change detected, updating VRIK references");
            UpdateSolverTargets();
        }

        private void OnReferencesUpdated()
        {
            CreateTargetsIfMissing();
            UpdateSolverTargets();
        }

        private void CreateTargetsIfMissing()
        {
            _vrikManager.solver_spine_headTarget   = CreateTargetIfMissing(_vrikManager.solver_spine_headTarget,   _vrik.references.head,      head);
            _vrikManager.solver_leftArm_target     = CreateTargetIfMissing(_vrikManager.solver_leftArm_target,     _vrik.references.leftHand,  leftHand);
            _vrikManager.solver_rightArm_target    = CreateTargetIfMissing(_vrikManager.solver_rightArm_target,    _vrik.references.rightHand, rightHand);
            _vrikManager.solver_spine_pelvisTarget = CreateTargetIfMissing(_vrikManager.solver_spine_pelvisTarget, _vrik.references.pelvis,    pelvis);
            _vrikManager.solver_leftLeg_target     = CreateTargetIfMissing(_vrikManager.solver_leftLeg_target,     _vrik.references.leftToes ?? _vrik.references.leftFoot,  leftLeg);
            _vrikManager.solver_rightLeg_target    = CreateTargetIfMissing(_vrikManager.solver_rightLeg_target,    _vrik.references.rightToes ?? _vrik.references.rightFoot, rightLeg);
        }

        private Transform CreateTargetIfMissing(Transform target, Transform reference, Transform parent)
        {
            if (target || !parent) return target;

            Transform newTarget = new GameObject().transform;

            newTarget.SetParent(parent, false);
            newTarget.position = reference.position;
            newTarget.rotation = reference.rotation;

            Plugin.logger.Info($"Created IK target for '{parent.name}'");

            return newTarget;
        }

        private void UpdateSolverTargets(bool forceEnableAll = false)
        {
            Plugin.logger.Info("Updating solver targets");

            _vrik.solver.spine.headTarget  = _vrikManager.solver_spine_headTarget;
            _vrik.solver.leftArm.target    = _vrikManager.solver_leftArm_target;
            _vrik.solver.rightArm.target   = _vrikManager.solver_rightArm_target;

            if (_vrikManager.solver_spine_maintainPelvisPosition > 0 && !_avatarSettings.allowMaintainPelvisPosition)
            {
                Plugin.logger.Warn("maintainPelvisPosition > 0 is not recommended because it can cause strange pelvis rotation issues. To allow maintainPelvisPosition > 0, please set allowMaintainPelvisPosition to true for your avatar in the configuration file.");
                _vrik.solver.spine.maintainPelvisPosition = 0;
            }

            Plugin.logger.Info("Updating conditional solver targets");

            if (input.TryGetLeftFootPose(out _) || forceEnableAll)
            {
                Plugin.logger.Debug("Left foot enabled");
                _vrik.solver.leftLeg.target = _vrikManager.solver_leftLeg_target;
                _vrik.solver.leftLeg.positionWeight = _vrikManager.solver_leftLeg_positionWeight;
                _vrik.solver.leftLeg.rotationWeight = _vrikManager.solver_leftLeg_rotationWeight;
            }
            else
            {
                Plugin.logger.Debug("Left foot disabled");
                _vrik.solver.leftLeg.target = null;
                _vrik.solver.leftLeg.positionWeight = 0;
                _vrik.solver.leftLeg.rotationWeight = 0;
            }

            if (input.TryGetRightFootPose(out _) || forceEnableAll)
            {
                Plugin.logger.Debug("Right foot enabled");
                _vrik.solver.rightLeg.target = _vrikManager.solver_rightLeg_target;
                _vrik.solver.rightLeg.positionWeight = _vrikManager.solver_rightLeg_positionWeight;
                _vrik.solver.rightLeg.rotationWeight = _vrikManager.solver_rightLeg_rotationWeight;
            }
            else
            {
                Plugin.logger.Debug("Right foot disabled");
                _vrik.solver.rightLeg.target = null;
                _vrik.solver.rightLeg.positionWeight = 0;
                _vrik.solver.rightLeg.rotationWeight = 0;
            }

            if (input.TryGetWaistPose(out _) || forceEnableAll)
            {
                Plugin.logger.Debug("Pelvis enabled");
                _vrik.solver.spine.pelvisTarget = _vrikManager.solver_spine_pelvisTarget;
                _vrik.solver.spine.pelvisPositionWeight = _vrikManager.solver_spine_pelvisPositionWeight;
                _vrik.solver.spine.pelvisRotationWeight = _vrikManager.solver_spine_pelvisRotationWeight;
                _vrik.solver.plantFeet = false;
            }
            else
            {
                Plugin.logger.Debug("Pelvis disabled");
                _vrik.solver.spine.pelvisTarget = null;
                _vrik.solver.spine.pelvisPositionWeight = 0;
                _vrik.solver.spine.pelvisRotationWeight = 0;
                _vrik.solver.plantFeet = _vrikManager.solver_plantFeet;
            }
        }
    }
}
