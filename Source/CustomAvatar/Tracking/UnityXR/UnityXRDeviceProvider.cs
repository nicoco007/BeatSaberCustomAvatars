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
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Tracking.UnityXR
{
    internal class UnityXRDeviceProvider : IInitializable, IDeviceProvider, IDisposable
    {
        public event Action devicesChanged;

        private List<InputDevice> _devices = new List<InputDevice>();

        public void Initialize()
        {
            InputDevices.deviceConnected     += OnDeviceChanged;
            InputDevices.deviceDisconnected  += OnDeviceChanged;
            InputDevices.deviceConfigChanged += OnDeviceChanged;
        }

        public void GetDevices(Dictionary<string, TrackedDevice> devices)
        {
            devices.Clear();

            InputDevices.GetDevices(_devices);

            foreach (InputDevice device in _devices)
            {
                if (!device.isValid) return;

                DeviceUse use = DeviceUse.Unknown;

                if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
                {
                    use = DeviceUse.Head;
                }
                else if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left))
                {
                    use = DeviceUse.LeftHand;
                }
                else if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right))
                {
                    use = DeviceUse.RightHand;
                }

                device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked);
                device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);

                devices.Add(device.name, new TrackedDevice(device.name, use, isTracked, position, rotation));
            }
        }

        public void Dispose()
        {
            InputDevices.deviceConnected     -= OnDeviceChanged;
            InputDevices.deviceDisconnected  -= OnDeviceChanged;
            InputDevices.deviceConfigChanged -= OnDeviceChanged;
        }

        private void OnDeviceChanged(InputDevice device)
        {
            devicesChanged?.Invoke();
        }
    }
}
