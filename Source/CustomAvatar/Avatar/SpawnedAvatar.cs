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

extern alias BeatSaberFinalIK;

using System;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    /// <summary>
    /// Represents a <see cref="AvatarPrefab"/> that has been spawned into the game.
    /// </summary>
    public class SpawnedAvatar : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="LoadedAvatar"/> used as a reference.
        /// </summary>
        [Obsolete("Use prefab instead")]
        public LoadedAvatar avatar { get; private set; }

        /// <summary>
        /// The <see cref="AvatarPrefab"/> used to spawn this avatar.
        /// </summary>
        public AvatarPrefab prefab { get; private set; }

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
                _logger.LogInformation("Avatar resized with scale: " + value);
            }
        }

        public float scaledEyeHeight => prefab.eyeHeight * scale;

        public Transform head { get; private set; }
        public Transform body { get; private set; }
        public Transform leftHand { get; private set; }
        public Transform rightHand { get; private set; }
        public Transform leftLeg { get; private set; }
        public Transform rightLeg { get; private set; }
        public Transform pelvis { get; private set; }

        [Obsolete("Use GetComponent<AvatarTracking>() instead")] internal AvatarTracking tracking { get; private set; }
        [Obsolete("Use GetComponent<AvatarIK>() instead")] internal AvatarIK ik { get; private set; }
        [Obsolete("Use GetComponent<AvatarFingerTracking>() instead")] internal AvatarFingerTracking fingerTracking { get; private set; }


        [Obsolete("Get isLocomotionEnabled on the AvatarIK component instead")] internal bool isLocomotionEnabled { get; private set; }

        private ILogger<SpawnedAvatar> _logger;
        private GameScenesManager _gameScenesManager;

        private FirstPersonExclusion[] _firstPersonExclusions;
        private Renderer[] _renderers;
        private EventManager _eventManager;

        private Vector3 _initialLocalPosition;
        private Vector3 _initialLocalScale;

        [Obsolete("Get isLocomotionEnabled on the AvatarIK component instead")]
        public void SetLocomotionEnabled(bool enabled)
        {
            if (TryGetComponent(out AvatarIK ik))
            {
                ik.isLocomotionEnabled = enabled;
            }
        }

        [Obsolete]
        public void EnableCalibrationMode()
        {
            if (!ik) return;

            tracking.isCalibrationModeEnabled = true;
            ik.isCalibrationModeEnabled = true;
        }

        [Obsolete]
        public void DisableCalibrationMode()
        {
            if (!ik) return;

            tracking.isCalibrationModeEnabled = false;
            ik.isCalibrationModeEnabled = false;
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
#pragma warning disable IDE0051

        private void Awake()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalScale = transform.localScale;

            _eventManager = GetComponent<EventManager>();
            _firstPersonExclusions = GetComponentsInChildren<FirstPersonExclusion>();
            _renderers = GetComponentsInChildren<Renderer>();

            head = transform.Find("Head");
            body = transform.Find("Body");
            leftHand = transform.Find("LeftHand");
            rightHand = transform.Find("RightHand");
            pelvis = transform.Find("Pelvis");
            leftLeg = transform.Find("LeftLeg");
            rightLeg = transform.Find("RightLeg");
        }

        [Inject]
        private void Construct(ILogger<SpawnedAvatar> logger, AvatarPrefab avatarPrefab, IAvatarInput avatarInput, GameScenesManager gameScenesManager)
        {
            prefab = avatarPrefab;
            input = avatarInput;

#pragma warning disable CS0612, CS0618
            avatar = avatarPrefab.loadedAvatar;
#pragma warning restore CS0612, CS0618

            _logger = logger;
            _gameScenesManager = gameScenesManager;

            _logger.name = prefab.descriptor.name;
        }

        private void Start()
        {
            name = $"SpawnedAvatar({prefab.descriptor.name})";

            if (_initialLocalPosition.sqrMagnitude > 0)
            {
                _logger.LogWarning("Avatar root position is not at origin; resizing by height and floor adjust may not work properly.");
            }

            _gameScenesManager.transitionDidFinishEvent += OnTransitionDidFinish;
        }

        private void OnDestroy()
        {
            _gameScenesManager.transitionDidFinishEvent -= OnTransitionDidFinish;

            Destroy(gameObject);
        }

#pragma warning restore IDE0051
        #endregion

        private void OnTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            if (!_eventManager) return;

            if (_gameScenesManager.IsSceneInStackAndActive("MenuCore"))
            {
                _eventManager.OnMenuEnter?.Invoke();
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

                    _logger.LogTrace($"Excluding '{gameObj.name}' from first person view");
                    gameObj.layer = AvatarLayers.kOnlyInThirdPerson;
                }
            }
        }
    }
}
