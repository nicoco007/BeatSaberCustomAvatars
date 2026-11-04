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
    internal class GenericNode : MonoBehaviour, ITrackedNode
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Transform _grabSphereTransform;
        [SerializeField] private Material _defaultMaterial;
        [SerializeField] private Material _activeMaterial;

        [field: SerializeField]
        public Transform offset { get; private set; }

        [field: SerializeField]
        public Transform manualOffset { get; private set; }

        [field: SerializeField]
        public Transform calibration { get; private set; }

        [field: SerializeField]
        internal GameObject grabSphere { get; private set; }

        public bool isTracking { get; set; }

        public bool isCalibrated { get; set; }

        public bool isGrabbable => isTracking && isCalibrated && !isGrabbed;

        public bool isGrabbed { get; private set; }

        internal void Grab()
        {
            isGrabbed = true;
            _renderer.sharedMaterial = _activeMaterial;
            _grabSphereTransform.localScale = Vector3.one * 0.03f;
        }

        internal void Release()
        {
            isGrabbed = false;
            _renderer.sharedMaterial = _defaultMaterial;
            _grabSphereTransform.localScale = Vector3.one * 0.05f;
        }
    }
}
