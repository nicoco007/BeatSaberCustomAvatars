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
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class CalibratableNode : MonoBehaviour, ITrackedNode
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

        public bool isGrabbed
        {
            get;
            set
            {
                field = value;
                _isGrabbedChanged?.Invoke(value);
            }
        }

        private event Action<bool> _isGrabbedChanged;

        internal void Grab()
        {
            _renderer.sharedMaterial = _activeMaterial;
            _grabSphereTransform.localScale = Vector3.one * 0.03f;
            isGrabbed = true;
        }

        internal void Release()
        {
            _renderer.sharedMaterial = _defaultMaterial;
            _grabSphereTransform.localScale = Vector3.one * 0.05f;
            isGrabbed = false;
        }

        internal class Mirrored : MonoBehaviour
        {
            [SerializeField] private Transform _calibration;

            [SerializeField] private Transform _manualOffset;

            [SerializeField] private Transform _sphereTransform;

            [SerializeField] private Renderer _sphereRenderer;

            [field: SerializeField] internal GameObject grabSphere { get; private set; }

            private CalibratableNode _calibratableNode;

            internal void Init(CalibratableNode calibratableNode)
            {
                _calibratableNode = calibratableNode;
            }

            internal void OnBeforeRender()
            {
                transform.CopyLocalPositionAndRotationFrom(_calibratableNode.transform);
                _calibration.CopyLocalPositionAndRotationFrom(_calibratableNode.calibration);
                _manualOffset.CopyLocalPositionAndRotationFrom(_calibratableNode.manualOffset);
            }

            protected void OnEnable()
            {
                _calibratableNode._isGrabbedChanged += OnIsGrabbedChanged;

                OnIsGrabbedChanged(_calibratableNode.isGrabbed);
            }

            protected void OnDisable()
            {
                _calibratableNode._isGrabbedChanged -= OnIsGrabbedChanged;
            }

            private void OnIsGrabbedChanged(bool grabbed)
            {
                _sphereTransform.localScale = _calibratableNode._grabSphereTransform.localScale;
                _sphereRenderer.sharedMaterial = _calibratableNode._renderer.sharedMaterial;
            }
        }
    }
}
