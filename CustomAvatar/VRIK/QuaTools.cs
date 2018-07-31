using System;
using UnityEngine;

namespace AvatarScriptPack
{
	// Token: 0x02000173 RID: 371
	public static class QuaTools
	{
		// Token: 0x060009B7 RID: 2487 RVA: 0x00043D4F File Offset: 0x0004214F
		public static Quaternion Lerp(Quaternion fromRotation, Quaternion toRotation, float weight)
		{
			if (weight <= 0f)
			{
				return fromRotation;
			}
			if (weight >= 1f)
			{
				return toRotation;
			}
			return Quaternion.Lerp(fromRotation, toRotation, weight);
		}

		// Token: 0x060009B8 RID: 2488 RVA: 0x00043D73 File Offset: 0x00042173
		public static Quaternion Slerp(Quaternion fromRotation, Quaternion toRotation, float weight)
		{
			if (weight <= 0f)
			{
				return fromRotation;
			}
			if (weight >= 1f)
			{
				return toRotation;
			}
			return Quaternion.Slerp(fromRotation, toRotation, weight);
		}

		// Token: 0x060009B9 RID: 2489 RVA: 0x00043D97 File Offset: 0x00042197
		public static Quaternion LinearBlend(Quaternion q, float weight)
		{
			if (weight <= 0f)
			{
				return Quaternion.identity;
			}
			if (weight >= 1f)
			{
				return q;
			}
			return Quaternion.Lerp(Quaternion.identity, q, weight);
		}

		// Token: 0x060009BA RID: 2490 RVA: 0x00043DC3 File Offset: 0x000421C3
		public static Quaternion SphericalBlend(Quaternion q, float weight)
		{
			if (weight <= 0f)
			{
				return Quaternion.identity;
			}
			if (weight >= 1f)
			{
				return q;
			}
			return Quaternion.Slerp(Quaternion.identity, q, weight);
		}

		// Token: 0x060009BB RID: 2491 RVA: 0x00043DF0 File Offset: 0x000421F0
		public static Quaternion FromToAroundAxis(Vector3 fromDirection, Vector3 toDirection, Vector3 axis)
		{
			Quaternion quaternion = Quaternion.FromToRotation(fromDirection, toDirection);
			float num = 0f;
			Vector3 zero = Vector3.zero;
			quaternion.ToAngleAxis(out num, out zero);
			float num2 = Vector3.Dot(zero, axis);
			if (num2 < 0f)
			{
				num = -num;
			}
			return Quaternion.AngleAxis(num, axis);
		}

		// Token: 0x060009BC RID: 2492 RVA: 0x00043E39 File Offset: 0x00042239
		public static Quaternion RotationToLocalSpace(Quaternion space, Quaternion rotation)
		{
			return Quaternion.Inverse(Quaternion.Inverse(space) * rotation);
		}

		// Token: 0x060009BD RID: 2493 RVA: 0x00043E4C File Offset: 0x0004224C
		public static Quaternion FromToRotation(Quaternion from, Quaternion to)
		{
			if (to == from)
			{
				return Quaternion.identity;
			}
			return to * Quaternion.Inverse(from);
		}

		// Token: 0x060009BE RID: 2494 RVA: 0x00043E6C File Offset: 0x0004226C
		public static Vector3 GetAxis(Vector3 v)
		{
			Vector3 vector = Vector3.right;
			bool flag = false;
			float num = Vector3.Dot(v, Vector3.right);
			float num2 = Mathf.Abs(num);
			if (num < 0f)
			{
				flag = true;
			}
			float num3 = Vector3.Dot(v, Vector3.up);
			float num4 = Mathf.Abs(num3);
			if (num4 > num2)
			{
				num2 = num4;
				vector = Vector3.up;
				flag = (num3 < 0f);
			}
			float num5 = Vector3.Dot(v, Vector3.forward);
			num4 = Mathf.Abs(num5);
			if (num4 > num2)
			{
				vector = Vector3.forward;
				flag = (num5 < 0f);
			}
			if (flag)
			{
				vector = -vector;
			}
			return vector;
		}

		// Token: 0x060009BF RID: 2495 RVA: 0x00043F10 File Offset: 0x00042310
		public static Quaternion ClampRotation(Quaternion rotation, float clampWeight, int clampSmoothing)
		{
			if (clampWeight >= 1f)
			{
				return Quaternion.identity;
			}
			if (clampWeight <= 0f)
			{
				return rotation;
			}
			float num = Quaternion.Angle(Quaternion.identity, rotation);
			float num2 = 1f - num / 180f;
			float num3 = Mathf.Clamp(1f - (clampWeight - num2) / (1f - num2), 0f, 1f);
			float num4 = Mathf.Clamp(num2 / clampWeight, 0f, 1f);
			for (int i = 0; i < clampSmoothing; i++)
			{
				float f = num4 * 3.14159274f * 0.5f;
				num4 = Mathf.Sin(f);
			}
			return Quaternion.Slerp(Quaternion.identity, rotation, num4 * num3);
		}

		// Token: 0x060009C0 RID: 2496 RVA: 0x00043FC4 File Offset: 0x000423C4
		public static float ClampAngle(float angle, float clampWeight, int clampSmoothing)
		{
			if (clampWeight >= 1f)
			{
				return 0f;
			}
			if (clampWeight <= 0f)
			{
				return angle;
			}
			float num = 1f - Mathf.Abs(angle) / 180f;
			float num2 = Mathf.Clamp(1f - (clampWeight - num) / (1f - num), 0f, 1f);
			float num3 = Mathf.Clamp(num / clampWeight, 0f, 1f);
			for (int i = 0; i < clampSmoothing; i++)
			{
				float f = num3 * 3.14159274f * 0.5f;
				num3 = Mathf.Sin(f);
			}
			return Mathf.Lerp(0f, angle, num3 * num2);
		}

		// Token: 0x060009C1 RID: 2497 RVA: 0x00044070 File Offset: 0x00042470
		public static Quaternion MatchRotation(Quaternion targetRotation, Vector3 targetforwardAxis, Vector3 targetUpAxis, Vector3 forwardAxis, Vector3 upAxis)
		{
			Quaternion rotation = Quaternion.LookRotation(forwardAxis, upAxis);
			Quaternion rhs = Quaternion.LookRotation(targetforwardAxis, targetUpAxis);
			Quaternion lhs = targetRotation * rhs;
			return lhs * Quaternion.Inverse(rotation);
		}
	}
}
