extern alias BeatSaberFinalIK;

using System;
using AvatarScriptPack;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;
using VRIK = BeatSaberFinalIK::RootMotion.FinalIK.VRIK;

namespace CustomAvatar.Avatar
{
	public class SpawnedAvatar : MonoBehaviour
	{
		public LoadedAvatar avatar { get; private set; }
		public AvatarInput input { get; private set; }

        public float verticalPosition
        {
            get => transform.position.y - _initialPosition.y;
            set => transform.position = _initialPosition + value * Vector3.up;
        }

        public float scale
        {
            get => transform.localScale.y / _initialScale.y;
            set
            {
                transform.localScale = _initialScale * value;
                _logger.Info("Avatar resized with scale: " + value);
            }
        }

        public float eyeHeight { get; private set; }
        public float armSpan { get; private set; }
        public bool supportsFingerTracking { get; private set; }
        public bool isIKAvatar { get; private set; }

		public AvatarTracking tracking { get; private set; }
        public AvatarIK ik { get; private set; }
        public AvatarFingerTracking fingerTracking { get; private set; }

        private ILogger _logger;
        private GameScenesManager _gameScenesManager;

        private FirstPersonExclusion[] _firstPersonExclusions;
        private Renderer[] _renderers;
        private EventManager _eventManager;
        private AvatarGameplayEventsPlayer _gameplayEventsPlayer;

        private bool _isCalibrationModeEnabled;

        private Vector3 _initialPosition;
        private Vector3 _initialScale;

        public void EnableCalibrationMode()
        {
            if (_isCalibrationModeEnabled || !ik) return;

            _isCalibrationModeEnabled = true;

            tracking.isCalibrationModeEnabled = true;
            ik.EnableCalibrationMode();
        }

        public void DisableCalibrationMode()
        {
            if (!_isCalibrationModeEnabled || !ik) return;

            tracking.isCalibrationModeEnabled = false;
            ik.DisableCalibrationMode();

            _isCalibrationModeEnabled = false;
        }

        public void UpdateFirstPersonVisibility(FirstPersonVisibility visibility)
        {
            switch (visibility)
            {
                case FirstPersonVisibility.Visible:
                    SetChildrenToLayer(AvatarLayers.kAlwaysVisible);
                    break;

                case FirstPersonVisibility.ApplyFirstPersonExclusions:
                    SetChildrenToLayer(AvatarLayers.kAlwaysVisible);
                    ApplyFirstPersonExclusions();
                    break;

                case FirstPersonVisibility.None:
                    SetChildrenToLayer(AvatarLayers.kOnlyInThirdPerson);
                    break;
            }
        }

        #region Behaviour Lifecycle

        private void Awake()
        {
            _initialPosition = transform.localPosition;
            _initialScale = transform.localScale;
        }
        
        [Inject]
        private void Inject(DiContainer container, ILoggerProvider loggerProvider, LoadedAvatar loadedAvatar, AvatarInput avatarInput, GameScenesManager gameScenesManager)
        {
            avatar = loadedAvatar ?? throw new ArgumentNullException(nameof(loadedAvatar));
            input = avatarInput ?? throw new ArgumentNullException(nameof(avatarInput));

            _logger = loggerProvider.CreateLogger<SpawnedAvatar>(loadedAvatar.descriptor.name);
            _gameScenesManager = gameScenesManager;

            _eventManager = GetComponent<EventManager>();
            _firstPersonExclusions = GetComponentsInChildren<FirstPersonExclusion>();
            _renderers = GetComponentsInChildren<Renderer>();

            supportsFingerTracking = GetComponentInChildren<Animator>() &&
                                     GetComponentInChildren<PoseManager>();

            VRIKManager vrikManager = GetComponentInChildren<VRIKManager>();
            IKManager ikManager = GetComponentInChildren<IKManager>();
            VRIK vrik = GetComponentInChildren<VRIK>();

            isIKAvatar = ikManager || vrikManager;

            if (vrik && !vrik.references.isFilled) vrik.AutoDetectReferences();
            if (vrikManager && !vrikManager.areReferencesFilled) vrikManager.AutoDetectReferences();

            if (isIKAvatar) FixTrackingReferences();

            eyeHeight = GetEyeHeight();
            armSpan = GetArmSpan();

            tracking       = container.InstantiateComponent<AvatarTracking>(gameObject, new object[] { this, avatarInput });

            if (isIKAvatar)
            {
                ik = container.InstantiateComponent<AvatarIK>(gameObject, new object[] { loadedAvatar, avatarInput });
            }

            if (supportsFingerTracking)
            {
                fingerTracking = container.InstantiateComponent<AvatarFingerTracking>(gameObject, new object[] { avatarInput });
            }

            if (_initialPosition.magnitude > 0.0f)
            {
                _logger.Warning("Avatar root position is not at origin; resizing by height and floor adjust may not work properly.");
            }

            DontDestroyOnLoad(this);

            _gameScenesManager.transitionDidFinishEvent += OnTransitionDidFinish;
        }

        private void OnDestroy()
        {
            _gameScenesManager.transitionDidFinishEvent -= OnTransitionDidFinish;

            if (input is IDisposable disposableInput)
            {
                disposableInput.Dispose();
            }

            Destroy(gameObject);
        }

        #endregion

        private void OnTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            if (_gameScenesManager.GetCurrentlyLoadedSceneNames().Contains("GameplayCore"))
            {
                if (_eventManager && !_gameplayEventsPlayer)
                {
                    _logger.Info($"Adding {nameof(AvatarGameplayEventsPlayer)}");
                    _gameplayEventsPlayer = container.InstantiateComponent<AvatarGameplayEventsPlayer>(gameObject, new object[] { avatar });
                }
            }
            else
            {
                if (_gameplayEventsPlayer)
                {
                    _logger.Info($"Removing {nameof(AvatarGameplayEventsPlayer)}");
                    Destroy(_gameplayEventsPlayer);
                }

                if (_eventManager && _gameScenesManager.GetCurrentlyLoadedSceneNames().Contains("MainMenu"))
                {
                    _eventManager.OnMenuEnter?.Invoke();
                }
            }
        }

        private void SetChildrenToLayer(int layer)
        {
	        foreach (Renderer renderer in _renderers)
            {
                renderer.gameObject.layer = layer;
	        }
        }

        private void ApplyFirstPersonExclusions()
        {
            foreach (FirstPersonExclusion firstPersonExclusion in _firstPersonExclusions)
            {
                foreach (GameObject gameObj in firstPersonExclusion.exclude)
                {
                    if (!gameObj) continue;

                    _logger.Trace($"Excluding '{gameObj.name}' from first person view");
                    gameObj.layer = AvatarLayers.kOnlyInThirdPerson;
                }
            }
        }

        private float GetEyeHeight()
        {
            Transform head = transform.Find("Head");

            if (!head)
            {
                _logger.Error("Avatar does not have a head tracking reference");
                Destroy(this);
            }

            // many avatars rely on this being global because their root position isn't at (0, 0, 0)
            return head.position.y;
        }

        private void FixTrackingReferences()
        {
            Vector3 headOffset = GetTargetOffset(nameof(VRIK.References.head), nameof(VRIKManager.solver_spine_headTarget), nameof(IKManager.HeadTarget));
            Vector3 leftHandOffset = GetTargetOffset(nameof(VRIK.References.leftHand), nameof(VRIKManager.solver_leftArm_target), nameof(IKManager.LeftHandTarget));
            Vector3 rightHandOffset = GetTargetOffset(nameof(VRIK.References.rightHand), nameof(VRIKManager.solver_rightArm_target), nameof(IKManager.RightHandTarget));
            
            // only warn if offset is larger than 1 mm
            if (headOffset.magnitude > 0.001f)
            {
                // manually putting each coordinate gives more resolution
                _logger.Warning($"Head bone and target are not at the same position; offset: ({headOffset.x}, {headOffset.y}, {headOffset.z})");
                transform.Find("Head").position -= headOffset;
            }

            if (leftHandOffset.magnitude > 0.001f)
            {
                _logger.Warning($"Left hand bone and target are not at the same position; offset: ({leftHandOffset.x}, {leftHandOffset.y}, {leftHandOffset.z})");
                transform.Find("LeftHand").position -= headOffset;
            }

            if (rightHandOffset.magnitude > 0.001f)
            {
                _logger.Warning($"Right hand bone and target are not at the same position; offset: ({rightHandOffset.x}, {rightHandOffset.y}, {rightHandOffset.z})");
                transform.Find("RightHand").position -= headOffset;
            }
        }

        /// <summary>
        /// Gets the offset between the target and the actual bone. Avoids issues when using just the tracking reference transform for calculations.
        /// </summary>
        private Vector3 GetTargetOffset(string referenceName, string vrikManagerTargetName, string ikManagerTargetName)
        {
            Transform reference = null;
            Transform target = null;
                
            #pragma warning disable 618
            VRIK vrik = GetComponentInChildren<VRIK>();
            IKManager ikManager = GetComponentInChildren<IKManager>();
            #pragma warning restore 618

            VRIKManager vrikManager = GetComponentInChildren<VRIKManager>();
                
            if (vrikManager)
            {
                if (!vrikManager.references_head) vrikManager.AutoDetectReferences();

                reference = vrikManager.GetFieldValue<Transform>("references_" + referenceName);
                target = vrikManager.GetFieldValue<Transform>(vrikManagerTargetName);
            }
            else if (vrik)
            {
                vrik.AutoDetectReferences();
                reference = vrik.references.GetFieldValue<Transform>(referenceName);

                if (ikManager)
                {
                    target = ikManager.GetFieldValue<Transform>(ikManagerTargetName);
                }
            }

            if (!reference)
            {
                _logger.Warning($"Could not find '{referenceName}' reference");
                return Vector3.zero;
            }

            if (!target)
            {
                // target will be added automatically, no need to adjust
                return Vector3.zero;
            }

            return target.position - reference.position;
        }

        /// <summary>
        /// Measure avatar arm span. Since the player's measured arm span is actually from palm to palm
        /// (approximately) due to the way the controllers are held, this isn't "true" arm span.
        /// </summary>
        private float GetArmSpan()
        {
            // TODO using animator here probably isn't a good idea, use VRIKManager references instead
            Animator animator = GetComponentInChildren<Animator>();

            if (!animator) return AvatarTailor.kDefaultPlayerArmSpan;

            Transform leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform leftHand = transform.Find("LeftHand");

            Transform rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform rightHand = transform.Find("RightHand");

            if (!leftShoulder || !leftUpperArm || !leftLowerArm || !rightShoulder || !rightUpperArm || !rightLowerArm)
            {
                _logger.Warning("Could not calculate avatar arm span due to missing bones");
                return AvatarTailor.kDefaultPlayerArmSpan;
            }

            if (!leftHand || !rightHand)
            {
                _logger.Warning("Could not calculate avatar arm span due to missing tracking references");
                return AvatarTailor.kDefaultPlayerArmSpan;
            }

            float leftArmLength = Vector3.Distance(leftShoulder.position, leftUpperArm.position) + Vector3.Distance(leftUpperArm.position, leftLowerArm.position) + Vector3.Distance(leftLowerArm.position, leftHand.position);
            float rightArmLength = Vector3.Distance(rightShoulder.position, rightUpperArm.position) + Vector3.Distance(rightUpperArm.position, rightLowerArm.position) + Vector3.Distance(rightLowerArm.position, rightHand.position);
            float shoulderToShoulderDistance = Vector3.Distance(leftShoulder.position, rightShoulder.position);

            float totalLength = leftArmLength + shoulderToShoulderDistance + rightArmLength;

            return totalLength;
        }
    }
}
