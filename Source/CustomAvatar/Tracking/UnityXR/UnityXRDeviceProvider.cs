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

using CustomAvatar.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Tracking.UnityXR
{
    internal class UnityXRDeviceProvider : IInitializable, IDeviceProvider, IDisposable
    {
        public event Action devicesChanged;

        private readonly ILogger<UnityXRDeviceProvider> _logger;

        private readonly Dictionary<string, InputDevice> _inputDevices = new Dictionary<string, InputDevice>();

        private UnityXRDeviceProvider(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger<UnityXRDeviceProvider>();
        }

        public void Initialize()
        {
            InputDevices.deviceDisconnected  += OnDeviceDisconnected;
        }

        public void GetDevices(Dictionary<string, TrackedDevice> devices)
        {
            devices.Clear();

            var inputDevices = new List<InputDevice>();
            bool changeDetected = false;

            InputDevices.GetDevices(inputDevices);

            foreach (InputDevice inputDevice in inputDevices)
            {
                if (!inputDevice.isValid) return;

                DeviceUse use = DeviceUse.Unknown;

                if (inputDevice.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
                {
                    use = DeviceUse.Head;
                }
                else if (inputDevice.characteristics.HasFlag(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left))
                {
                    use = DeviceUse.LeftHand;
                }
                else if (inputDevice.characteristics.HasFlag(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right))
                {
                    use = DeviceUse.RightHand;
                }

                inputDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked);
                inputDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
                inputDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);

                if (_inputDevices.ContainsKey(inputDevice.name))
                {
                    if (inputDevice.characteristics != _inputDevices[inputDevice.name].characteristics)
                    {
                        _logger.Info($"Characteristics of device '{inputDevice.name}' changed from {_inputDevices[inputDevice.name].characteristics} to {inputDevice.characteristics}");
                        changeDetected = true;
                    }

                    if (isTracked && (!_inputDevices[inputDevice.name].TryGetFeatureValue(CommonUsages.isTracked, out bool previouslyTracked) || isTracked != previouslyTracked))
                    {
                        if (isTracked)
                        {
                            _logger.Info($"Acquired tracking of device '{inputDevice.name}'");
                        }
                        else
                        {
                            _logger.Info($"Lost tracking of device '{inputDevice.name}'");
                        }

                        changeDetected = true;
                    }

                    _inputDevices[inputDevice.name] = inputDevice;
                }
                else
                {
                    _logger.Info($"Device '{inputDevice.name}' connected with characteristics {inputDevice.characteristics}");

                    _inputDevices.Add(inputDevice.name, inputDevice);

                    changeDetected = true;
                }

                devices.Add(inputDevice.name, new TrackedDevice(inputDevice.name, use, isTracked, position, rotation));
            }

            if (changeDetected) devicesChanged?.Invoke();
        }

        public void Dispose()
        {
            InputDevices.deviceDisconnected  -= OnDeviceDisconnected;
        }

        private void OnDeviceDisconnected(InputDevice device)
        {
            if (_inputDevices.ContainsKey(device.name))
            {
                _logger.Info($"Device '{device.name}' disconnected");
                _inputDevices.Remove(device.name);
            }

            devicesChanged?.Invoke();
        }
    }
}
