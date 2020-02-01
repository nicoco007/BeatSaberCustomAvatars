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

        private readonly HashSet<ulong> _foundNodes = new HashSet<ulong>();

        private bool _isOpenVRRunning;

        public void Start()
        {
            _isOpenVRRunning = OpenVRStatus.isRunning;

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
                            var role = TrackedDeviceType.Unknown;
                            
                            if (openVRDeviceId != -1)
                            {
                                role = OpenVRWrapper.GetTrackedDeviceType((uint)openVRDeviceId);
                            }
                            
                            switch (role)
                            {
                                case TrackedDeviceType.Waist:
                                    waistNodeState = nodeState;
                                    break;

                                case TrackedDeviceType.LeftFoot:
                                    leftFootNodeState = nodeState;
                                    break;

                                case TrackedDeviceType.RightFoot:
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

            AssignTrackedDevice(head,      headNodeState,      nameof(head));
            AssignTrackedDevice(leftHand,  leftHandNodeState,  nameof(leftHand));
            AssignTrackedDevice(rightHand, rightHandNodeState, nameof(rightHand));
            AssignTrackedDevice(waist,     waistNodeState,     nameof(waist));
            AssignTrackedDevice(leftFoot,  leftFootNodeState,  nameof(leftFoot));
            AssignTrackedDevice(rightFoot, rightFootNodeState, nameof(rightFoot));

            foreach (ulong uniqueID in _foundNodes.ToList())
            {
                if (!nodeStates.Exists(ns => ns.uniqueID == uniqueID))
                {
                    Plugin.logger.Info($"Lost node with ID {uniqueID}");
                    _foundNodes.Remove(uniqueID);
                }
            }
        }

        private void AssignTrackedDevice(TrackedDeviceState deviceState, XRNodeState? possibleNodeState, string use)
        {
            if (possibleNodeState.HasValue && possibleNodeState.Value.tracked && !deviceState.found)
            {
                XRNodeState nodeState = possibleNodeState.Value;

                Plugin.logger.Info($"Using device \"{InputTracking.GetNodeName(nodeState.uniqueID)}\" ({nodeState.uniqueID}) as {use}");

                deviceState.uniqueID = nodeState.uniqueID;
                deviceState.found = true;
                
                deviceAdded?.Invoke(deviceState);
            }
            
            if (!(possibleNodeState.HasValue && possibleNodeState.Value.tracked) && deviceState.found) {
                Plugin.logger.Info($"Lost device with ID {deviceState.uniqueID} that was used as {use}");

                deviceState.uniqueID = default;
                deviceState.found = false;

                deviceRemoved?.Invoke(deviceState);
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

            UpdateTrackedDevice(head,      headNodeState,      nameof(head));
            UpdateTrackedDevice(leftHand,  leftHandNodeState,  nameof(leftHand));
            UpdateTrackedDevice(rightHand, rightHandNodeState, nameof(rightHand));
            UpdateTrackedDevice(waist,     waistNodeState,     nameof(head));
            UpdateTrackedDevice(leftFoot,  leftFootNodeState,  nameof(leftFoot));
            UpdateTrackedDevice(rightFoot, rightFootNodeState, nameof(rightFoot));
        }

        private void UpdateTrackedDevice(TrackedDeviceState deviceState, XRNodeState? possibleNodeState, string use)
        {
            if (!possibleNodeState.HasValue) return;

            var nodeState = possibleNodeState.Value;
            
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
        }
    }
}
