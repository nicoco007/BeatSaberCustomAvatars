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

		public static PosRot operator +(PosRot a, PosRot b) {
			return new PosRot(a.Position + b.Position, a.Rotation * b.Rotation);
		}

		public override string ToString()
		{
			return $"PosRot({{Position={Position}, Rotation={Rotation}}})";
		}
	}
}
