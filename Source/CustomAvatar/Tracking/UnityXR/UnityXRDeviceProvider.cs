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

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Zenject;

namespace CustomAvatar.Tracking.UnityXR
{
    internal class UnityXRDeviceProvider : IInitializable, IDeviceProvider, IDisposable
    {
        private const string kTrackerDeviceTypeName = "XRTracker";
        private const string kWaistUsage = "Waist";
        private const string kLeftFootUsage = "LeftFoot";
        private const string kRightFootUsage = "RightFoot";

        private readonly InputActionMap _inputActions = new("Custom Avatars");

        private readonly UnityXRDevice _head;
        private readonly UnityXRDevice _leftHand;
        private readonly UnityXRDevice _rightHand;
        private readonly UnityXRDevice _waist;
        private readonly UnityXRDevice _leftFoot;
        private readonly UnityXRDevice _rightFoot;

        internal UnityXRDeviceProvider()
        {
            _head = CreateDevice("Head", nameof(XRHMD));
            _leftHand = CreateDevice("LeftHand", nameof(XRController), CommonUsages.LeftHand);
            _rightHand = CreateDevice("RightHand", nameof(XRController), CommonUsages.RightHand);
            _waist = CreateDevice("Waist", kTrackerDeviceTypeName, kWaistUsage);
            _leftFoot = CreateDevice("LeftFoot", kTrackerDeviceTypeName, kLeftFootUsage);
            _rightFoot = CreateDevice("RightFoot", kTrackerDeviceTypeName, kRightFootUsage);
        }

        public event Action devicesChanged;

        public void Initialize()
        {
            RegisterCallbacks(_head);
            RegisterCallbacks(_leftHand);
            RegisterCallbacks(_rightHand);
            RegisterCallbacks(_waist);
            RegisterCallbacks(_leftFoot);
            RegisterCallbacks(_rightFoot);

            _inputActions.Enable();
        }

        public bool TryGetDeviceState(DeviceUse deviceUse, out DeviceState deviceState)
        {
            if (!_inputActions.enabled)
            {
                deviceState = default;
                return false;
            }

            switch (deviceUse)
            {
                case DeviceUse.Head:
                    deviceState = _head.GetDeviceState();
                    break;

                case DeviceUse.LeftHand:
                    deviceState = _leftHand.GetDeviceState();
                    break;

                case DeviceUse.RightHand:
                    deviceState = _rightHand.GetDeviceState();
                    break;

                case DeviceUse.Waist:
                    deviceState = _waist.GetDeviceState();
                    break;

                case DeviceUse.LeftFoot:
                    deviceState = _leftFoot.GetDeviceState();
                    break;

                case DeviceUse.RightFoot:
                    deviceState = _rightFoot.GetDeviceState();
                    break;

                default:
                    deviceState = default;
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

        private UnityXRDevice CreateDevice(string name, string deviceTypeName)
        {
            InputAction positionAction = _inputActions.AddAction($"{name}Position", binding: $"<{deviceTypeName}>/devicePosition", groups: "XR");
            InputAction orientationAction = _inputActions.AddAction($"{name}Orientation", binding: $"<{deviceTypeName}>/deviceRotation", groups: "XR");
            InputAction isTrackedAction = _inputActions.AddAction($"{name}IsTracked", binding: $"<{deviceTypeName}>/isTracked", groups: "XR");

            return new UnityXRDevice(isTrackedAction, positionAction, orientationAction);
        }

        private UnityXRDevice CreateDevice(string name, string deviceTypeName, string deviceUsage)
        {
            InputAction positionAction = _inputActions.AddAction($"{name}Position", binding: $"<{deviceTypeName}>{{{deviceUsage}}}/devicePosition", groups: "XR");
            InputAction orientationAction = _inputActions.AddAction($"{name}Orientation", binding: $"<{deviceTypeName}>{{{deviceUsage}}}/deviceRotation", groups: "XR");
            InputAction isTrackedAction = _inputActions.AddAction($"{name}IsTracked", binding: $"<{deviceTypeName}>{{{deviceUsage}}}/isTracked", groups: "XR");

            return new UnityXRDevice(isTrackedAction, positionAction, orientationAction);
        }

        private void RegisterCallbacks(UnityXRDevice device)
        {
            // isTracked is a ButtonControl so "started" is triggered when tracking starts and "canceled" when tracking stops
            device.isTrackedAction.started += OnInputActionChanged;
            device.isTrackedAction.canceled += OnInputActionChanged;

            // positionAction is a Vector3Control that is "started" when the device is turned on and "canceled" when it is turned off
            device.positionAction.started += OnInputActionChanged;
            device.positionAction.canceled += OnInputActionChanged;
        }

        private void DeregisterCallbacks(UnityXRDevice device)
        {
            device.isTrackedAction.started -= OnInputActionChanged;
            device.isTrackedAction.canceled -= OnInputActionChanged;

            device.positionAction.started -= OnInputActionChanged;
            device.positionAction.canceled -= OnInputActionChanged;
        }

        private void OnInputActionChanged(InputAction.CallbackContext context)
        {
            devicesChanged?.Invoke();
        }

        private class UnityXRDevice
        {
            internal UnityXRDevice(InputAction isTrackedAction, InputAction positionAction, InputAction orientationAction)
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
                return $"{nameof(UnityXRDevice)}@{GetHashCode()}[" +
                    $"{nameof(isTrackedAction)}={isTrackedAction.ReadValue<float>()}, " +
                    $"{nameof(positionAction)}={positionAction.ReadValue<Vector3>()}, " +
                    $"{nameof(orientationAction)}={orientationAction.ReadValue<Quaternion>()}]";
            }

            internal DeviceState GetDeviceState()
            {
                bool isConnected = positionAction.inProgress;

                if (!isConnected)
                {
                    return default;
                }

                bool isTracked = isTrackedAction.ReadValue<float>() > 0.5f;

                // If we don't check isTracked here, positionAction.ReadValue below throws an InvalidOperationException when the OpenXR loader is disabled.
                if (!isTracked)
                {
                    return new DeviceState(isConnected, isTracked, Vector3.zero, Quaternion.identity);
                }

                return new DeviceState(isConnected, isTracked, positionAction.ReadValue<Vector3>(), orientationAction.ReadValue<Quaternion>());
            }
        }
    }
}
