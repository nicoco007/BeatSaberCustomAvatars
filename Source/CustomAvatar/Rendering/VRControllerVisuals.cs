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

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    [DisallowMultipleComponent]
    internal class VRControllerVisuals : MonoBehaviour
    {
        private static readonly string[] kHandleTransforms = { "Glowing", "Normal", "FakeGlow0", "FakeGlow1" };

        private VRControllerVisualsManager _vrControllerVisualsManager;

        private GameObject[] _gameObjects;
#pragma warning disable CS0612
        private ConditionalActivation[] _conditionalActivations;
#pragma warning restore CS0612

        internal void SetHandleActive(bool active)
        {
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.SetActive(active);
            }

            if (active)
            {
#pragma warning disable CS0612
                foreach (ConditionalActivation conditionalActivation in _conditionalActivations)
                {
                    conditionalActivation.Awake();
                }
#pragma warning restore CS0612
            }
        }

        [Inject]
        [UsedImplicitly]
        private void Construct(VRControllerVisualsManager vrControllerVisualsManager)
        {
            _vrControllerVisualsManager = vrControllerVisualsManager;
        }

        protected void Awake()
        {
            VRController vrController = GetComponent<VRController>();
            List<GameObject> gameObjects = new(kHandleTransforms.Length);
#pragma warning disable CS0612
            List<ConditionalActivation> conditionalActivations = new(kHandleTransforms.Length);
#pragma warning restore CS0612

            foreach (string name in kHandleTransforms)
            {
                Transform transform = vrController.viewAnchorTransform.Find(name);

                if (transform == null)
                {
                    continue;
                }

                GameObject gameObject = transform.gameObject;
                gameObjects.Add(gameObject);

#pragma warning disable CS0612
                if (gameObject.TryGetComponent(out ConditionalActivation conditionalActivation))
                {
                    conditionalActivations.Add(conditionalActivation);
                }
#pragma warning restore CS0612
            }

            _gameObjects = gameObjects.ToArray();
            _conditionalActivations = conditionalActivations.ToArray();
        }

        protected void Start()
        {
            _vrControllerVisualsManager?.Add(this);
        }

        protected void OnDestroy()
        {
            _vrControllerVisualsManager?.Remove(this);
        }
    }
}
