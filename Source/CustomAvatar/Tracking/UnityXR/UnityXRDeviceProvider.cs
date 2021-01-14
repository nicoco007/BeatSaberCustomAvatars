//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
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
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Tracking.UnityXR
{
    internal class UnityXRDeviceProvider : IInitializable, IDeviceProvider, IDisposable
    {
        private static readonly Regex kSerialNumberRegex = new Regex(@"(.*)S/N ([^ ]+)(.*)");

        private readonly ILogger<UnityXRDeviceProvider> _logger;

        private readonly Dictionary<string, UnityXRDevice> _devices = new Dictionary<string, UnityXRDevice>();

        private bool deviceRemovedSinceLastCall;

        private UnityXRDeviceProvider(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger<UnityXRDeviceProvider>();
        }

        public void Initialize()
        {
            InputDevices.deviceDisconnected  += OnDeviceDisconnected;
        }

        public bool GetDevices(Dictionary<string, TrackedDevice> devices)
        {
            devices.Clear();

            var inputDevices = new List<InputDevice>();
            bool changeDetected = deviceRemovedSinceLastCall;

            InputDevices.GetDevices(inputDevices);

            foreach (InputDevice inputDevice in inputDevices)
            {
                if (!inputDevice.isValid) continue;

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

                string id;

                if (_devices.TryGetValue(inputDevice.name, out UnityXRDevice existingDevice))
                {
                    id = existingDevice.id;

                    if (inputDevice.characteristics != existingDevice.characteristics)
                    {
                        _logger.Info($"Characteristics of device '{existingDevice.id}' changed from {existingDevice.characteristics} to {inputDevice.characteristics}");
                        changeDetected = true;
                    }

                    if (existingDevice.isTracked != isTracked)
                    {
                        if (isTracked)
                        {
                            _logger.Info($"Acquired tracking of device '{existingDevice.id}'");
                        }
                        else
                        {
                            _logger.Info($"Lost tracking of device '{existingDevice.id}'");
                        }

                        changeDetected = true;
                    }

                    _devices[inputDevice.name] = new UnityXRDevice(existingDevice.id, true, isTracked, inputDevice.characteristics);
                }
                else
                {
                    Match match = kSerialNumberRegex.Match(inputDevice.name);

                    if (match.Success)
                    {
                        id = match.Groups[1].Value + (uint)match.Groups[2].Value.GetHashCode() + match.Groups[3].Value;
                    }
                    else
                    {
                        id = inputDevice.name;
                    }

                    _logger.Info($"Device '{id}' connected with characteristics {inputDevice.characteristics}");

                    _devices.Add(inputDevice.name, new UnityXRDevice(id, true, isTracked, inputDevice.characteristics));

                    changeDetected = true;
                }

                devices.Add(inputDevice.name, new TrackedDevice(id, use, isTracked, position, rotation));
            }

            deviceRemovedSinceLastCall = false;

            return changeDetected;
        }

        public void Dispose()
        {
            InputDevices.deviceDisconnected  -= OnDeviceDisconnected;
        }

        private void OnDeviceDisconnected(InputDevice device)
        {
            if (_devices.TryGetValue(device.name, out UnityXRDevice existingDevice))
            {
                _logger.Info($"Device '{existingDevice.id}' disconnected");
                _devices.Remove(device.name);

                deviceRemovedSinceLastCall = true;
            }
        }

        private readonly struct UnityXRDevice
        {
            public readonly string id;
            public readonly bool isConnected;
            public readonly bool isTracked;
            public readonly InputDeviceCharacteristics characteristics;

            public UnityXRDevice(string id, bool isConnected, bool isTracked, InputDeviceCharacteristics characteristics)
            {
                this.id = id;
                this.isConnected = isConnected;
                this.isTracked = isTracked;
                this.characteristics = characteristics;
            }
        }
    }
}
