using System;
using System.Collections.Generic;
using System.Linq;
using CustomAvatar.Logging;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar.Tracking
{
    internal class TrackedDeviceManager : MonoBehaviour
    {
        public TrackedDeviceState head      { get; } = new TrackedDeviceState();
        public TrackedDeviceState leftHand  { get; } = new TrackedDeviceState();
        public TrackedDeviceState rightHand { get; } = new TrackedDeviceState();
        public TrackedDeviceState leftFoot  { get; } = new TrackedDeviceState();
        public TrackedDeviceState rightFoot { get; } = new TrackedDeviceState();
        public TrackedDeviceState waist     { get; } = new TrackedDeviceState();

        // these only trigger for devices that are registered to a specific target, not all found input devices
        public event Action<TrackedDeviceState, DeviceUse> deviceAdded;
        public event Action<TrackedDeviceState, DeviceUse> deviceRemoved;
        public event Action<TrackedDeviceState, DeviceUse> deviceTrackingAcquired;
        public event Action<TrackedDeviceState, DeviceUse> deviceTrackingLost;

        private readonly HashSet<string> _foundDevices = new HashSet<string>();

        private MainSettingsModelSO _mainSettingsModel;
        private ILogger _logger;

        private bool _isOpenVRRunning;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        [Inject]
        private void Inject(MainSettingsModelSO mainSettingsModel, ILoggerProvider loggerProvider)
        {
            _mainSettingsModel = mainSettingsModel;
            _logger = loggerProvider.CreateLogger<TrackedDeviceManager>();
        }

        public void Start()
        {
            try
            {
                _isOpenVRRunning = OpenVR.IsRuntimeInstalled();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to check if SteamVR is running; assuming it is not");
                _logger.Error(ex);
            }

            InputDevices.deviceConnected += device => UpdateInputDevices();
            InputDevices.deviceDisconnected += device => UpdateInputDevices();
            InputDevices.deviceConfigChanged += device => UpdateInputDevices();

            UpdateInputDevices();
        }

        private void Update()
        {
            var inputDevices = new List<InputDevice>();

            InputDevices.GetDevices(inputDevices);

            InputDevice? headInputDevice      = null;
            InputDevice? leftHandInputDevice  = null;
            InputDevice? rightHandInputDevice = null;
            InputDevice? waistInputDevice     = null;
            InputDevice? leftFootInputDevice  = null;
            InputDevice? rightFootInputDevice = null;

            foreach (InputDevice inputDevice in inputDevices)
            {
                if (inputDevice.name == head.name)      headInputDevice      = inputDevice;
                if (inputDevice.name == leftHand.name)  leftHandInputDevice  = inputDevice;
                if (inputDevice.name == rightHand.name) rightHandInputDevice = inputDevice;
                if (inputDevice.name == waist.name)     waistInputDevice     = inputDevice;
                if (inputDevice.name == leftFoot.name)  leftFootInputDevice  = inputDevice;
                if (inputDevice.name == rightFoot.name) rightFootInputDevice = inputDevice;
            }

            UpdateTrackedDevice(head,      headInputDevice,      DeviceUse.Head);
            UpdateTrackedDevice(leftHand,  leftHandInputDevice,  DeviceUse.LeftHand);
            UpdateTrackedDevice(rightHand, rightHandInputDevice, DeviceUse.RightHand);
            UpdateTrackedDevice(waist,     waistInputDevice,     DeviceUse.Waist);
            UpdateTrackedDevice(leftFoot,  leftFootInputDevice,  DeviceUse.LeftFoot);
            UpdateTrackedDevice(rightFoot, rightFootInputDevice, DeviceUse.RightFoot);
        }
        
        // ReSharper restore UnusedMember.Local
        #pragma warning restore IDE0051
        #endregion

        private void UpdateInputDevices()
        {
            var inputDevices = new List<InputDevice>();
            var unassignedDevices = new Queue<InputDevice>();
            var openVRDevicesBySerialNumber = new Dictionary<string, uint>();

            InputDevices.GetDevices(inputDevices);
            
            var deviceRoles = new Dictionary<InputDevice, TrackedDeviceRole>(inputDevices.Count);

            if (_isOpenVRRunning)
            {
                string[] serialNumbers = OpenVRWrapper.GetTrackedDeviceSerialNumbers();

                for (uint i = 0; i < serialNumbers.Length; i++)
                {
                    if (string.IsNullOrEmpty(serialNumbers[i])) continue;

                    _logger.Trace($"Got serial number '{serialNumbers[i]}' for device at index {i}");

                    if (!openVRDevicesBySerialNumber.ContainsKey(serialNumbers[i]))
                    {
                        openVRDevicesBySerialNumber.Add(serialNumbers[i], i);
                    }
                    else
                    {
                        _logger.Warning($"Got more than one device with serial number '{serialNumbers[i]}'");
                    }
                }
            }

            InputDevice? headInputDevice      = null;
            InputDevice? leftHandInputDevice  = null;
            InputDevice? rightHandInputDevice = null;
            InputDevice? waistInputDevice     = null;
            InputDevice? leftFootInputDevice  = null;
            InputDevice? rightFootInputDevice = null;

            int trackerCount = 0;

            foreach (InputDevice device in inputDevices)
            {
                if (!device.isValid) continue;

                deviceRoles.Add(device, TrackedDeviceRole.Unknown);

                if (!_foundDevices.Contains(device.name))
                {
                    _logger.Info($"Found new input device '{device.name}' with serial number '{device.serialNumber}'");
                    _foundDevices.Add(device.name);
                }

                if (device.HasCharacteristics(InputDeviceCharacteristics.HeadMounted))
                {
                    headInputDevice = device;
                }
                else if (device.HasCharacteristics(InputDeviceCharacteristics.HeldInHand |
                                                   InputDeviceCharacteristics.Left))
                {
                    leftHandInputDevice = device;
                }
                else if (device.HasCharacteristics(InputDeviceCharacteristics.HeldInHand |
                                                   InputDeviceCharacteristics.Right))
                {
                    rightHandInputDevice = device;
                }
                else if (device.HasCharacteristics(InputDeviceCharacteristics.TrackedDevice) && !device.HasCharacteristics(InputDeviceCharacteristics.TrackingReference))
                {
                    if (_isOpenVRRunning &&
                        !string.IsNullOrEmpty(device.serialNumber) &&
                        openVRDevicesBySerialNumber.TryGetValue(device.serialNumber, out uint openVRDeviceId))
                    {
                        // try to figure out tracker role using OpenVR
                        var role = OpenVRWrapper.GetTrackedDeviceRole(openVRDeviceId);
                        deviceRoles[device] = role;

                        _logger.Info($"Tracker '{device.name}' has role {role}");

                        switch (role)
                        {
                            case TrackedDeviceRole.Waist:
                                waistInputDevice = device;
                                break;

                            case TrackedDeviceRole.LeftFoot:
                                leftFootInputDevice = device;
                                break;

                            case TrackedDeviceRole.RightFoot:
                                rightFootInputDevice = device;
                                break;

                            default:
                                unassignedDevices.Enqueue(device);
                                break;
                        }
                    }
                    else
                    {
                        unassignedDevices.Enqueue(device);
                    }

                    trackerCount++;
                }
            }

            // fallback if OpenVR tracker roles aren't set/supported
            if (leftFootInputDevice == null && trackerCount >= 2 && unassignedDevices.Count > 0)
            {
                leftFootInputDevice = unassignedDevices.Dequeue();
            }

            if (rightFootInputDevice == null && trackerCount >= 2 && unassignedDevices.Count > 0)
            {
                rightFootInputDevice = unassignedDevices.Dequeue();
            }

            if (waistInputDevice == null && unassignedDevices.Count > 0)
            {
                waistInputDevice = unassignedDevices.Dequeue();
            }

            AssignTrackedDevice(head,      headInputDevice,      DeviceUse.Head,      headInputDevice.HasValue      ? deviceRoles[headInputDevice.Value]      : TrackedDeviceRole.Unknown);
            AssignTrackedDevice(leftHand,  leftHandInputDevice,  DeviceUse.LeftHand,  leftHandInputDevice.HasValue  ? deviceRoles[leftHandInputDevice.Value]  : TrackedDeviceRole.Unknown);
            AssignTrackedDevice(rightHand, rightHandInputDevice, DeviceUse.RightHand, rightHandInputDevice.HasValue ? deviceRoles[rightHandInputDevice.Value] : TrackedDeviceRole.Unknown);
            AssignTrackedDevice(waist,     waistInputDevice,     DeviceUse.Waist,     waistInputDevice.HasValue     ? deviceRoles[waistInputDevice.Value]     : TrackedDeviceRole.Unknown);
            AssignTrackedDevice(leftFoot,  leftFootInputDevice,  DeviceUse.LeftFoot,  leftFootInputDevice.HasValue  ? deviceRoles[leftFootInputDevice.Value]  : TrackedDeviceRole.Unknown);
            AssignTrackedDevice(rightFoot, rightFootInputDevice, DeviceUse.RightFoot, rightFootInputDevice.HasValue ? deviceRoles[rightFootInputDevice.Value] : TrackedDeviceRole.Unknown);

            foreach (string deviceName in _foundDevices.ToList())
            {
                if (!inputDevices.Exists(d => d.name == deviceName))
                {
                    _logger.Info($"Lost device '{deviceName}'");
                    _foundDevices.Remove(deviceName);
                }
            }
        }

        private void AssignTrackedDevice(TrackedDeviceState deviceState, InputDevice? possibleInputDevice, DeviceUse use, TrackedDeviceRole deviceRole)
        {
            if ((!possibleInputDevice.HasValue && deviceState.found) || (possibleInputDevice.HasValue && deviceState.found && possibleInputDevice.Value.name != deviceState.name)) {
                _logger.Info($"Removing device '{deviceState.name}' that was used as {use}");

                deviceState.name = null;
                deviceState.serialNumber = null;
                deviceState.found = false;
                deviceState.tracked = false;
                deviceState.role = TrackedDeviceRole.Unknown;

                deviceRemoved?.Invoke(deviceState, use);
            }

            if (possibleInputDevice.HasValue && (!deviceState.found || possibleInputDevice.Value.name != deviceState.name))
            {
                InputDevice inputDevice = possibleInputDevice.Value;

                _logger.Info($"Using device '{inputDevice.name}' as {use}");

                deviceState.name = inputDevice.name;
                deviceState.serialNumber = inputDevice.serialNumber;
                deviceState.found = true;
                deviceState.role = deviceRole;
                
                deviceAdded?.Invoke(deviceState, use);
            }
        }

        private void UpdateTrackedDevice(TrackedDeviceState deviceState, InputDevice? possibleInputDevice, DeviceUse use)
        {
            if (!possibleInputDevice.HasValue) return;

            var inputDevice = possibleInputDevice.Value;

            if (!inputDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) || !isTracked)
            {
                if (deviceState.tracked)
                {
                    _logger.Info($"Lost tracking of device '{deviceState.name}'");
                    deviceState.tracked = false;
                    deviceTrackingLost?.Invoke(deviceState, use);
                }

                return;
            }

            if (!deviceState.tracked)
            {
                _logger.Info($"Acquired tracking of device '{deviceState.name}'");
                deviceState.tracked = true;
                deviceTrackingAcquired?.Invoke(deviceState, use);
            }
            
            Vector3 origin = _mainSettingsModel.roomCenter.value;
            Quaternion originRotation = Quaternion.Euler(0, _mainSettingsModel.roomRotation.value, 0);

            if (inputDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            {
                deviceState.position = origin + originRotation * position;
            }

            if (inputDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                deviceState.rotation = originRotation * rotation;

                // Driver4VR rotation correction
                if (deviceState.name?.StartsWith("d4vr_tracker_") == true && (use == DeviceUse.LeftFoot || use == DeviceUse.RightFoot))
                {
                    deviceState.rotation *= Quaternion.Euler(-90, 180, 0);
                }

                // KinectToVR rotation correction
                if (deviceState.role == TrackedDeviceRole.KinectToVrTracker)
                {
                    if (use == DeviceUse.Waist)
                    {
                        deviceState.rotation *= Quaternion.Euler(-90, 180, 0);
                    }

                    if (use == DeviceUse.LeftFoot || use == DeviceUse.RightFoot)
                    {
                        deviceState.rotation *= Quaternion.Euler(0, 180, 0);
                    }
                }
            }
        }
    }
}
