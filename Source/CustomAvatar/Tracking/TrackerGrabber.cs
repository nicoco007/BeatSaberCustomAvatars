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
using CustomAvatar.Rendering;
using UnityEngine;
using UnityEngine.XR;
using VRUIControls;
using Zenject;

namespace CustomAvatar.Tracking
{
    internal class TrackerGrabber : MonoBehaviour
    {
        [SerializeField] private Cylinder _cylinder;
        [SerializeField] private Transform _cylinderTransform;
        [SerializeField] private GameObject _cylinderObject;
        [SerializeField] private XRNode _node;
        [SerializeField] private Material _defaultMaterial;
        [SerializeField] private Material _activeMaterial;

        private TrackingRig _trackingRig;

        private CalibratableNode _grabbed;
        private Pose _offset;

        private bool _isActive
        {
            get;
            set
            {
                if (field != value)
                {
                    _cylinderObject.SetActive(value);
                    field = value;
                    _isActiveChanged?.Invoke(value);
                }
            }
        }

        private bool _isGrabbed
        {
            get;
            set
            {
                field = value;
                _isGrabbedChanged?.Invoke(value);
            }
        }

        private event Action<bool> _isActiveChanged;

        private event Action<bool> _isGrabbedChanged;

        [Inject]
        protected void Construct(TrackingRig trackingRig)
        {
            _trackingRig = trackingRig;
        }

        protected void Awake()
        {
            _cylinder.sharedMaterial = _defaultMaterial;
        }

        protected void Update()
        {
            CalibratableNode closest = null;

            if (_grabbed != null)
            {
                closest = _grabbed;

                AdjustMode adjustMode = _trackingRig.adjustMode;

                switch (adjustMode)
                {
                    case AdjustMode.Position:
                        _grabbed.manualOffset.position = transform.TransformPoint(_offset.position);
                        break;

                    case AdjustMode.Rotation:
                        _grabbed.manualOffset.rotation = transform.rotation * _offset.rotation;
                        break;

                    case AdjustMode.PositionAndRotation:
                        _grabbed.manualOffset.SetPositionAndRotation(transform.TransformPoint(_offset.position), transform.rotation * _offset.rotation);
                        break;
                }
            }
            else
            {
                Vector3 self = transform.position;
                float distance = float.PositiveInfinity;

                SetIfCloser(_trackingRig.pelvis, self, ref distance, ref closest);
                SetIfCloser(_trackingRig.leftFoot, self, ref distance, ref closest);
                SetIfCloser(_trackingRig.rightFoot, self, ref distance, ref closest);

                if (closest == null)
                {
                    _isActive = false;
                    return;
                }
            }

            _isActive = true;

            Transform target = closest.manualOffset;
            _cylinder.target = target;

            InputDevice device = InputDevices.GetDeviceAtXRNode(_node);

            if (device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue) && triggerValue >= VRInputModule.kMinPressValue)
            {
                if (!_isGrabbed)
                {
                    _grabbed = closest;
                    _grabbed.Grab();
                    _offset = new Pose(transform.InverseTransformPoint(target.position), Quaternion.Inverse(transform.rotation) * target.rotation);
                    _cylinder.sharedMaterial = _activeMaterial;
                    _cylinderTransform.localScale = Vector3.one * 0.015f;
                    _isGrabbed = true;
                }
            }
            else if (_isGrabbed)
            {
                Release();
            }
        }

        protected void OnDisable()
        {
            _isActive = false;

            if (_isGrabbed)
            {
                Release();
            }
        }

        private void SetIfCloser(CalibratableNode node, Vector3 self, ref float distance, ref CalibratableNode closest)
        {
            if (!node.isGrabbable)
            {
                return;
            }

            float newDistance = Vector3.Distance(self, node.manualOffset.position);

            if (newDistance < distance)
            {
                distance = newDistance;
                closest = node;
            }
        }

        private void Release()
        {
            _grabbed.Release();
            _grabbed = null;
            _cylinder.sharedMaterial = _defaultMaterial;
            _cylinderTransform.localScale = Vector3.one * 0.01f;
            _isGrabbed = false;
        }

        internal class Mirrored : MonoBehaviour
        {
            [SerializeField]
            private GameObject _cylinderObject;

            [SerializeField]
            private Transform _cylinderTransform;

            [SerializeField]
            private MirroredCylinder _cylinder;

            private TrackerGrabber _trackerGrabber;

            internal void Init(TrackerGrabber trackerGrabber)
            {
                _trackerGrabber = trackerGrabber;
                _cylinder.Init(trackerGrabber._cylinder);
            }

            internal void OnBeforeRender()
            {
                _cylinder.OnBeforeRender();
            }

            protected void OnEnable()
            {
                _trackerGrabber._isActiveChanged += OnTrackerGrabberIsActiveChanged;
                _trackerGrabber._isGrabbedChanged += OnTrackerGrabberIsGrabbedChanged;

                OnTrackerGrabberIsActiveChanged(_trackerGrabber._isActive);
            }

            protected void OnDisable()
            {
                _trackerGrabber._isActiveChanged -= OnTrackerGrabberIsActiveChanged;
                _trackerGrabber._isGrabbedChanged -= OnTrackerGrabberIsGrabbedChanged;
            }

            private void OnTrackerGrabberIsActiveChanged(bool isActive)
            {
                _cylinderObject.SetActive(isActive);
            }

            private void OnTrackerGrabberIsGrabbedChanged(bool isGrabbed)
            {
                _cylinderTransform.localScale = _trackerGrabber._cylinderTransform.localScale;
            }
        }
    }
}
