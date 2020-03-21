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
        public event Action<TrackedDeviceState, NodeUse> deviceAdded;
        public event Action<TrackedDeviceState, NodeUse> deviceRemoved;
        public event Action<TrackedDeviceState, NodeUse> deviceTrackingAcquired;
        public event Action<TrackedDeviceState, NodeUse> deviceTrackingLost;

        private readonly HashSet<ulong> _foundNodes = new HashSet<ulong>();

        private bool _isOpenVRRunning;

        public void Start()
        {
            _isOpenVRRunning = OpenVRUtilities.isInitialized;

            InputTracking.nodeAdded += node => UpdateNodes();
            InputTracking.nodeRemoved += node => UpdateNodes();

            UpdateNodes();
        }

        public void UpdateNodes()
        {
            var nodeStates = new List<XRNodeState>();
            var unassignedDevices = new Queue<XRNodeState>();
            var serialNumbers = new string[0];

            InputTracking.GetNodeStates(nodeStates);
            
            var deviceRoles = new Dictionary<ulong, TrackedDeviceRole>(nodeStates.Count);

            if (_isOpenVRRunning)
            {
                serialNumbers = OpenVRWrapper.GetTrackedDeviceSerialNumbers();
            }

            XRNodeState? headNodeState      = null;
            XRNodeState? leftHandNodeState  = null;
            XRNodeState? rightHandNodeState = null;
            XRNodeState? waistNodeState     = null;
            XRNodeState? leftFootNodeState  = null;
            XRNodeState? rightFootNodeState = null;

            int trackerCount = 0;

            foreach (XRNodeState nodeState in nodeStates)
            {
                deviceRoles.Add(nodeState.uniqueID, TrackedDeviceRole.Unknown);

                if (!_foundNodes.Contains(nodeState.uniqueID))
                {
                    Plugin.logger.Info($"Found new node {InputTracking.GetNodeName(nodeState.uniqueID)} with ID {nodeState.uniqueID}");
                    _foundNodes.Add(nodeState.uniqueID);
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
                        if (_isOpenVRRunning)
                        {
                            // try to figure out tracker role using OpenVR
                            string deviceName = InputTracking.GetNodeName(nodeState.uniqueID);
                            int openVRDeviceId = Array.FindIndex(serialNumbers, s => !string.IsNullOrEmpty(s) && deviceName.Contains(s));
                            var role = TrackedDeviceRole.Unknown;
                            
                            if (openVRDeviceId != -1)
                            {
                                role = OpenVRWrapper.GetTrackedDeviceRole((uint)openVRDeviceId);
                                deviceRoles[nodeState.uniqueID] = role;
                            }

                            Plugin.logger.Info($"Tracker {nodeState.uniqueID} has role {role}");

                            switch (role)
                            {
                                case TrackedDeviceRole.Waist:
                                    waistNodeState = nodeState;
                                    break;

                                case TrackedDeviceRole.LeftFoot:
                                    leftFootNodeState = nodeState;
                                    break;

                                case TrackedDeviceRole.RightFoot:
                                    rightFootNodeState = nodeState;
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

            AssignTrackedDevice(head,      headNodeState,      NodeUse.Head,      headNodeState.HasValue      ? deviceRoles[headNodeState.Value.uniqueID]      : TrackedDeviceRole.Unknown);
            AssignTrackedDevice(leftHand,  leftHandNodeState,  NodeUse.LeftHand,  leftHandNodeState.HasValue  ? deviceRoles[leftHandNodeState.Value.uniqueID]  : TrackedDeviceRole.Unknown);
            AssignTrackedDevice(rightHand, rightHandNodeState, NodeUse.RightHand, rightHandNodeState.HasValue ? deviceRoles[rightHandNodeState.Value.uniqueID] : TrackedDeviceRole.Unknown);
            AssignTrackedDevice(waist,     waistNodeState,     NodeUse.Waist,     waistNodeState.HasValue     ? deviceRoles[waistNodeState.Value.uniqueID]     : TrackedDeviceRole.Unknown);
            AssignTrackedDevice(leftFoot,  leftFootNodeState,  NodeUse.LeftFoot,  leftFootNodeState.HasValue  ? deviceRoles[leftFootNodeState.Value.uniqueID]  : TrackedDeviceRole.Unknown);
            AssignTrackedDevice(rightFoot, rightFootNodeState, NodeUse.RightFoot, rightFootNodeState.HasValue ? deviceRoles[rightFootNodeState.Value.uniqueID] : TrackedDeviceRole.Unknown);

            foreach (ulong uniqueID in _foundNodes.ToList())
            {
                if (!nodeStates.Exists(ns => ns.uniqueID == uniqueID))
                {
                    Plugin.logger.Info($"Lost node with ID {uniqueID}");
                    _foundNodes.Remove(uniqueID);
                }
            }
        }

        private void AssignTrackedDevice(TrackedDeviceState deviceState, XRNodeState? possibleNodeState, NodeUse use, TrackedDeviceRole deviceRole)
        {
            if (possibleNodeState.HasValue && !deviceState.found)
            {
                XRNodeState nodeState = possibleNodeState.Value;

                Plugin.logger.Info($"Using device \"{InputTracking.GetNodeName(nodeState.uniqueID)}\" ({nodeState.uniqueID}) as {use}");

                deviceState.uniqueID = nodeState.uniqueID;
                deviceState.name = InputTracking.GetNodeName(nodeState.uniqueID);
                deviceState.found = true;
                deviceState.role = deviceRole;
                
                deviceAdded?.Invoke(deviceState, use);
            }
            
            if (!possibleNodeState.HasValue && deviceState.found) {
                Plugin.logger.Info($"Lost device with ID {deviceState.uniqueID} that was used as {use}");

                deviceState.uniqueID = default;
                deviceState.name = null;
                deviceState.found = false;
                deviceState.role = TrackedDeviceRole.Unknown;

                deviceRemoved?.Invoke(deviceState, use);
            }
        }

        private void Update()
        {
            var nodeStates = new List<XRNodeState>();
            InputTracking.GetNodeStates(nodeStates);

            XRNodeState? headNodeState      = null;
            XRNodeState? leftHandNodeState  = null;
            XRNodeState? rightHandNodeState = null;
            XRNodeState? waistNodeState     = null;
            XRNodeState? leftFootNodeState  = null;
            XRNodeState? rightFootNodeState = null;

            foreach (XRNodeState nodeState in nodeStates)
            {
                if (nodeState.uniqueID == head.uniqueID) headNodeState = nodeState;
                if (nodeState.uniqueID == leftHand.uniqueID) leftHandNodeState = nodeState;
                if (nodeState.uniqueID == rightHand.uniqueID) rightHandNodeState = nodeState;
                if (nodeState.uniqueID == waist.uniqueID) waistNodeState = nodeState;
                if (nodeState.uniqueID == leftFoot.uniqueID) leftFootNodeState = nodeState;
                if (nodeState.uniqueID == rightFoot.uniqueID) rightFootNodeState = nodeState;
            }

            UpdateTrackedDevice(head,      headNodeState,      NodeUse.Head);
            UpdateTrackedDevice(leftHand,  leftHandNodeState,  NodeUse.LeftHand);
            UpdateTrackedDevice(rightHand, rightHandNodeState, NodeUse.RightHand);
            UpdateTrackedDevice(waist,     waistNodeState,     NodeUse.Waist);
            UpdateTrackedDevice(leftFoot,  leftFootNodeState,  NodeUse.LeftFoot);
            UpdateTrackedDevice(rightFoot, rightFootNodeState, NodeUse.RightFoot);
        }

        private void UpdateTrackedDevice(TrackedDeviceState deviceState, XRNodeState? possibleNodeState, NodeUse use)
        {
            if (!possibleNodeState.HasValue) return;

            var nodeState = possibleNodeState.Value;

            if (!nodeState.tracked)
            {
                if (deviceState.tracked)
                {
                    Plugin.logger.Info($"Lost tracking of device with ID {deviceState.uniqueID}");
                    deviceState.tracked = false;
                    deviceTrackingLost?.Invoke(deviceState, use);
                }

                return;
            }

            if (!deviceState.tracked)
            {
                Plugin.logger.Info($"Acquired tracking of device with ID {deviceState.uniqueID}");
                deviceState.tracked = true;
                deviceTrackingAcquired?.Invoke(deviceState, use);
            }
            
            Vector3 origin = BeatSaberUtil.GetRoomCenter();
            Quaternion originRotation = BeatSaberUtil.GetRoomRotation();

            if (nodeState.TryGetPosition(out Vector3 position))
            {
                deviceState.position = origin + originRotation * position;
            }

            if (nodeState.TryGetRotation(out Quaternion rotation))
            {
                deviceState.rotation = originRotation * rotation;

                // Driver4VR rotation correction
                if (deviceState.name?.StartsWith("d4vr_tracker_") == true && (use == NodeUse.LeftFoot || use == NodeUse.RightFoot))
                {
                    deviceState.rotation *= Quaternion.Euler(-90, 180, 0);
                }

                // KinectToVR rotation correction
                if (deviceState.role == TrackedDeviceRole.KinectToVrTracker)
                {
                    if (use == NodeUse.Waist)
                    {
                        deviceState.rotation *= Quaternion.Euler(-90, 180, 0);
                    }

                    if (use == NodeUse.LeftFoot || use == NodeUse.RightFoot)
                    {
                        deviceState.rotation *= Quaternion.Euler(0, 180, 0);
                    }
                }
            }
        }
    }
}
