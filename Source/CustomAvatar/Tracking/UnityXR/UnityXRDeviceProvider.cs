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
using CustomAvatar.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Zenject;
using Pose = UnityEngine.XR.OpenXR.Input.Pose;

namespace CustomAvatar.Tracking.UnityXR
{
    internal class UnityXRDeviceProvider : IInitializable, IDeviceProvider, IDisposable
    {
        private readonly ILogger<UnityXRDeviceProvider> _logger;
        private readonly UnityXRHelper _unityXRHelper;

        private readonly PositionAndRotationXRDevice _head = new PositionAndRotationXRDevice(DeviceUse.Head);
        private readonly PositionAndRotationXRDevice _leftHand = new PositionAndRotationXRDevice(DeviceUse.LeftHand);
        private readonly PositionAndRotationXRDevice _rightHand = new PositionAndRotationXRDevice(DeviceUse.RightHand);
        private readonly PoseXRDevice _waist = new PoseXRDevice(DeviceUse.Waist);
        private readonly PoseXRDevice _leftFoot = new PoseXRDevice(DeviceUse.LeftFoot);
        private readonly PoseXRDevice _rightFoot = new PoseXRDevice(DeviceUse.RightFoot);

        private readonly InputActionMap _inputActions = new InputActionMap("Custom Avatars Additional Tracking");

        internal UnityXRDeviceProvider(ILogger<UnityXRDeviceProvider> logger, IVRPlatformHelper vrPlatformHelper)
        {
            _logger = logger;

            if (!(vrPlatformHelper is UnityXRHelper unityXRHelper))
            {
                _logger.LogError($"{nameof(UnityXRDeviceProvider)} expects {nameof(IVRPlatformHelper)} to be {nameof(UnityXRHelper)} but got {vrPlatformHelper.GetType().Name}");
                return;
            }

            _unityXRHelper = unityXRHelper;
        }

        public event Action devicesChanged;

        public void Initialize()
        {
            _head.positionAction = _unityXRHelper._headPositionActionReference.action;
            _head.rotationAction = _unityXRHelper._headOrientationActionReference.action;

            _head.isTrackedAction = CreateActionAndRegisterCallbacks("HeadIsTracked", $"<{nameof(XRHMD)}>/isTracked");
            _leftHand.isTrackedAction = CreateActionAndRegisterCallbacks("LeftHandIsTracked", $"<{nameof(XRController)}>{{{CommonUsages.LeftHand}}}/isTracked");
            _rightHand.isTrackedAction = CreateActionAndRegisterCallbacks("RightHandIsTracked", $"<{nameof(XRController)}>{{{CommonUsages.RightHand}}}/isTracked");
            _waist.isTrackedAction = CreateActionAndRegisterCallbacks("WaistIsTracked", "<XRTracker>{Waist}/isTracked");
            _leftFoot.isTrackedAction = CreateActionAndRegisterCallbacks("LeftFootIsTracked", "<XRTracker>{LeftFoot}/isTracked");
            _rightFoot.isTrackedAction = CreateActionAndRegisterCallbacks("RightFootIsTracked", "<XRTracker>{RightFoot}/isTracked");

            _waist.poseAction = CreateAction("WaistPose", "<XRTracker>{Waist}/devicePose");
            _leftFoot.poseAction = CreateAction("LeftFootPose", "<XRTracker>{LeftFoot}/devicePose");
            _rightFoot.poseAction = CreateAction("RightFootPose", "<XRTracker>{RightFoot}/devicePose");

            _inputActions.Enable();

            _unityXRHelper.controllersDidChangeReferenceEvent += OnControllersDidChangeReference;
            OnControllersDidChangeReference();
        }

        public bool TryGetDevice(DeviceUse deviceUse, out TrackedDevice trackedDevice)
        {
            switch (deviceUse)
            {
                case DeviceUse.Head:
                    trackedDevice = _head.GetDevice();
                    break;

                case DeviceUse.LeftHand:
                    trackedDevice = _leftHand.GetDevice();
                    break;

                case DeviceUse.RightHand:
                    trackedDevice = _rightHand.GetDevice();
                    break;

                case DeviceUse.Waist:
                    trackedDevice = _waist.GetDevice();
                    break;

                case DeviceUse.LeftFoot:
                    trackedDevice = _leftFoot.GetDevice();
                    break;

                case DeviceUse.RightFoot:
                    trackedDevice = _rightFoot.GetDevice();
                    break;

                default:
                    trackedDevice = default;
                    return false;
            }

            return true;
        }

        public void Dispose()
        {
            _unityXRHelper.controllersDidChangeReferenceEvent -= OnControllersDidChangeReference;

            DeregisterCallbacks(_head.isTrackedAction);
            DeregisterCallbacks(_leftHand.isTrackedAction);
            DeregisterCallbacks(_rightHand.isTrackedAction);
            DeregisterCallbacks(_waist.isTrackedAction);
            DeregisterCallbacks(_leftFoot.isTrackedAction);
            DeregisterCallbacks(_rightFoot.isTrackedAction);

            _inputActions.Disable();
        }

        private InputAction CreateActionAndRegisterCallbacks(string name, string bindingPath)
        {
            InputAction inputAction = CreateAction(name, bindingPath);

            // isTracked is a ButtonControl so "started" is triggered when tracking starts and "canceled" when tracking stops
            inputAction.started += OnInputActionChanged;
            inputAction.canceled += OnInputActionChanged;

            return inputAction;
        }

        private void DeregisterCallbacks(InputAction inputAction)
        {
            inputAction.started -= OnInputActionChanged;
            inputAction.canceled -= OnInputActionChanged;
        }

        private void OnInputActionChanged(InputAction.CallbackContext context)
        {
            devicesChanged?.Invoke();
        }

        private InputAction CreateAction(string name, string bindingPath)
        {
            InputAction inputAction = _inputActions.AddAction(name);
            inputAction.AddBinding(bindingPath, groups: "XR");
            return inputAction;
        }

        private void OnControllersDidChangeReference()
        {
            UnityXRController leftHandController = _unityXRHelper.ControllerFromNode(UnityEngine.XR.XRNode.LeftHand);
            _leftHand.positionAction = leftHandController?.positionAction;
            _leftHand.rotationAction = leftHandController?.rotationAction;

            UnityXRController rightHandController = _unityXRHelper.ControllerFromNode(UnityEngine.XR.XRNode.RightHand);
            _rightHand.positionAction = rightHandController?.positionAction;
            _rightHand.rotationAction = rightHandController?.rotationAction;
        }

        private abstract class XRDevice
        {
            public InputAction isTrackedAction { get; set; }

            public XRDevice(DeviceUse use)
            {
                this.use = use;
            }

            public DeviceUse use { get; }

            public abstract TrackedDevice GetDevice();
        }

        private class PositionAndRotationXRDevice : XRDevice
        {
            public PositionAndRotationXRDevice(DeviceUse use)
                : base(use)
            {
            }

            public InputAction positionAction { get; set; }

            public InputAction rotationAction { get; set; }

            public override TrackedDevice GetDevice()
            {
                return new TrackedDevice(use, isTrackedAction?.ReadValue<float>() > 0.5f, positionAction?.ReadValue<Vector3>() ?? Vector3.zero, rotationAction?.ReadValue<Quaternion>() ?? Quaternion.identity);
            }
        }

        private class PoseXRDevice : XRDevice
        {
            public InputAction poseAction { get; set; }

            public PoseXRDevice(DeviceUse use)
                : base(use)
            {
            }

            public override TrackedDevice GetDevice()
            {
                Pose pose = poseAction?.ReadValue<Pose>() ?? default;
                return new TrackedDevice(use, pose.isTracked, pose.position, pose.rotation);
            }
        }
    }
}
