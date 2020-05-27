extern alias BeatSaberFinalIK;
extern alias BeatSaberDynamicBone;

using System;
using System.Collections.Generic;
using BeatSaberFinalIK::RootMotion.FinalIK;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar.Avatar
{
    public class AvatarIK : BodyAwareBehaviour
    {
        private VRIK _vrik;
        private VRIKManager _vrikManager;
        private Settings.AvatarSpecificSettings _avatarSettings;

        private bool _fixTransforms;

        private List<TwistRelaxer> _twistRelaxers = new List<TwistRelaxer>();

        private BeatSaberDynamicBone::DynamicBone[] _dynamicBones;

        private Action<BeatSaberDynamicBone::DynamicBone> _preUpdateDelegate;
        private Action<BeatSaberDynamicBone::DynamicBone, float> _updateDynamicBonesDelegate;
        
        private AvatarInput _input;
        private LoadedAvatar _avatar;
        private Settings _settings;
        private ILogger _logger;

        private bool _isCalibrationModeEnabled = false;
        
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

        [Inject]
        private void Inject(AvatarInput input, LoadedAvatar avatar, Settings settings, ILoggerProvider loggerProvider)
        {
            _input = input;
            _avatar = avatar;
            _settings = settings;
            _logger = loggerProvider.CreateLogger<AvatarIK>(_avatar.descriptor.name);
        }

        protected override void Start()
        {
            if (_input == null)
            {
                Destroy(this);
                throw new ArgumentNullException(nameof(_input));
            }

            if (_avatar == null)
            {
                Destroy(this);
                throw new ArgumentNullException(nameof(_avatar));
            }

            base.Start();

            _avatarSettings = _settings.GetAvatarSettings(_avatar.fullPath);

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

            if (_vrikManager.solver_spine_maintainPelvisPosition > 0 && !_avatarSettings.allowMaintainPelvisPosition)
            {
                _logger.Warning("solver.spine.maintainPelvisPosition > 0 is not recommended because it can cause strange pelvis rotation issues. To allow maintainPelvisPosition > 0, please set allowMaintainPelvisPosition to true for your avatar in the configuration file.");
            }
            
            _input.inputChanged += OnInputChanged;
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
            _vrikManager.referencesUpdated -= OnReferencesUpdated;
            _input.inputChanged -= OnInputChanged;
        }

        // ReSharper restore UnusedMember.Local
        #pragma warning restore IDE0051
        #endregion

        internal void EnableCalibrationMode()
        {
            _isCalibrationModeEnabled = true;
            UpdateSolverTargets();
        }

        internal void DisableCalibrationMode()
        {
            _isCalibrationModeEnabled = false;
            UpdateSolverTargets();
        }

        private void OnInputChanged()
        {
            _logger.Info("Tracking device change detected, updating VRIK references");
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
            if (target || !parent) return target;

            Transform newTarget = new GameObject().transform;

            newTarget.SetParent(parent, false);
            newTarget.position = reference.position;
            newTarget.rotation = reference.rotation;

            _logger.Info($"Created IK target for '{parent.name}'");

            return newTarget;
        }

        private void UpdateSolverTargets()
        {
            _logger.Info("Updating solver targets");

            _vrik.solver.spine.headTarget  = _vrikManager.solver_spine_headTarget;
            _vrik.solver.leftArm.target    = _vrikManager.solver_leftArm_target;
            _vrik.solver.rightArm.target   = _vrikManager.solver_rightArm_target;

            if (_vrikManager.solver_spine_maintainPelvisPosition > 0 && !_avatarSettings.allowMaintainPelvisPosition)
            {
                _vrik.solver.spine.maintainPelvisPosition = 0;
            }

            _logger.Info("Updating conditional solver targets");

            if (_input.TryGetLeftFootPose(out _) || _isCalibrationModeEnabled)
            {
                _logger.Trace("Left foot enabled");
                _vrik.solver.leftLeg.target = _vrikManager.solver_leftLeg_target;
                _vrik.solver.leftLeg.positionWeight = _vrikManager.solver_leftLeg_positionWeight;
                _vrik.solver.leftLeg.rotationWeight = _vrikManager.solver_leftLeg_rotationWeight;
            }
            else
            {
                _logger.Trace("Left foot disabled");
                _vrik.solver.leftLeg.target = null;
                _vrik.solver.leftLeg.positionWeight = 0;
                _vrik.solver.leftLeg.rotationWeight = 0;
            }

            if (_input.TryGetRightFootPose(out _) || _isCalibrationModeEnabled)
            {
                _logger.Trace("Right foot enabled");
                _vrik.solver.rightLeg.target = _vrikManager.solver_rightLeg_target;
                _vrik.solver.rightLeg.positionWeight = _vrikManager.solver_rightLeg_positionWeight;
                _vrik.solver.rightLeg.rotationWeight = _vrikManager.solver_rightLeg_rotationWeight;
            }
            else
            {
                _logger.Trace("Right foot disabled");
                _vrik.solver.rightLeg.target = null;
                _vrik.solver.rightLeg.positionWeight = 0;
                _vrik.solver.rightLeg.rotationWeight = 0;
            }

            if (_input.TryGetWaistPose(out _) || _isCalibrationModeEnabled)
            {
                _logger.Trace("Pelvis enabled");
                _vrik.solver.spine.pelvisTarget = _vrikManager.solver_spine_pelvisTarget;
                _vrik.solver.spine.pelvisPositionWeight = _vrikManager.solver_spine_pelvisPositionWeight;
                _vrik.solver.spine.pelvisRotationWeight = _vrikManager.solver_spine_pelvisRotationWeight;
                _vrik.solver.plantFeet = false;
            }
            else
            {
                _logger.Trace("Pelvis disabled");
                _vrik.solver.spine.pelvisTarget = null;
                _vrik.solver.spine.pelvisPositionWeight = 0;
                _vrik.solver.spine.pelvisRotationWeight = 0;
                _vrik.solver.plantFeet = _vrikManager.solver_plantFeet;
            }
        }
    }
}
