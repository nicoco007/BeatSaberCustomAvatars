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
            set
            {
                foreach (Renderer renderer in _renderers)
                {
                    renderer.sharedMaterial = value;
                }
            }
        }

        protected void LateUpdate()
        {
            float distance = transform.InverseTransformPoint(target.position).magnitude;
            _cylinder.localPosition = new Vector3(0, 0, distance / 2);
            _cylinder.localScale = new Vector3(1, 1, distance);
            transform.LookAt(target);
        }
    }
}
