//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using CustomAvatar.Logging;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Tracking.UnityXR
{
    internal class UnityXRDeviceManager : ITrackedDeviceManager, IInitializable, ITickable, IDisposable
    {
        // these only trigger for devices that are registered to a specific target, not all found input devices
        public event Action<ITrackedDeviceState> deviceAdded;
        public event Action<ITrackedDeviceState> deviceRemoved;
        public event Action<ITrackedDeviceState> deviceTrackingAcquired;
        public event Action<ITrackedDeviceState> deviceTrackingLost;

        private readonly UnityXRDeviceState _head      = new UnityXRDeviceState(DeviceUse.Head);
        private readonly UnityXRDeviceState _leftHand  = new UnityXRDeviceState(DeviceUse.LeftHand);
        private readonly UnityXRDeviceState _rightHand = new UnityXRDeviceState(DeviceUse.RightHand);

        private readonly MainSettingsModelSO _mainSettingsModel;
        private readonly ILogger<UnityXRDeviceManager> _logger;

        private readonly HashSet<string> _foundDevices = new HashSet<string>();

        public UnityXRDeviceManager(ILoggerProvider loggerProvider, MainSettingsModelSO mainSettingsModel)
        {
            _mainSettingsModel = mainSettingsModel;
            _logger = loggerProvider.CreateLogger<UnityXRDeviceManager>();
        }

        public bool TryGetDeviceState(DeviceUse use, out ITrackedDeviceState deviceState)
        {
            switch (use)
            {
                case DeviceUse.Head:
                    deviceState = _head;
                    return true;

                case DeviceUse.LeftHand:
                    deviceState = _leftHand;
                    return true;

                case DeviceUse.RightHand:
                    deviceState = _rightHand;
                    return true;

                default:
                    deviceState = null;
                    return false;
            }
        }

        public void Initialize()
        {
            _logger.Info($"Initializing {nameof(UnityXRDeviceManager)}");

            InputDevices.deviceConnected += OnInputDevicesUpdated;
            InputDevices.deviceDisconnected += OnInputDevicesUpdated;
            InputDevices.deviceConfigChanged += OnInputDevicesUpdated;

            UpdateInputDevices();
        }

        public void Tick()
        {
            var inputDevices = new List<InputDevice>();

            InputDevices.GetDevices(inputDevices);

            InputDevice? headInputDevice      = null;
            InputDevice? leftHandInputDevice  = null;
            InputDevice? rightHandInputDevice = null;

            foreach (InputDevice inputDevice in inputDevices)
            {
                if (inputDevice.name == _head.name)      headInputDevice      = inputDevice;
                if (inputDevice.name == _leftHand.name)  leftHandInputDevice  = inputDevice;
                if (inputDevice.name == _rightHand.name) rightHandInputDevice = inputDevice;
            }

            UpdateTrackedDevice(_head,      headInputDevice);
            UpdateTrackedDevice(_leftHand,  leftHandInputDevice);
            UpdateTrackedDevice(_rightHand, rightHandInputDevice);
        }

        public void Dispose()
        {
            InputDevices.deviceConnected -= OnInputDevicesUpdated;
            InputDevices.deviceDisconnected -= OnInputDevicesUpdated;
            InputDevices.deviceConfigChanged -= OnInputDevicesUpdated;
        }

        private void OnInputDevicesUpdated(InputDevice device) => UpdateInputDevices();

        private void UpdateInputDevices()
        {
            var inputDevices = new List<InputDevice>();

            InputDevices.GetDevices(inputDevices);

            InputDevice? headInputDevice      = null;
            InputDevice? leftHandInputDevice  = null;
            InputDevice? rightHandInputDevice = null;

            foreach (InputDevice device in inputDevices)
            {
                if (!device.isValid) continue;

                if (!_foundDevices.Contains(device.name))
                {
                    _logger.Info($"Found new input device '{device.name}' with serial number '{device.serialNumber}'");
                    _foundDevices.Add(device.name);
                }

                if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
                {
                    headInputDevice = device;
                }
                else if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left))
                {
                    leftHandInputDevice = device;
                }
                else if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right))
                {
                    rightHandInputDevice = device;
                }
            }

            AssignTrackedDevice(_head,      headInputDevice,      DeviceUse.Head);
            AssignTrackedDevice(_leftHand,  leftHandInputDevice,  DeviceUse.LeftHand);
            AssignTrackedDevice(_rightHand, rightHandInputDevice, DeviceUse.RightHand);

            foreach (string deviceName in _foundDevices.ToList())
            {
                if (!inputDevices.Exists(d => d.name == deviceName))
                {
                    _logger.Info($"Lost device '{deviceName}'");
                    _foundDevices.Remove(deviceName);
                }
            }
        }

        private void AssignTrackedDevice(UnityXRDeviceState deviceState, InputDevice? possibleInputDevice, DeviceUse use)
        {
            if ((!possibleInputDevice.HasValue && deviceState.isConnected) || (possibleInputDevice.HasValue && deviceState.isConnected && possibleInputDevice.Value.name != deviceState.name)) {
                _logger.Info($"Removing device '{deviceState.name}' that was used as {use}");

                deviceState.name = null;
                deviceState.isConnected = false;
                deviceState.isTracking = false;

                deviceRemoved?.Invoke(deviceState);
            }

            if (possibleInputDevice.HasValue && (!deviceState.isConnected || possibleInputDevice.Value.name != deviceState.name))
            {
                InputDevice inputDevice = possibleInputDevice.Value;

                _logger.Info($"Using device '{inputDevice.name}' as {use}");

                deviceState.name = inputDevice.name;
                deviceState.isConnected = true;
                
                deviceAdded?.Invoke(deviceState);
            }
        }

        private void UpdateTrackedDevice(UnityXRDeviceState deviceState, InputDevice? possibleInputDevice)
        {
            if (!possibleInputDevice.HasValue) return;

            var inputDevice = possibleInputDevice.Value;

            if (!inputDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) || !isTracked)
            {
                if (deviceState.isTracking)
                {
                    _logger.Info($"Lost tracking of device '{deviceState.name}' used as '{deviceState.use}'");
                    deviceState.isTracking = false;
                    deviceTrackingLost?.Invoke(deviceState);
                }

                return;
            }

            if (!deviceState.isTracking)
            {
                _logger.Info($"Acquired tracking of device '{deviceState.name}'");
                deviceState.isTracking = true;
                deviceTrackingAcquired?.Invoke(deviceState);
            }
            
            if (inputDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            {
                deviceState.position = position;
            }

            if (inputDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                deviceState.rotation = rotation;
            }
        }
    }
}
