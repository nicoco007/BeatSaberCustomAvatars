using System;
using UnityEngine;

namespace AvatarScriptPack
{
	// Token: 0x02000177 RID: 375
	public static class V3Tools
	{
		// Token: 0x060009D9 RID: 2521 RVA: 0x00044119 File Offset: 0x00042519
		public static Vector3 Lerp(Vector3 fromVector, Vector3 toVector, float weight)
		{
			if (weight <= 0f)
			{
				return fromVector;
			}
			if (weight >= 1f)
			{
				return toVector;
			}
			return Vector3.Lerp(fromVector, toVector, weight);
		}

		// Token: 0x060009DA RID: 2522 RVA: 0x0004413D File Offset: 0x0004253D
		public static Vector3 Slerp(Vector3 fromVector, Vector3 toVector, float weight)
		{
			if (weight <= 0f)
			{
				return fromVector;
			}
			if (weight >= 1f)
			{
				return toVector;
			}
			return Vector3.Slerp(fromVector, toVector, weight);
		}

		// Token: 0x060009DB RID: 2523 RVA: 0x00044161 File Offset: 0x00042561
		public static Vector3 ExtractVertical(Vector3 v, Vector3 verticalAxis, float weight)
		{
			if (weight == 0f)
			{
				return Vector3.zero;
			}
			return Vector3.Project(v, verticalAxis) * weight;
		}

		// Token: 0x060009DC RID: 2524 RVA: 0x00044184 File Offset: 0x00042584
		public static Vector3 ExtractHorizontal(Vector3 v, Vector3 normal, float weight)
		{
			if (weight == 0f)
			{
				return Vector3.zero;
			}
			Vector3 onNormal = v;
			Vector3.OrthoNormalize(ref normal, ref onNormal);
			return Vector3.Project(v, onNormal) * weight;
		}

		// Token: 0x060009DD RID: 2525 RVA: 0x000441BC File Offset: 0x000425BC
		public static Vector3 ClampDirection(Vector3 direction, Vector3 normalDirection, float clampWeight, int clampSmoothing, out bool changed)
		{
			changed = false;
			if (clampWeight <= 0f)
			{
				return direction;
			}
			if (clampWeight >= 1f)
			{
				changed = true;
				return normalDirection;
			}
			float num = Vector3.Angle(normalDirection, direction);
			float num2 = 1f - num / 180f;
			if (num2 > clampWeight)
			{
				return direction;
			}
			changed = true;
			float num3 = (clampWeight <= 0f) ? 1f : Mathf.Clamp(1f - (clampWeight - num2) / (1f - num2), 0f, 1f);
			float num4 = (clampWeight <= 0f) ? 1f : Mathf.Clamp(num2 / clampWeight, 0f, 1f);
			for (int i = 0; i < clampSmoothing; i++)
			{
				float f = num4 * 3.14159274f * 0.5f;
				num4 = Mathf.Sin(f);
			}
			return Vector3.Slerp(normalDirection, direction, num4 * num3);
		}

		// Token: 0x060009DE RID: 2526 RVA: 0x000442A4 File Offset: 0x000426A4
		public static Vector3 ClampDirection(Vector3 direction, Vector3 normalDirection, float clampWeight, int clampSmoothing, out float clampValue)
		{
			clampValue = 1f;
			if (clampWeight <= 0f)
			{
				return direction;
			}
			if (clampWeight >= 1f)
			{
				return normalDirection;
			}
			float num = Vector3.Angle(normalDirection, direction);
			float num2 = 1f - num / 180f;
			if (num2 > clampWeight)
			{
				clampValue = 0f;
				return direction;
			}
			float num3 = (clampWeight <= 0f) ? 1f : Mathf.Clamp(1f - (clampWeight - num2) / (1f - num2), 0f, 1f);
			float num4 = (clampWeight <= 0f) ? 1f : Mathf.Clamp(num2 / clampWeight, 0f, 1f);
			for (int i = 0; i < clampSmoothing; i++)
			{
				float f = num4 * 3.14159274f * 0.5f;
				num4 = Mathf.Sin(f);
			}
			float num5 = num4 * num3;
			clampValue = 1f - num5;
			return Vector3.Slerp(normalDirection, direction, num5);
		}

		// Token: 0x060009DF RID: 2527 RVA: 0x000443A0 File Offset: 0x000427A0
		public static Vector3 LineToPlane(Vector3 origin, Vector3 direction, Vector3 planeNormal, Vector3 planePoint)
		{
			float num = Vector3.Dot(planePoint - origin, planeNormal);
			float num2 = Vector3.Dot(direction, planeNormal);
			if (num2 == 0f)
			{
				return Vector3.zero;
			}
			float d = num / num2;
			return origin + direction.normalized * d;
		}

		// Token: 0x060009E0 RID: 2528 RVA: 0x000443EC File Offset: 0x000427EC
		public static Vector3 PointToPlane(Vector3 point, Vector3 planePosition, Vector3 planeNormal)
		{
			if (planeNormal == Vector3.up)
			{
				return new Vector3(point.x, planePosition.y, point.z);
			}
			Vector3 onNormal = point - planePosition;
			Vector3 vector = planeNormal;
			Vector3.OrthoNormalize(ref vector, ref onNormal);
			return planePosition + Vector3.Project(point - planePosition, onNormal);
		}
	}
}
