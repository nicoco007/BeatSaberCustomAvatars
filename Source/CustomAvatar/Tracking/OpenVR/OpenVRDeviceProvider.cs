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
using System.Linq;
using UnityEngine;
using Valve.VR;

namespace CustomAvatar.Tracking.OpenVR
{
    internal class OpenVRDeviceProvider : IDeviceProvider
    {
        public event Action devicesChanged;

        private readonly ETrackingResult[] _validTrackingResults = { ETrackingResult.Running_OK, ETrackingResult.Running_OutOfRange, ETrackingResult.Calibrating_OutOfRange };

        private readonly ILogger<OpenVRDeviceProvider> _logger;
        private readonly OpenVRFacade _openVRFacade;

        private readonly TrackedDevicePose_t[] _poses = new TrackedDevicePose_t[OpenVRFacade.kMaxTrackedDeviceCount];
        private readonly OpenVRDevice[] _devices = new OpenVRDevice[OpenVRFacade.kMaxTrackedDeviceCount];

        public OpenVRDeviceProvider(ILoggerProvider loggerProvider, OpenVRFacade openVRFacade)
        {
            _logger = loggerProvider.CreateLogger<OpenVRDeviceProvider>();
            _openVRFacade = openVRFacade;
        }

        public void GetDevices(Dictionary<string, TrackedDevice> devices)
        {
            devices.Clear();
            bool changeDetected = false;

            _openVRFacade.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, _poses);

            for (uint i = 0; i < _poses.Length; i++)
            {
                DeviceUse use = DeviceUse.Unknown;

                bool isConnected = _devices[i].isConnected;
                string modelName = _devices[i].modelName;
                string serialNumber = _devices[i].serialNumber;

                ETrackedDeviceClass deviceClass = _openVRFacade.GetTrackedDeviceClass(i);
                ETrackedControllerRole controllerRole = _openVRFacade.GetControllerRoleForTrackedDeviceIndex(i);
                string role = _openVRFacade.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_ControllerType_String);

                if (_poses[i].bDeviceIsConnected != isConnected)
                {
                    isConnected = _poses[i].bDeviceIsConnected;

                    if (_poses[i].bDeviceIsConnected)
                    {
                        modelName = _openVRFacade.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_ModelNumber_String);
                        serialNumber = _openVRFacade.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_SerialNumber_String);

                        _logger.Info($"Device '{modelName}' (class '{deviceClass}', serial number '{serialNumber}') connected at index {i}");
                    }
                    else
                    {
                        _logger.Info($"Device '{modelName}' (class '{deviceClass}', serial number '{serialNumber}') disconnected from index {i}");

                        modelName = null;
                        serialNumber = null;
                    }

                    changeDetected = true;
                }

                if (deviceClass != _devices[i].deviceClass)
                {
                    _logger.Trace($"Device '{serialNumber}' class changed from '{_devices[i].deviceClass}' to '{deviceClass}'");

                    changeDetected = true;
                }

                if (controllerRole != _devices[i].controllerRole)
                {
                    _logger.Trace($"Device '{serialNumber}' role changed from '{_devices[i].controllerRole}' to '{controllerRole}'");

                    changeDetected = true;
                }

                if (role != _devices[i].role)
                {
                    if (role == null)
                    {
                        _logger.Trace($"Device '{serialNumber}' role unset from '{_devices[i].role}'");
                    }
                    else if (_devices[i].role == null)
                    {
                        _logger.Trace($"Device '{serialNumber}' role set to '{role}'");
                    }
                    else
                    {
                        _logger.Trace($"Device '{serialNumber}' role changed from '{_devices[i].role}' to '{role}'");
                    }

                    changeDetected = true;
                }

                switch (deviceClass)
                {
                    case ETrackedDeviceClass.HMD:
                        use = DeviceUse.Head;
                        break;

                    case ETrackedDeviceClass.Controller:
                        switch (controllerRole)
                        {
                            case ETrackedControllerRole.LeftHand:
                                use = DeviceUse.LeftHand;
                                break;

                            case ETrackedControllerRole.RightHand:
                                use = DeviceUse.RightHand;
                                break;
                        }

                        break;

                    case ETrackedDeviceClass.GenericTracker:
                        switch (role)
                        {
                            case "vive_tracker_waist":
                                use = DeviceUse.Waist;
                                break;

                            case "vive_tracker_left_foot":
                                use = DeviceUse.LeftFoot;
                                break;

                            case "vive_tracker_right_foot":
                                use = DeviceUse.RightFoot;
                                break;
                        }

                        break;
                }

                bool isTracking = _poses[i].bPoseIsValid && _validTrackingResults.Contains(_poses[i].eTrackingResult);

                if (_devices[i].isTracking != isTracking)
                {
                    if (isTracking)
                    {
                        _logger.Info($"Acquired tracking of device '{serialNumber}'");
                    }
                    else
                    {
                        _logger.Info($"Lost tracking of device '{serialNumber}'");
                    }

                    changeDetected = true;
                }

                Vector3 position = Vector3.zero;
                Quaternion rotation = Quaternion.identity;

                if (isTracking)
                {
                    position = _openVRFacade.GetPosition(_poses[i].mDeviceToAbsoluteTracking);
                    rotation = _openVRFacade.GetRotation(_poses[i].mDeviceToAbsoluteTracking);

                    // Driver4VR rotation correction
                    if (role.StartsWith("d4vr_tracker_") && (use == DeviceUse.LeftFoot || use == DeviceUse.RightFoot))
                    {
                        rotation *= Quaternion.Euler(-90, 180, 0);
                    }

                    // KinectToVR rotation correction
                    if (role == "kinect_device")
                    {
                        if (use == DeviceUse.Waist)
                        {
                            rotation *= Quaternion.Euler(-90, 180, 0);
                        }

                        if (use == DeviceUse.LeftFoot || use == DeviceUse.RightFoot)
                        {
                            rotation *= Quaternion.Euler(0, 180, 0);
                        }
                    }
                }

                _devices[i] = new OpenVRDevice(isConnected, isTracking, controllerRole, deviceClass, modelName, serialNumber, role);

                if (isConnected)
                {
                    devices.Add(serialNumber, new TrackedDevice(serialNumber, use, isTracking, position, rotation));
                }
            }

            if (changeDetected) devicesChanged?.Invoke();
        }

        private struct OpenVRDevice
        {
            public readonly bool isConnected;
            public readonly bool isTracking;
            public readonly ETrackedControllerRole controllerRole;
            public readonly ETrackedDeviceClass deviceClass;
            public readonly string modelName;
            public readonly string serialNumber;
            public readonly string role;

            public OpenVRDevice(bool isConnected, bool isTracking, ETrackedControllerRole controllerRole, ETrackedDeviceClass deviceClass, string modelName, string serialNumber, string role)
            {
                this.isConnected = isConnected;
                this.isTracking = isTracking;
                this.controllerRole = controllerRole;
                this.deviceClass = deviceClass;
                this.modelName = modelName;
                this.serialNumber = serialNumber;
                this.role = role;
            }
        }
    }
}
