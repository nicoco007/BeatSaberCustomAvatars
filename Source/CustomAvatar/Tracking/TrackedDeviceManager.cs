using System;
using System.Collections.Generic;
using System.Linq;
using CustomAvatar.Utilities;
using DynamicOpenVR;
using UnityEngine;
using UnityEngine.XR;

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

        // these should only trigger for nodes that are registered to a specific target, not all found XR nodes
        public event Action<TrackedDeviceState> deviceAdded;
        public event Action<TrackedDeviceState> deviceRemoved;

        private readonly HashSet<ulong> _foundDevices = new HashSet<ulong>();

        public void Update()
        {
            var nodeStates = new List<XRNodeState>();
            var unassignedDevices = new Queue<XRNodeState>();
            var serialNumbers = new string[0];
            InputTracking.GetNodeStates(nodeStates);

            if (OpenVRStatus.isRunning)
            {
                serialNumbers = OpenVRWrapper.GetTrackedDeviceSerialNumbers();
            }

            XRNodeState? headNodeState      = null;
            XRNodeState? leftHandNodeState  = null;
            XRNodeState? rightHandNodeState = null;
            XRNodeState? leftFootNodeState  = null;
            XRNodeState? rightFootNodeState = null;
            XRNodeState? waistNodeState     = null;

            int trackerCount = 0;

            foreach (XRNodeState nodeState in nodeStates)
            {
                if (!_foundDevices.Contains(nodeState.uniqueID))
                {
                    _foundDevices.Add(nodeState.uniqueID);
                    Plugin.logger.Debug($"Found new XR device of type \"{nodeState.nodeType}\" named \"{InputTracking.GetNodeName(nodeState.uniqueID)}\" with ID {nodeState.uniqueID}");
                }

                switch (nodeState.nodeType)
                {
                    case XRNode.CenterEye:
                        headNodeState = nodeState;
                        break;

                    case XRNode.LeftHand:
                        leftHandNodeState = nodeState;
                        break;

                    case XRNode.RightHand:
                        rightHandNodeState = nodeState;
                        break;

                    case XRNode.HardwareTracker:
                        if (OpenVRStatus.isRunning)
                        {
                            // try to figure out tracker role using OpenVR
                            string deviceName = InputTracking.GetNodeName(nodeState.uniqueID);
                            int openVRDeviceId = Array.FindIndex(serialNumbers, s => !string.IsNullOrEmpty(s) && deviceName.Contains(s));
                            var role = TrackedDeviceType.Unknown;
                            
                            if (openVRDeviceId != -1)
                            {
                                role = OpenVRWrapper.GetTrackedDeviceType((uint)openVRDeviceId);
                            }
                            
                            switch (role)
                            {
                                case TrackedDeviceType.LeftFoot:
                                    leftFootNodeState = nodeState;
                                    break;

                                case TrackedDeviceType.RightFoot:
                                    rightFootNodeState = nodeState;
                                    break;

                                case TrackedDeviceType.Waist:
                                    waistNodeState = nodeState;
                                    break;

                                default:
                                    unassignedDevices.Enqueue(nodeState);
                                    break;
                            }
                        }
                        else
                        {
                            unassignedDevices.Enqueue(nodeState);
                        }

                        trackerCount++;

                        break;
                }
            }

            // fallback if OpenVR tracker roles aren't set/supported
            if (leftFootNodeState == null && trackerCount >= 2 && unassignedDevices.Count > 0)
            {
                leftFootNodeState = unassignedDevices.Dequeue();
            }

            if (rightFootNodeState == null && trackerCount >= 2 && unassignedDevices.Count > 0)
            {
                rightFootNodeState = unassignedDevices.Dequeue();
            }

            if (waistNodeState == null && unassignedDevices.Count > 0)
            {
                waistNodeState = unassignedDevices.Dequeue();
            }

            UpdateTrackedDevice(head,      headNodeState,      nameof(head));
            UpdateTrackedDevice(leftHand,  leftHandNodeState,  nameof(leftHand));
            UpdateTrackedDevice(rightHand, rightHandNodeState, nameof(rightHand));
            UpdateTrackedDevice(leftFoot,  leftFootNodeState,  nameof(leftFoot));
            UpdateTrackedDevice(rightFoot, rightFootNodeState, nameof(rightFoot));
            UpdateTrackedDevice(waist,     waistNodeState,     nameof(waist));

            foreach (ulong id in _foundDevices.ToList())
            {
                if (!nodeStates.Exists(n => n.uniqueID == id))
                {
                    Plugin.logger.Debug($"Lost XR device with ID " + id);
                    _foundDevices.Remove(id);
                }
            }
        }

        private void UpdateTrackedDevice(TrackedDeviceState deviceState, XRNodeState? possibleNodeState, string use)
        {
            if (possibleNodeState == null || !possibleNodeState.Value.tracked)
            {
                if (deviceState.found)
                {
                    deviceState.position = default;
                    deviceState.rotation = default;
                    deviceState.found = false;
                    deviceState.nodeState = default;
                    Plugin.logger.Info($"Lost device with ID {deviceState.nodeState.uniqueID} that was used as {use}");
                    deviceRemoved?.Invoke(deviceState);
                }

                return;
            }

            var nodeState = (XRNodeState)possibleNodeState;
            ulong previousId = deviceState.nodeState.uniqueID;
            
            Vector3 origin = BeatSaberUtil.GetRoomCenter();
            Quaternion originRotation = BeatSaberUtil.GetRoomRotation();

            if (nodeState.TryGetPosition(out Vector3 position))
            {
                deviceState.position = origin + originRotation * position;
            }

            if (nodeState.TryGetRotation(out Quaternion rotation))
            {
                deviceState.rotation = originRotation * rotation;
            }

            deviceState.found = true;
            deviceState.nodeState = nodeState;

            if (nodeState.uniqueID != previousId)
            {
                Plugin.logger.Info($"Using device \"{InputTracking.GetNodeName(nodeState.uniqueID)}\" ({nodeState.uniqueID}) as {use}");
                deviceAdded?.Invoke(deviceState);
            }
        }
    }
}
