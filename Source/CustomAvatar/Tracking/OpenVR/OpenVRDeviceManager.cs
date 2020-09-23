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
using System.Linq;
using UnityEngine;
using Valve.VR;
using Zenject;

namespace CustomAvatar.Tracking.OpenVR
{
    internal class OpenVRDeviceManager : ITrackedDeviceManager, ITickable
    {
        public event Action<ITrackedDeviceState> deviceAdded;
        public event Action<ITrackedDeviceState> deviceRemoved;
        public event Action<ITrackedDeviceState> deviceTrackingAcquired;
        public event Action<ITrackedDeviceState> deviceTrackingLost;

        private readonly ETrackingResult[] _validTrackingResults = { ETrackingResult.Running_OK, ETrackingResult.Running_OutOfRange, ETrackingResult.Calibrating_OutOfRange };
        
        private readonly OpenVRDeviceState _head      = new OpenVRDeviceState(DeviceUse.Head);
        private readonly OpenVRDeviceState _leftHand  = new OpenVRDeviceState(DeviceUse.LeftHand);
        private readonly OpenVRDeviceState _rightHand = new OpenVRDeviceState(DeviceUse.RightHand);
        private readonly OpenVRDeviceState _waist     = new OpenVRDeviceState(DeviceUse.Waist);
        private readonly OpenVRDeviceState _leftFoot  = new OpenVRDeviceState(DeviceUse.LeftFoot);
        private readonly OpenVRDeviceState _rightFoot = new OpenVRDeviceState(DeviceUse.RightFoot);

        private readonly ILogger<OpenVRDeviceManager> _logger;
        private readonly OpenVRFacade _openVRFacade;

        private readonly TrackedDevicePose_t[] _poses = new TrackedDevicePose_t[OpenVRFacade.kMaxTrackedDeviceCount];
        private readonly OpenVRDevice[] _devices = new OpenVRDevice[OpenVRFacade.kMaxTrackedDeviceCount];

        public OpenVRDeviceManager(ILoggerProvider loggerProvider, OpenVRFacade openVRFacade)
        {
            _logger = loggerProvider.CreateLogger<OpenVRDeviceManager>();
            _openVRFacade = openVRFacade;
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

                case DeviceUse.Waist:
                    deviceState = _waist;
                    return true;

                case DeviceUse.LeftFoot:
                    deviceState = _leftFoot;
                    return true;

                case DeviceUse.RightFoot:
                    deviceState = _rightFoot;
                    return true;

                default:
                    deviceState = null;
                    return false;
            }
        }

        public void Tick()
        {
            _openVRFacade.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, _poses);

            if (DetectDeviceChanges())
            {
                AssignDeviceStates();
            }

            UpdateDeviceState(_head);
            UpdateDeviceState(_leftHand);
            UpdateDeviceState(_rightHand);
            UpdateDeviceState(_waist);
            UpdateDeviceState(_leftFoot);
            UpdateDeviceState(_rightFoot);
        }

        private bool DetectDeviceChanges()
        {
            bool changeDetected = false;

            for (uint i = 0; i < _poses.Length; i++)
            {
                ETrackedControllerRole controllerRole = _openVRFacade.GetControllerRoleForTrackedDeviceIndex(i);
                string role = _openVRFacade.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_ControllerType_String);

                if (_poses[i].bDeviceIsConnected != _devices[i].isConnected)
                {
                    if (_poses[i].bDeviceIsConnected)
                    {
                        ETrackedDeviceClass deviceClass = _openVRFacade.GetTrackedDeviceClass(i);
                        string modelName = _openVRFacade.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_ModelNumber_String);
                        string serialNumber = _openVRFacade.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_SerialNumber_String);

                        _devices[i].deviceClass = deviceClass;
                        _devices[i].modelName = modelName;
                        _devices[i].serialNumber = serialNumber;

                        _logger.Info($"Device '{_devices[i].modelName}' (class '{_devices[i].deviceClass}', serial number '{_devices[i].serialNumber}') connected at index {i}");
                    }
                    else
                    {
                        _logger.Info($"Device '{_devices[i].modelName}' (class '{_devices[i].deviceClass}', serial number '{_devices[i].serialNumber}') disconnected from index {i}");

                        _devices[i].deviceClass = ETrackedDeviceClass.Invalid;
                        _devices[i].modelName = null;
                        _devices[i].serialNumber = null;
                    }

                    changeDetected = true;
                }

                if (controllerRole != _devices[i].controllerRole)
                {
                    _logger.Trace($"Device {i} role changed from '{_devices[i].controllerRole}' to '{controllerRole}'");

                    changeDetected = true;
                }

                if (role != _devices[i].role)
                {
                    if (role == null)
                    {
                        _logger.Trace($"Device {i} role unset from '{_devices[i].role}'");
                    }
                    else if (_devices[i].role == null)
                    {
                        _logger.Trace($"Device {i} role set to '{role}'");
                    }
                    else
                    {
                        _logger.Trace($"Device {i} role changed from '{_devices[i].role}' to '{role}'");
                    }

                    changeDetected = true;
                }

                _devices[i].isConnected = _poses[i].bDeviceIsConnected;
                _devices[i].controllerRole = controllerRole;
                _devices[i].role = role;
            }

            return changeDetected;
        }

        private void AssignDeviceStates()
        {
            _logger.Info("Device change detected, updating devices");

            uint? head      = null;
            uint? leftHand  = null;
            uint? rightHand = null;
            uint? waist     = null;
            uint? leftFoot  = null;
            uint? rightFoot = null;

            for (uint deviceIndex = 0; deviceIndex < _devices.Length; deviceIndex++)
            {
                if (!_devices[deviceIndex].isConnected) continue;

                switch (_devices[deviceIndex].deviceClass)
                {
                    case ETrackedDeviceClass.HMD:
                        head = deviceIndex;
                        break;

                    case ETrackedDeviceClass.Controller:
                        switch (_devices[deviceIndex].controllerRole)
                        {
                            case ETrackedControllerRole.LeftHand:
                                leftHand = deviceIndex;
                                break;

                            case ETrackedControllerRole.RightHand:
                                rightHand = deviceIndex;
                                break;
                        }

                        break;

                    case ETrackedDeviceClass.GenericTracker:
                        switch (_devices[deviceIndex].role)
                        {
                            case "vive_tracker_waist":
                                waist = deviceIndex;
                                break;

                            case "vive_tracker_left_foot":
                                leftFoot = deviceIndex;
                                break;

                            case "vive_tracker_right_foot":
                                rightFoot = deviceIndex;
                                break;
                        }

                        break;
                }
            }

            AssignDeviceState(_head,      head);
            AssignDeviceState(_leftHand,  leftHand);
            AssignDeviceState(_rightHand, rightHand);
            AssignDeviceState(_waist,     waist);
            AssignDeviceState(_leftFoot,  leftFoot);
            AssignDeviceState(_rightFoot, rightFoot);
        }

        private void AssignDeviceState(OpenVRDeviceState deviceState, uint? possibleDeviceIndex)
        {
            // no valid device was found
            if (!possibleDeviceIndex.HasValue)
            {
                if (deviceState.isConnected)
                {
                    _logger.Info($"Deassigned device {deviceState.deviceIndex} that was previously used as {deviceState.use}");

                    deviceState.deviceIndex = null;
                    deviceState.isConnected = false;
                    deviceState.isTracking = false;
                    deviceState.position = Vector3.zero;
                    deviceState.rotation = Quaternion.identity;

                    deviceRemoved?.Invoke(deviceState);
                }

                return;
            }

            uint deviceIndex = possibleDeviceIndex.Value;

            // device is already assigned
            if (deviceIndex == deviceState.deviceIndex) return;

            // previous device existed but is different
            if (deviceState.deviceIndex.HasValue)
            {
                _logger.Info($"Deassigned device {deviceState.deviceIndex} that was previously used as {deviceState.use}");

                deviceRemoved?.Invoke(deviceState);
            }

            // assign new device
            deviceState.deviceIndex = deviceIndex;
            deviceState.isConnected = true;
            deviceState.isTracking = false;
            deviceState.position = Vector3.zero;
            deviceState.rotation = Quaternion.identity;

            _logger.Info($"Assigned device {deviceIndex} to {deviceState.use}");

            deviceAdded?.Invoke(deviceState);
        }

        private void UpdateDeviceState(OpenVRDeviceState deviceState)
        {
            if (!deviceState.deviceIndex.HasValue || !deviceState.isConnected) return;

            uint deviceIndex = deviceState.deviceIndex.Value;
            OpenVRDevice device = _devices[deviceIndex];
            TrackedDevicePose_t pose = _poses[deviceIndex];

            bool isTracking = pose.bPoseIsValid && _validTrackingResults.Contains(pose.eTrackingResult);

            if (deviceState.isTracking != isTracking)
            {
                if (isTracking)
                {
                    _logger.Info($"Acquired tracking of device {deviceIndex}");
                    deviceTrackingAcquired?.Invoke(deviceState);
                }
                else
                {
                    _logger.Info($"Lost tracking of device {deviceIndex}");
                    deviceTrackingLost?.Invoke(deviceState);
                }
            }

            deviceState.isTracking = isTracking;

            if (!isTracking) return;

            Vector3 position = _openVRFacade.GetPosition(pose.mDeviceToAbsoluteTracking);
            Quaternion rotation = _openVRFacade.GetRotation(pose.mDeviceToAbsoluteTracking);

            // Driver4VR rotation correction
            if (device.role.StartsWith("d4vr_tracker_") && (deviceState.use == DeviceUse.LeftFoot || deviceState.use == DeviceUse.RightFoot))
            {
                rotation *= Quaternion.Euler(-90, 180, 0);
            }

            // KinectToVR rotation correction
            if (device.role == "kinect_device")
            {
                if (deviceState.use == DeviceUse.Waist)
                {
                    rotation *= Quaternion.Euler(-90, 180, 0);
                }

                if (deviceState.use == DeviceUse.LeftFoot || deviceState.use == DeviceUse.RightFoot)
                {
                    rotation *= Quaternion.Euler(0, 180, 0);
                }
            }

            deviceState.position = position;
            deviceState.rotation = rotation;
        }

        private struct OpenVRDevice
        {
            public bool isConnected;
            public ETrackedControllerRole controllerRole;
            public ETrackedDeviceClass deviceClass;
            public string modelName;
            public string serialNumber;
            public string role;
        }
    }
}
