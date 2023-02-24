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

extern alias BeatSaberFinalIK;

using System;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    /// <summary>
    /// Represents a <see cref="AvatarPrefab"/> that has been spawned into the game.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpawnedAvatar : MonoBehaviour
    {
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
            }
        }

        public float absoluteScale => transform.localScale.y;

        public float scaledEyeHeight
        {
            get => prefab.eyeHeight * scale;
            set
            {
                scale = value / prefab.eyeHeight;
            }
        }

        public Transform head { get; private set; }
        public Transform body { get; private set; }
        public Transform leftHand { get; private set; }
        public Transform rightHand { get; private set; }
        public Transform leftLeg { get; private set; }
        public Transform rightLeg { get; private set; }
        public Transform pelvis { get; private set; }

        internal AvatarTransformTracking transformTracking { get; private set; }
        internal AvatarIK ik { get; private set; }
        internal AvatarFingerTracking fingerTracking { get; private set; }
        internal EventManager eventManager { get; private set; }

        private ILogger<SpawnedAvatar> _logger;

        private FirstPersonExclusion[] _firstPersonExclusions;
        private Renderer[] _renderers;

        private Vector3 _initialLocalPosition;
        private Vector3 _initialLocalScale;

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

            _firstPersonExclusions = GetComponentsInChildren<FirstPersonExclusion>();
            _renderers = GetComponentsInChildren<Renderer>();

            eventManager = GetComponent<EventManager>();

            head = transform.Find("Head");
            body = transform.Find("Body");
            leftHand = transform.Find("LeftHand");
            rightHand = transform.Find("RightHand");
            pelvis = transform.Find("Pelvis");
            leftLeg = transform.Find("LeftLeg");
            rightLeg = transform.Find("RightLeg");

            transformTracking = GetComponent<AvatarTransformTracking>();
            ik = GetComponent<AvatarIK>();
            fingerTracking = GetComponent<AvatarFingerTracking>();
        }

        [Inject]
        private void Construct(ILoggerFactory loggerFactory, AvatarPrefab avatarPrefab, IAvatarInput avatarInput)
        {
            prefab = avatarPrefab;
            input = avatarInput;

            _logger = loggerFactory.CreateLogger<SpawnedAvatar>(prefab.descriptor.name);
        }

        private void Start()
        {
            name = $"SpawnedAvatar({prefab.descriptor.name})";

            if (_initialLocalPosition.sqrMagnitude > 0)
            {
                _logger.LogWarning("Avatar root position is not at origin; resizing by height and floor adjust may not work properly.");
            }
        }

        private void OnDestroy()
        {
            Destroy(gameObject);
        }

#pragma warning restore IDE0051
        #endregion

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
