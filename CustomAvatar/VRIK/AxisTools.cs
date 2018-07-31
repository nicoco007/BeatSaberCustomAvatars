using System;
using UnityEngine;

namespace AvatarScriptPack
{
	// Token: 0x02000164 RID: 356
	public class AxisTools
	{
		// Token: 0x06000940 RID: 2368 RVA: 0x00041017 File Offset: 0x0003F417
		public static Vector3 ToVector3(Axis axis)
		{
			if (axis == Axis.X)
			{
				return Vector3.right;
			}
			if (axis == Axis.Y)
			{
				return Vector3.up;
			}
			return Vector3.forward;
		}

		// Token: 0x06000941 RID: 2369 RVA: 0x00041038 File Offset: 0x0003F438
		public static Axis ToAxis(Vector3 v)
		{
			float num = Mathf.Abs(v.x);
			float num2 = Mathf.Abs(v.y);
			float num3 = Mathf.Abs(v.z);
			Axis result = Axis.X;
			if (num2 > num && num2 > num3)
			{
				result = Axis.Y;
			}
			if (num3 > num && num3 > num2)
			{
				result = Axis.Z;
			}
			return result;
		}

		// Token: 0x06000942 RID: 2370 RVA: 0x00041090 File Offset: 0x0003F490
		public static Axis GetAxisToPoint(Transform t, Vector3 worldPosition)
		{
			Vector3 axisVectorToPoint = AxisTools.GetAxisVectorToPoint(t, worldPosition);
			if (axisVectorToPoint == Vector3.right)
			{
				return Axis.X;
			}
			if (axisVectorToPoint == Vector3.up)
			{
				return Axis.Y;
			}
			return Axis.Z;
		}

		// Token: 0x06000943 RID: 2371 RVA: 0x000410CC File Offset: 0x0003F4CC
		public static Axis GetAxisToDirection(Transform t, Vector3 direction)
		{
			Vector3 axisVectorToDirection = AxisTools.GetAxisVectorToDirection(t, direction);
			if (axisVectorToDirection == Vector3.right)
			{
				return Axis.X;
			}
			if (axisVectorToDirection == Vector3.up)
			{
				return Axis.Y;
			}
			return Axis.Z;
		}

		// Token: 0x06000944 RID: 2372 RVA: 0x00041106 File Offset: 0x0003F506
		public static Vector3 GetAxisVectorToPoint(Transform t, Vector3 worldPosition)
		{
			return AxisTools.GetAxisVectorToDirection(t, worldPosition - t.position);
		}

		// Token: 0x06000945 RID: 2373 RVA: 0x0004111C File Offset: 0x0003F51C
		public static Vector3 GetAxisVectorToDirection(Transform t, Vector3 direction)
		{
			direction = direction.normalized;
			Vector3 result = Vector3.right;
			float num = Mathf.Abs(Vector3.Dot(Vector3.Normalize(t.right), direction));
			float num2 = Mathf.Abs(Vector3.Dot(Vector3.Normalize(t.up), direction));
			if (num2 > num)
			{
				result = Vector3.up;
			}
			float num3 = Mathf.Abs(Vector3.Dot(Vector3.Normalize(t.forward), direction));
			if (num3 > num && num3 > num2)
			{
				result = Vector3.forward;
			}
			return result;
		}
	}
}
