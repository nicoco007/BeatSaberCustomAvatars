using System;
using System.Collections.Generic;
using System.Linq;
using CustomAvatar.Utilities;
using UnityEngine;
using UnityEngine.XR;

namespace CustomAvatar.Tracking
{
    internal class TrackedDeviceManager : MonoBehaviour
    {
        public TrackedDeviceState Head      { get; } = new TrackedDeviceState();
        public TrackedDeviceState LeftHand  { get; } = new TrackedDeviceState();
        public TrackedDeviceState RightHand { get; } = new TrackedDeviceState();
        public TrackedDeviceState LeftFoot  { get; } = new TrackedDeviceState();
        public TrackedDeviceState RightFoot { get; } = new TrackedDeviceState();
        public TrackedDeviceState Waist     { get; } = new TrackedDeviceState();

        // these should only trigger for nodes that are registered to a specific target, not all found XR nodes
        public event Action<TrackedDeviceState> DeviceAdded;
        public event Action<TrackedDeviceState> DeviceRemoved;

        private HashSet<ulong> foundDevices = new HashSet<ulong>();

        public void Update()
        {
            var nodeStates = new List<XRNodeState>();
            var unassignedDevices = new Queue<XRNodeState>();
            string[] serialNumbers = OpenVRWrapper.GetTrackedDeviceSerialNumbers();
            InputTracking.GetNodeStates(nodeStates);

            XRNodeState? head      = null;
            XRNodeState? leftHand  = null;
            XRNodeState? rightHand = null;
            XRNodeState? leftFoot  = null;
            XRNodeState? rightFoot = null;
            XRNodeState? waist     = null;

            int trackerCount = 0;

            for (int i = 0; i < nodeStates.Count; i++)
            {
                if (!foundDevices.Contains(nodeStates[i].uniqueID))
                {
                    foundDevices.Add(nodeStates[i].uniqueID);
                    Plugin.Logger.Debug($"Found new XR device of type \"{nodeStates[i].nodeType}\" named \"{InputTracking.GetNodeName(nodeStates[i].uniqueID)}\" with ID {nodeStates[i].uniqueID}");
                }

                switch (nodeStates[i].nodeType)
                {
                    case XRNode.CenterEye:
                        head = nodeStates[i];
                        break;

                    case XRNode.LeftHand:
                        leftHand = nodeStates[i];
                        break;

                    case XRNode.RightHand:
                        rightHand = nodeStates[i];
                        break;

                    case XRNode.HardwareTracker:
                        // try to figure out tracker role using OpenVR
                        string deviceName = InputTracking.GetNodeName(nodeStates[i].uniqueID);
                        uint openVRDeviceId = (uint)Array.FindIndex(serialNumbers, s => !string.IsNullOrEmpty(s) && deviceName.Contains(s));
                        TrackedDeviceType role = OpenVRWrapper.GetTrackedDeviceType(openVRDeviceId);

                        switch (role)
                        {
                            case TrackedDeviceType.LeftFoot:
                                leftFoot = nodeStates[i];
                                break;

                            case TrackedDeviceType.RightFoot:
                                rightFoot = nodeStates[i];
                                break;

                            case TrackedDeviceType.Waist:
                                waist = nodeStates[i];
                                break;

                            default:
	                            unassignedDevices.Enqueue(nodeStates[i]);
	                            break;
                        }

                        trackerCount++;

                        break;
                }
            }

            // fallback if OpenVR tracker roles aren't set/supported
            if (LeftFoot.Equals(default) && trackerCount >= 2 && unassignedDevices.Count > 0)
            {
                leftFoot = unassignedDevices.Dequeue();
            }

            if (RightFoot.Equals(default) && trackerCount >= 2 && unassignedDevices.Count > 0)
            {
                rightFoot = unassignedDevices.Dequeue();
            }

            if (Waist.Equals(default) && unassignedDevices.Count > 0)
            {
                waist = unassignedDevices.Dequeue();
            }

            UpdateTrackedDevice(Head,      head,      nameof(Head));
            UpdateTrackedDevice(LeftHand,  leftHand,  nameof(LeftHand));
            UpdateTrackedDevice(RightHand, rightHand, nameof(RightHand));
            UpdateTrackedDevice(LeftFoot,  leftFoot,  nameof(LeftFoot));
            UpdateTrackedDevice(RightFoot, rightFoot, nameof(RightFoot));
            UpdateTrackedDevice(Waist,     waist,     nameof(Waist));

            foreach (ulong id in foundDevices.ToList())
            {
                if (!nodeStates.Exists(n => n.uniqueID == id))
                {
                    Plugin.Logger.Debug($"Lost XR device with ID " + id);
                    foundDevices.Remove(id);
                }
            }
        }

        private void UpdateTrackedDevice(TrackedDeviceState deviceState, XRNodeState? possibleNodeState, string use)
        {
            if (possibleNodeState == null)
            {
                if (deviceState.Found)
                {
                    deviceState.Position = default;
                    deviceState.Rotation = default;
                    deviceState.Found = false;
                    deviceState.NodeState = default;
                    Plugin.Logger.Info($"Lost device with ID {deviceState.NodeState.uniqueID} that was used as {use}");
                    DeviceRemoved?.Invoke(deviceState);
                }

                return;
            }

            XRNodeState nodeState = (XRNodeState)possibleNodeState;
            ulong previousId = deviceState.NodeState.uniqueID;

            if (nodeState.TryGetPosition(out var position) && nodeState.TryGetRotation(out var rotation))
            {
                Vector3 origin = BeatSaberUtil.GetRoomCenter();
                Quaternion originRotation = BeatSaberUtil.GetRoomRotation();

                deviceState.Position = originRotation * position + originRotation * origin;
                deviceState.Rotation = originRotation * rotation;
            }

            deviceState.Found = true;
            deviceState.NodeState = nodeState;

            if (nodeState.uniqueID != previousId)
            {
                Plugin.Logger.Info($"Using device \"{InputTracking.GetNodeName(nodeState.uniqueID)}\" ({nodeState.uniqueID}) as {use}");
                DeviceAdded?.Invoke(deviceState);
            }
        }
    }
}
