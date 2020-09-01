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
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace CustomAvatar.Avatar
{
    /// <summary>
    /// Represents a <see cref="LoadedAvatar"/> that has been spawned into the game.
    /// </summary>
	public class SpawnedAvatar : MonoBehaviour
	{
        /// <summary>
        /// The <see cref="LoadedAvatar"/> used as a reference.
        /// </summary>
		public LoadedAvatar avatar { get; private set; }

        /// <summary>
        /// The <see cref="IAvatarInput"/> used for tracking.
        /// </summary>
		public IAvatarInput input { get; private set; }

        /// <summary>
        /// The avatar's scale as a ratio of it's exported scale (i.e. it is initially 1 even if the avatar was exported with a different scale).
        /// </summary>
        public float scale
        {
            get => transform.localScale.y / _initialLocalScale.y;
            set
            {
                if (value <= 0) throw new InvalidOperationException("Scale must be greater than 0");
                if (float.IsInfinity(value)) throw new InvalidOperationException("Scale cannot be infinity");

                transform.localScale = _initialLocalScale * value;
                _logger.Info("Avatar resized with scale: " + value);
            }
        }

        public float scaledEyeHeight => avatar.eyeHeight * scale;

        public Transform head { get; private set; }
        public Transform body { get; private set; }
        public Transform leftHand { get; private set; }
        public Transform rightHand { get; private set; }
        public Transform leftLeg { get; private set; }
        public Transform rightLeg { get; private set; }
        public Transform pelvis { get; private set; }

        internal AvatarTracking tracking { get; private set; }
        internal AvatarIK ik { get; private set; }
        internal AvatarFingerTracking fingerTracking { get; private set; }

        internal bool isLocomotionEnabled { get; private set; }

        private ILogger<SpawnedAvatar> _logger;
        private DiContainer _container;
        private GameScenesManager _gameScenesManager;

        private FirstPersonExclusion[] _firstPersonExclusions;
        private Renderer[] _renderers;
        private EventManager _eventManager;
        private AvatarGameplayEventsPlayer _gameplayEventsPlayer;

        private bool _isCalibrationModeEnabled;

        private Vector3 _initialLocalPosition;
        private Vector3 _initialLocalScale;

        public void SetLocomotionEnabled(bool enabled)
        {
            isLocomotionEnabled = enabled;

            if (ik)
            {
                ik.SetLocomotionEnabled(enabled);
            }
        }

        public void EnableCalibrationMode()
        {
            if (_isCalibrationModeEnabled || !ik) return;

            _isCalibrationModeEnabled = true;

            tracking.isCalibrationModeEnabled = true;
            ik.SetCalibrationModeEnabled(true);
        }

        public void DisableCalibrationMode()
        {
            if (!_isCalibrationModeEnabled || !ik) return;

            tracking.isCalibrationModeEnabled = false;
            ik.SetCalibrationModeEnabled(false);

            _isCalibrationModeEnabled = false;
        }

        public void SetFirstPersonVisibility(FirstPersonVisibility visibility)
        {
            switch (visibility)
            {
                case FirstPersonVisibility.Visible:
                    SetChildrenToLayer(AvatarLayers.kAlwaysVisible);
                    break;

                case FirstPersonVisibility.VisibleWithExclusionsApplied:
                    SetChildrenToLayer(AvatarLayers.kAlwaysVisible);
                    ApplyFirstPersonExclusions();
                    break;

                case FirstPersonVisibility.Hidden:
                    SetChildrenToLayer(AvatarLayers.kOnlyInThirdPerson);
                    break;
            }
        }

        #region Behaviour Lifecycle

        private void Awake()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalScale = transform.localScale;

            _eventManager = GetComponent<EventManager>();
            _firstPersonExclusions = GetComponentsInChildren<FirstPersonExclusion>();
            _renderers = GetComponentsInChildren<Renderer>();

            head      = transform.Find("Head");
            body      = transform.Find("Body");
            leftHand  = transform.Find("LeftHand");
            rightHand = transform.Find("RightHand");
            pelvis    = transform.Find("Pelvis");
            leftLeg   = transform.Find("LeftLeg");
            rightLeg  = transform.Find("RightLeg");
        }
        
        [Inject]
        private void Inject(DiContainer container, ILoggerProvider loggerProvider, LoadedAvatar loadedAvatar, IAvatarInput avatarInput, GameScenesManager gameScenesManager)
        {
            avatar = loadedAvatar ?? throw new ArgumentNullException(nameof(loadedAvatar));
            input = avatarInput ?? throw new ArgumentNullException(nameof(avatarInput));

            _logger = loggerProvider.CreateLogger<SpawnedAvatar>(loadedAvatar.descriptor.name);
            _container = new DiContainer(container);
            _gameScenesManager = gameScenesManager;

            _container.Bind<SpawnedAvatar>().FromInstance(this);
        }

        private void Start()
        {
            name = $"SpawnedAvatar({avatar.descriptor.name})";

            tracking = _container.InstantiateComponent<AvatarTracking>(gameObject);

            if (avatar.isIKAvatar)
            {
                ik = _container.InstantiateComponent<AvatarIK>(gameObject);
            }

            if (avatar.supportsFingerTracking)
            {
                fingerTracking = _container.InstantiateComponent<AvatarFingerTracking>(gameObject);
            }

            if (_initialLocalPosition.sqrMagnitude > 0)
            {
                _logger.Warning("Avatar root position is not at origin; resizing by height and floor adjust may not work properly.");
            }

            _gameScenesManager.transitionDidFinishEvent += OnTransitionDidFinish;
        }

        private void OnDestroy()
        {
            _gameScenesManager.transitionDidFinishEvent -= OnTransitionDidFinish;

            Destroy(gameObject);
        }

        #endregion

        private void OnTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            if (!_eventManager) return;

            switch (SceneManager.GetActiveScene().name)
            {
                case "GameCore":
                    _logger.Info($"Adding {nameof(AvatarGameplayEventsPlayer)}");
                    _gameplayEventsPlayer = container.InstantiateComponent<AvatarGameplayEventsPlayer>(gameObject, new object[] { avatar });

                    break;

                case "MenuViewControllers":
                    _eventManager.OnMenuEnter?.Invoke();
                    break;
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
    }
}
