using UnityEngine;
using UnityEngine.XR;

namespace CustomAvatar.Tracking
{
    internal class TrackedDeviceState
    {
        public XRNodeState nodeState { get; set; }
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public bool found { get; set; }
    }
}
