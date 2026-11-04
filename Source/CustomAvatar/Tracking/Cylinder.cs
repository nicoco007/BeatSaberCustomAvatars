//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System;
using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class Cylinder : MonoBehaviour
    {
        [SerializeField]
        private Renderer[] _renderers;

        [SerializeField]
        private Transform _cylinder;

        public Transform target { get; set; }

        internal Material sharedMaterial
        {
            private get;
            set
            {
                field = value;

                foreach (Renderer renderer in _renderers)
                {
                    renderer.sharedMaterial = value;
                }

                _sharedMaterialChanged?.Invoke(value);
            }
        }

        private event Action<Material> _sharedMaterialChanged;

        protected void LateUpdate()
        {
            float distance = transform.InverseTransformPoint(target.position).magnitude;
            _cylinder.localPosition = new Vector3(0, 0, distance / 2);
            _cylinder.localScale = new Vector3(1, 1, distance);
            transform.LookAt(target);
        }

        internal class Mirrored : MonoBehaviour
        {
            [SerializeField]
            private Renderer[] _renderers;

            [SerializeField]
            private Transform _cylinderTransform;

            private Cylinder _cylinder;

            internal void Init(Cylinder cylinder)
            {
                _cylinder = cylinder;
            }

            internal void OnBeforeRender()
            {
                transform.localRotation = _cylinder.transform.localRotation;

                Transform cylinderTransform = _cylinder._cylinder;
                _cylinderTransform.localPosition = cylinderTransform.localPosition;
                _cylinderTransform.localScale = cylinderTransform.localScale;
            }

            protected void OnEnable()
            {
                _cylinder._sharedMaterialChanged += OnSharedMaterialChanged;

                OnSharedMaterialChanged(_cylinder.sharedMaterial);
            }

            protected void OnDisable()
            {
                _cylinder._sharedMaterialChanged -= OnSharedMaterialChanged;
            }

            private void OnSharedMaterialChanged(Material material)
            {
                foreach (Renderer renderer in _renderers)
                {
                    renderer.sharedMaterial = _cylinder.sharedMaterial;
                }
            }
        }
    }
}
