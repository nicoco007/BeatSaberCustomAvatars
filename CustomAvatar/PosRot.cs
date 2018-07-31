using UnityEngine;

namespace CustomAvatar
{
	public struct PosRot
	{
		public Vector3 Position { get; }
		public Quaternion Rotation { get; }

		public PosRot(Vector3 position, Quaternion rotation)
		{
			Position = position;
			Rotation = rotation;
		}
	}
}