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

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace CustomAvatar.Tracking.UnityXR
{
    internal class UnityXRDeviceProvider : IInitializable, IDeviceProvider, IDisposable
    {
        private const string kTrackerDeviceTypeName = "XRTracker";
        private const string kWaistUsage = "Waist";
        private const string kLeftFootUsage = "LeftFoot";
        private const string kRightFootUsage = "RightFoot";

        private readonly InputActionMap _inputActions = new("Custom Avatars Additional Tracking");

        private readonly UnityXRHelper _unityXRHelper;

        private XRDevice _head;
        private XRDevice _leftHand;
        private XRDevice _rightHand;
        private XRDevice _waist;
        private XRDevice _leftFoot;
        private XRDevice _rightFoot;

        internal UnityXRDeviceProvider(IVRPlatformHelper vrPlatformHelper)
        {
            _unityXRHelper = (UnityXRHelper)vrPlatformHelper;
        }

        public event Action devicesChanged;

        public void Initialize()
        {
            _head = CreateDevice("Head", _unityXRHelper._headPositionActionReference, _unityXRHelper._headOrientationActionReference);
            _leftHand = CreateDevice("LeftHand", _unityXRHelper._leftControllerConfiguration.positionActionReference, _unityXRHelper._leftControllerConfiguration.orientationActionReference);
            _rightHand = CreateDevice("RightHand", _unityXRHelper._rightControllerConfiguration.positionActionReference, _unityXRHelper._rightControllerConfiguration.orientationActionReference);
            _waist = CreateDevice("Waist", kTrackerDeviceTypeName, kWaistUsage);
            _leftFoot = CreateDevice("LeftFoot", kTrackerDeviceTypeName, kLeftFootUsage);
            _rightFoot = CreateDevice("RightFoot", kTrackerDeviceTypeName, kRightFootUsage);

            _inputActions.Enable();
        }

        public bool TryGetDevice(DeviceUse deviceUse, out TrackedDevice trackedDevice)
        {
            switch (deviceUse)
            {
                case DeviceUse.Head:
                    trackedDevice = _head.GetDevice();
                    break;

                case DeviceUse.LeftHand:
                    trackedDevice = _leftHand.GetDevice();
                    break;

                case DeviceUse.RightHand:
                    trackedDevice = _rightHand.GetDevice();
                    break;

                case DeviceUse.Waist:
                    trackedDevice = _waist.GetDevice();
                    break;

                case DeviceUse.LeftFoot:
                    trackedDevice = _leftFoot.GetDevice();
                    break;

                case DeviceUse.RightFoot:
                    trackedDevice = _rightFoot.GetDevice();
                    break;

                default:
                    trackedDevice = default;
                    return false;
            }

            return true;
        }

        public void Dispose()
        {
            DeregisterCallbacks(_head);
            DeregisterCallbacks(_leftHand);
            DeregisterCallbacks(_rightHand);
            DeregisterCallbacks(_waist);
            DeregisterCallbacks(_leftFoot);
            DeregisterCallbacks(_rightFoot);

            _inputActions.Disable();
            _inputActions.Dispose();
        }

        private XRDevice CreateDevice(string name, InputAction positionAction, InputAction orientationAction)
        {
            InputAction isTrackedAction = _inputActions.AddAction($"{name}IsTracked");

            foreach (InputBinding binding in positionAction.bindings)
            {
                isTrackedAction.AddBinding($"{binding.path.Substring(0, binding.path.IndexOf('/'))}/isTracked", groups: binding.groups);
            }

            return CreateDevice(isTrackedAction, positionAction, orientationAction);
        }

        private XRDevice CreateDevice(string name, string deviceTypeName, string deviceUsage)
        {
            InputAction positionAction = CreateAction($"{name}Position", $"<{deviceTypeName}>{{{deviceUsage}}}/devicePosition");
            InputAction orientationAction = CreateAction($"{name}Orientation", $"<{deviceTypeName}>{{{deviceUsage}}}/deviceRotation");
            InputAction isTrackedAction = CreateAction($"{name}IsTracked", $"<{deviceTypeName}>{{{deviceUsage}}}/isTracked");

            return CreateDevice(isTrackedAction, positionAction, orientationAction);
        }

        private XRDevice CreateDevice(InputAction isTrackedAction, InputAction positionAction, InputAction orientationAction)
        {
            // isTracked is a ButtonControl so "started" is triggered when tracking starts and "canceled" when tracking stops
            isTrackedAction.started += OnInputActionChanged;
            isTrackedAction.canceled += OnInputActionChanged;

            return new XRDevice(isTrackedAction, positionAction, orientationAction);
        }

        private InputAction CreateAction(string name, string bindingPath)
        {
            InputAction inputAction = _inputActions.AddAction(name);
            inputAction.AddBinding(bindingPath, groups: "XR");
            return inputAction;
        }

        private void DeregisterCallbacks(XRDevice device)
        {
            device.isTrackedAction.started -= OnInputActionChanged;
            device.isTrackedAction.canceled -= OnInputActionChanged;
        }

        private void OnInputActionChanged(InputAction.CallbackContext context)
        {
            devicesChanged?.Invoke();
        }

        private class XRDevice
        {
            internal XRDevice(InputAction isTrackedAction, InputAction positionAction, InputAction orientationAction)
            {
                this.isTrackedAction = isTrackedAction;
                this.positionAction = positionAction;
                this.orientationAction = orientationAction;
            }

            internal InputAction isTrackedAction { get; }

            internal InputAction positionAction { get; }

            internal InputAction orientationAction { get; }

            public override string ToString()
            {
                return $"{nameof(XRDevice)}@{GetHashCode()}[" +
                    $"{nameof(isTrackedAction)}={isTrackedAction.ReadValue<float>()}, " +
                    $"{nameof(positionAction)}={positionAction.ReadValue<Vector3>()}, " +
                    $"{nameof(orientationAction)}={orientationAction.ReadValue<Quaternion>()}]";
            }

            internal TrackedDevice GetDevice()
            {
                bool isTracked = isTrackedAction.ReadValue<float>() > 0.5f;

                // If we don't check isTracked here, positionAction.ReadValue below throws an InvalidOperationException when the OpenXR loader is disabled.
                if (!isTracked)
                {
                    return default;
                }

                return new TrackedDevice(isTracked, positionAction.ReadValue<Vector3>(), orientationAction.ReadValue<Quaternion>());
            }
        }
    }
}
