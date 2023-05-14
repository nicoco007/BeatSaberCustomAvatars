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
using System.Collections.Generic;
using CustomAvatar.Logging;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
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

        private readonly InputActionMap _inputActions = new InputActionMap("Custom Avatars Full Body Tracking");

        internal UnityXRDeviceProvider(ILogger<UnityXRDeviceProvider> logger, IVRPlatformHelper vrPlatformHelper)
        {
            _logger = logger;

            if (!(vrPlatformHelper is UnityXRHelper unityXRHelper))
            {
                _logger.LogError($"{nameof(UnityXRDeviceProvider)} expects {nameof(IVRPlatformHelper)} to be {nameof(UnityXRHelper)} but got {vrPlatformHelper.GetType().Name}");
                return;
            }

            _unityXRHelper = unityXRHelper;

            _waist.poseAction = CreateAction("Waist Pose", "<XRViveTracker>{Waist}/devicePose");
            _leftFoot.poseAction = CreateAction("Left Foot Pose", "<XRViveTracker>{Left Foot}/devicePose");
            _rightFoot.poseAction = CreateAction("Right Foot Pose", "<XRViveTracker>{Right Foot}/devicePose");

            _inputActions.Enable();
        }

        public void Initialize()
        {
            _unityXRHelper.controllersDidChangeReferenceEvent += OnControllersDidChangeReference;

            _head.positionAction = _unityXRHelper.GetField<InputActionReference, UnityXRHelper>("_headPositionActionReference").action;
            _head.rotationAction = _unityXRHelper.GetField<InputActionReference, UnityXRHelper>("_headOrientationActionReference").action;

            OnControllersDidChangeReference();
        }

        public bool GetDevices(Dictionary<string, TrackedDevice> devices)
        {
            devices.Clear();

            (bool headChanged, TrackedDevice head) = _head.GetDevice();
            (bool leftHandChanged, TrackedDevice leftHand) = _leftHand.GetDevice();
            (bool rightHandChanged, TrackedDevice rightHand) = _rightHand.GetDevice();
            (bool waistChanged, TrackedDevice waist) = _waist.GetDevice();
            (bool leftFootChanged, TrackedDevice leftFoot) = _leftFoot.GetDevice();
            (bool rightFootChanged, TrackedDevice rightFoot) = _rightFoot.GetDevice();

            devices.Add(head.id, head);
            devices.Add(leftHand.id, leftHand);
            devices.Add(rightHand.id, rightHand);
            devices.Add(waist.id, waist);
            devices.Add(leftFoot.id, leftFoot);
            devices.Add(rightFoot.id, rightFoot);

            return headChanged || leftHandChanged || rightHandChanged || waistChanged || leftFootChanged || rightFootChanged;
        }

        public void Dispose()
        {
            _unityXRHelper.controllersDidChangeReferenceEvent -= OnControllersDidChangeReference;
        }

        private InputAction CreateAction(string name, string bindingPath)
        {
            InputAction inputAction = _inputActions.AddAction(name);
            inputAction.AddBinding(bindingPath, groups: "XR;PSVR2");
            return inputAction;
        }

        private void OnControllersDidChangeReference()
        {
            UnityXRController leftHandController = _unityXRHelper.ControllerFromNode(XRNode.LeftHand);
            _leftHand.positionAction = leftHandController?.positionAction;
            _leftHand.rotationAction = leftHandController?.rotationAction;

            UnityXRController rightHandController = _unityXRHelper.ControllerFromNode(XRNode.RightHand);
            _rightHand.positionAction = rightHandController?.positionAction;
            _rightHand.rotationAction = rightHandController?.rotationAction;
        }

        private abstract class XRDevice
        {
            protected bool wasPreviouslyTracked { get; set; }

            public XRDevice(DeviceUse use)
            {
                this.use = use;
            }

            public DeviceUse use { get; }

            public string name => use.ToString();

            public abstract (bool, TrackedDevice) GetDevice();
        }

        private class PositionAndRotationXRDevice : XRDevice
        {
            public PositionAndRotationXRDevice(DeviceUse use)
                : base(use)
            {
            }

            public InputAction positionAction { get; set; }

            public InputAction rotationAction { get; set; }

            public override (bool, TrackedDevice) GetDevice()
            {
                bool isTracked = positionAction != null && rotationAction != null;
                bool changed = wasPreviouslyTracked != isTracked;
                wasPreviouslyTracked = isTracked;

                return (changed, new TrackedDevice($"OpenXR {name}", use, isTracked, positionAction?.ReadValue<Vector3>() ?? Vector3.zero, rotationAction?.ReadValue<Quaternion>() ?? Quaternion.identity));
            }
        }

        private class PoseXRDevice : XRDevice
        {
            public InputAction poseAction { get; set; }

            public PoseXRDevice(DeviceUse use)
                : base(use)
            {
            }

            public override (bool, TrackedDevice) GetDevice()
            {
                Pose pose = poseAction?.ReadValue<Pose>() ?? default(Pose);

                bool changed = wasPreviouslyTracked != pose.isTracked;
                wasPreviouslyTracked = pose.isTracked;

                return (changed, new TrackedDevice($"OpenXR {name}", use, pose.isTracked, pose.position, pose.rotation));
            }
        }
    }
}
