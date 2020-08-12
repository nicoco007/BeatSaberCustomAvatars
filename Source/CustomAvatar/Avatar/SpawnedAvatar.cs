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
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
	public class SpawnedAvatar : MonoBehaviour
	{
		public LoadedAvatar avatar { get; private set; }
		public IAvatarInput input { get; private set; }

        public float verticalPosition
        {
            get => transform.localPosition.y - _initialLocalPosition.y;
            set => transform.localPosition = new Vector3(transform.localPosition.x, _initialLocalPosition.y + value, transform.localPosition.z);
        }

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

        public Transform head { get; private set; }
        public Transform body { get; private set; }
        public Transform leftHand { get; private set; }
        public Transform rightHand { get; private set; }
        public Transform leftLeg { get; private set; }
        public Transform rightLeg { get; private set; }
        public Transform pelvis { get; private set; }

		public AvatarTracking tracking { get; private set; }
        public AvatarIK ik { get; private set; }
        public AvatarFingerTracking fingerTracking { get; private set; }

        private ILogger<SpawnedAvatar> _logger;
        private DiContainer _container;
        private GameScenesHelper _gameScenesHelper;

        private FirstPersonExclusion[] _firstPersonExclusions;
        private Renderer[] _renderers;
        private EventManager _eventManager;
        private AvatarGameplayEventsPlayer _gameplayEventsPlayer;

        private bool _isCalibrationModeEnabled;

        private Vector3 _initialLocalPosition;
        private Vector3 _initialLocalScale;

        public void EnableCalibrationMode()
        {
            if (_isCalibrationModeEnabled || !ik) return;

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

                case FirstPersonVisibility.VisibleWithExclusionsApplied:
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
        private void Inject(DiContainer container, ILoggerProvider loggerProvider, LoadedAvatar loadedAvatar, IAvatarInput avatarInput, GameScenesHelper gameScenesHelper)
        {
            avatar = loadedAvatar ?? throw new ArgumentNullException(nameof(loadedAvatar));
            input = avatarInput ?? throw new ArgumentNullException(nameof(avatarInput));

            _logger = loggerProvider.CreateLogger<SpawnedAvatar>(loadedAvatar.descriptor.name);
            _container = new DiContainer(container);
            _gameScenesHelper = gameScenesHelper;

            _container.Bind<SpawnedAvatar>().FromInstance(this);
        }

        private void Start()
        {
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

            DontDestroyOnLoad(this);

            _gameScenesHelper.transitionDidFinish += OnTransitionDidFinish;
        }

        private void OnDestroy()
        {
            _gameScenesHelper.transitionDidFinish -= OnTransitionDidFinish;

            input.Dispose();

            Destroy(gameObject);
        }

        #endregion

        private void OnTransitionDidFinish(BeatSaberScene scene, DiContainer container)
        {
            if (scene == BeatSaberScene.Game)
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

                if (_eventManager && scene == BeatSaberScene.MainMenu)
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
    }
}
