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
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Tracking
{
    internal class GenericDeviceProvider : IInitializable, IDeviceProvider, IDisposable
    {
        private readonly IVRPlatformHelper _platformHelper;

        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private GenericDeviceProvider(IVRPlatformHelper platformHelper)
        {
            _platformHelper = platformHelper;
        }

        public event Action devicesChanged;

        public void Initialize()
        {
            _platformHelper.controllersDidChangeReferenceEvent += OnControllersDidChangeReference;
        }

        public void Dispose()
        {
            _platformHelper.controllersDidChangeReferenceEvent -= OnControllersDidChangeReference;
        }

        public bool TryGetDevice(DeviceUse deviceUse, out TrackedDevice trackedDevice)
        {
            XRNode node;

            switch (deviceUse)
            {
                case DeviceUse.Head:
                    node = XRNode.Head;
                    break;

                case DeviceUse.LeftHand:
                    node = XRNode.LeftHand;
                    break;

                case DeviceUse.RightHand:
                    node = XRNode.RightHand;
                    break;

                default:
                    trackedDevice = default;
                    return false;
            }

            if (!_platformHelper.GetNodePose(node, 0, out Vector3 position, out Quaternion rotation))
            {
                trackedDevice = default;
                return false;
            }

            trackedDevice = new TrackedDevice(deviceUse, true, position, rotation);
            return true;
        }

        private void OnControllersDidChangeReference()
        {
            devicesChanged?.Invoke();
        }
    }
}
