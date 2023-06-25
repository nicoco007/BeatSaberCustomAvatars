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
using System.Linq;
using CustomAvatar.Logging;
using DynamicOpenVR.IO;
using UnityEngine;
using Valve.VR;
using Zenject;

namespace CustomAvatar.Tracking.OpenVR
{
    internal class OpenVRDeviceProvider : IDeviceProvider, ITickable
    {
        private static readonly ETrackingResult[] kValidTrackingResults = { ETrackingResult.Running_OK, ETrackingResult.Running_OutOfRange, ETrackingResult.Calibrating_OutOfRange };

        private readonly ILogger<OpenVRDeviceProvider> _logger;
        private readonly OpenVRFacade _openVRFacade;

        // from DynamicOpenVR.BeatSaber
        private readonly PoseInput _leftHandPose = new("/actions/main/in/left_hand_pose");
        private readonly PoseInput _rightHandPose = new("/actions/main/in/right_hand_pose");

        private readonly TrackedDevicePose_t[] _poses = new TrackedDevicePose_t[OpenVRFacade.kMaxTrackedDeviceCount];
        private readonly OpenVRDevice[] _devices = new OpenVRDevice[OpenVRFacade.kMaxTrackedDeviceCount];

        private OpenVRDevice _leftHandInput;
        private OpenVRDevice _rightHandInput;

        private uint? _head;
        private uint? _leftHand;
        private uint? _rightHand;
        private uint? _waist;
        private uint? _leftFoot;
        private uint? _rightFoot;

        public OpenVRDeviceProvider(ILogger<OpenVRDeviceProvider> logger, OpenVRFacade openVRFacade)
        {
            _logger = logger;
            _openVRFacade = openVRFacade;
        }

        public event Action devicesChanged;

        public bool TryGetDevice(DeviceUse deviceUse, out TrackedDevice trackedDevice)
        {
            OpenVRDevice device;

            if (deviceUse == DeviceUse.LeftHand)
            {
                device = _leftHandInput;
            }
            else if (deviceUse == DeviceUse.RightHand)
            {
                device = _rightHandInput;
            }
            else
            {
                uint? deviceIndex = deviceUse switch
                {
                    DeviceUse.Head => _head,
                    DeviceUse.LeftHand => _leftHand,
                    DeviceUse.RightHand => _rightHand,
                    DeviceUse.Waist => _waist,
                    DeviceUse.LeftFoot => _leftFoot,
                    DeviceUse.RightFoot => _rightFoot,
                    _ => null,
                };

                if (!deviceIndex.HasValue)
                {
                    trackedDevice = default;
                    return false;
                }

                device = _devices[deviceIndex.Value];
            }

            trackedDevice = new TrackedDevice(device.isTracking, device.position, device.rotation);
            return true;
        }

        public bool TryGetRenderModelPose(DeviceUse deviceUse, out TrackedDevice trackedDevice)
        {
            uint? deviceIndex = deviceUse switch
            {
                DeviceUse.Head => _head,
                DeviceUse.LeftHand => _leftHand,
                DeviceUse.RightHand => _rightHand,
                DeviceUse.Waist => _waist,
                DeviceUse.LeftFoot => _leftFoot,
                DeviceUse.RightFoot => _rightFoot,
                _ => null,
            };

            if (!deviceIndex.HasValue)
            {
                trackedDevice = default;
                return false;
            }

            OpenVRDevice device = _devices[deviceIndex.Value];
            trackedDevice = new TrackedDevice(device.isTracking, device.position, device.rotation);
            return true;
        }

        public void Tick()
        {
            bool changeDetected = false;

            _openVRFacade.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, _poses);

            _head = null;
            _leftHand = null;
            _rightHand = null;
            _waist = null;
            _leftFoot = null;
            _rightFoot = null;

            for (uint i = 0; i < _poses.Length; i++)
            {
                OpenVRDevice device = _devices[i];
                TrackedDevicePose_t pose = _poses[i];

                DeviceUse use = DeviceUse.Unknown;

                bool isConnected = device.isConnected;

                if (pose.bDeviceIsConnected != isConnected)
                {
                    isConnected = pose.bDeviceIsConnected;

                    _logger.LogInformation(isConnected ? $"Device {i} connected" : $"Device {i} disconnected");

                    changeDetected = true;
                }

                if (!isConnected)
                {
                    _devices[i] = default;
                    continue;
                }

                ETrackedDeviceClass deviceClass = _openVRFacade.GetTrackedDeviceClass(i);
                ETrackedControllerRole controllerRole = _openVRFacade.GetControllerRoleForTrackedDeviceIndex(i);
                string role = _openVRFacade.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_ControllerType_String);

                if (deviceClass != device.deviceClass)
                {
                    _logger.LogTrace($"Device {i} class changed from '{device.deviceClass}' to '{deviceClass}'");

                    changeDetected = true;
                }

                if (controllerRole != device.controllerRole)
                {
                    _logger.LogTrace($"Device {i} controller role changed from '{device.controllerRole}' to '{controllerRole}'");

                    changeDetected = true;
                }

                if (role != device.role)
                {
                    if (role == null)
                    {
                        _logger.LogTrace($"Device {i} role unset from '{device.role}'");
                    }
                    else if (device.role == null)
                    {
                        _logger.LogTrace($"Device {i} role set to '{role}'");
                    }
                    else
                    {
                        _logger.LogTrace($"Device {i} role changed from '{device.role}' to '{role}'");
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

                bool isTracking = pose.bPoseIsValid && kValidTrackingResults.Contains(pose.eTrackingResult);

                if (device.isTracking != isTracking)
                {
                    if (isTracking)
                    {
                        _logger.LogInformation($"Acquired tracking of device {i}");
                    }
                    else
                    {
                        _logger.LogInformation($"Lost tracking of device {i}");
                    }

                    changeDetected = true;
                }

                Vector3 position = Vector3.zero;
                Quaternion rotation = Quaternion.identity;

                if (isTracking)
                {
                    _openVRFacade.GetPositionAndRotation(pose.mDeviceToAbsoluteTracking, out position, out rotation);

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

                        if (use is DeviceUse.LeftFoot or DeviceUse.RightFoot)
                        {
                            rotation *= Quaternion.Euler(0, 180, 0);
                        }
                    }

                    switch (use)
                    {
                        case DeviceUse.Head:
                            _head = i;
                            break;

                        case DeviceUse.LeftHand:
                            _leftHand = i;
                            break;

                        case DeviceUse.RightHand:
                            _rightHand = i;
                            break;

                        case DeviceUse.Waist:
                            _waist = i;
                            break;

                        case DeviceUse.LeftFoot:
                            _leftFoot = i;
                            break;

                        case DeviceUse.RightFoot:
                            _rightFoot = i;
                            break;
                    }
                }

                _devices[i] = new OpenVRDevice(i, isConnected, isTracking, controllerRole, deviceClass, role, position, rotation);
            }

            CheckPoseInputChanged(ref _leftHandInput, _leftHandPose, ref changeDetected);
            CheckPoseInputChanged(ref _rightHandInput, _rightHandPose, ref changeDetected);

            if (changeDetected)
            {
                devicesChanged?.Invoke();
            }
        }

        private void CheckPoseInputChanged(ref OpenVRDevice prevDevice, PoseInput poseInput, ref bool changeDetected)
        {
            var device = new OpenVRDevice(0, poseInput.deviceConnected, poseInput.isTracking, ETrackedControllerRole.Invalid, ETrackedDeviceClass.Controller, null, poseInput.position, poseInput.rotation);

            if (prevDevice.isTracking != device.isTracking)
            {
                _logger.LogInformation($"{(device.isTracking ? "Acquired" : "Lost")} tracking of {nameof(PoseInput)} {poseInput.name}");
                changeDetected = true;
            }

            prevDevice = device;
        }

        private readonly struct OpenVRDevice
        {
            public readonly uint index;
            public readonly bool isConnected;
            public readonly bool isTracking;
            public readonly ETrackedControllerRole controllerRole;
            public readonly ETrackedDeviceClass deviceClass;
            public readonly string role;
            public readonly Vector3 position;
            public readonly Quaternion rotation;

            public OpenVRDevice(uint index, bool isConnected, bool isTracking, ETrackedControllerRole controllerRole, ETrackedDeviceClass deviceClass, string role, Vector3 position, Quaternion rotation)
            {
                this.index = index;
                this.isConnected = isConnected;
                this.isTracking = isTracking;
                this.controllerRole = controllerRole;
                this.deviceClass = deviceClass;
                this.role = role;
                this.position = position;
                this.rotation = rotation;
            }
        }
    }
}
