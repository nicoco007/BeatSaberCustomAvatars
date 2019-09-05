using UnityEngine;
using UnityEngine.XR;

namespace CustomAvatar
{
	public class TrackedDeviceState
	{
		public XRNodeState NodeState { get; set; }
		public Vector3 Position { get; set; }
		public Quaternion Rotation { get; set; }
		public bool Found { get; set; }

		public override string ToString()
		{
			return $"TrackedDeviceState({{Position={Position}, Rotation={Rotation}, Exists={Found}}})";
		}
	}
}
