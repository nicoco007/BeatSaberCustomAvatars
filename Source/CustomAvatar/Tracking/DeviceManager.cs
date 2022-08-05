//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Logging;
using System;
using System.Collections.Generic;
using Zenject;

namespace CustomAvatar.Tracking
{
    internal class DeviceManager : ITickable
    {
        public event Action devicesChanged;

        private string _head;
        private string _leftHand;
        private string _rightHand;
        private string _waist;
        private string _leftFoot;
        private string _rightFoot;

        private readonly ILogger<DeviceManager> _logger;
        private readonly IDeviceProvider _deviceProvider;

        private readonly Dictionary<string, TrackedDevice> _devices = new Dictionary<string, TrackedDevice>();

        public DeviceManager(ILogger<DeviceManager> logger, IDeviceProvider deviceProvider)
        {
            _logger = logger;
            _deviceProvider = deviceProvider;
        }

        public bool TryGetDeviceState(DeviceUse use, out TrackedDevice device)
        {
            switch (use)
            {
                case DeviceUse.Head:
                    return TryGetDevice(_head, out device);

                case DeviceUse.LeftHand:
                    return TryGetDevice(_leftHand, out device);

                case DeviceUse.RightHand:
                    return TryGetDevice(_rightHand, out device);

                case DeviceUse.Waist:
                    return TryGetDevice(_waist, out device);

                case DeviceUse.LeftFoot:
                    return TryGetDevice(_leftFoot, out device);

                case DeviceUse.RightFoot:
                    return TryGetDevice(_rightFoot, out device);

                default:
                    device = default;
                    return false;
            }
        }

        public void Tick()
        {
            if (_deviceProvider.GetDevices(_devices))
            {
                AssignDevices();
                devicesChanged?.Invoke();
            }
        }

        private bool TryGetDevice(string id, out TrackedDevice device)
        {
            if (string.IsNullOrEmpty(id) || !_devices.ContainsKey(id))
            {
                device = default;
                return false;
            }

            device = _devices[id];
            return true;
        }

        private void AssignDevices()
        {
            _logger.Info("Device change detected, updating device assignments");

            string head = null;
            string leftHand = null;
            string rightHand = null;
            string waist = null;
            string leftFoot = null;
            string rightFoot = null;

            foreach (TrackedDevice device in _devices.Values)
            {
                _logger.Trace($"Got device '{device.id}'");

                switch (device.deviceUse)
                {
                    case DeviceUse.Head:
                        head = device.id;
                        break;

                    case DeviceUse.LeftHand:
                        leftHand = device.id;
                        break;

                    case DeviceUse.RightHand:
                        rightHand = device.id;
                        break;

                    case DeviceUse.Waist:
                        waist = device.id;
                        break;

                    case DeviceUse.LeftFoot:
                        leftFoot = device.id;
                        break;

                    case DeviceUse.RightFoot:
                        rightFoot = device.id;
                        break;
                }
            }

            AssignDevice(ref _head, head, DeviceUse.Head);
            AssignDevice(ref _leftHand, leftHand, DeviceUse.LeftHand);
            AssignDevice(ref _rightHand, rightHand, DeviceUse.RightHand);
            AssignDevice(ref _waist, waist, DeviceUse.Waist);
            AssignDevice(ref _leftFoot, leftFoot, DeviceUse.LeftFoot);
            AssignDevice(ref _rightFoot, rightFoot, DeviceUse.RightFoot);
        }

        private void AssignDevice(ref string current, string potential, DeviceUse use)
        {
            if (current == potential) return;

            if (string.IsNullOrEmpty(potential))
            {
                _logger.Info($"Lost device '{current}' that was used as {use}");

                current = null;
            }
            else
            {
                if (current != null)
                {
                    _logger.Info($"Replacing device '{current}' with '{potential}' as {use}");
                }
                else
                {
                    _logger.Info($"Using device '{potential}' as {use}");
                }

                current = potential;
            }
        }
    }
}
