//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

        protected void Awake()
        {
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
            name = $"SpawnedAvatar({prefab.descriptor.name})";

            _logger = loggerFactory.CreateLogger<SpawnedAvatar>(prefab.descriptor.name);
        }

        protected void OnDestroy()
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
