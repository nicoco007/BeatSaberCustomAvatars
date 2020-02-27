using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class TrackedDeviceState
    {
        public ulong uniqueID { get; set; }
        public string name { get; set; }
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public bool found { get; set; }
        public bool tracked { get; set; }
        public TrackedDeviceRole role { get; set; }
    }
}
