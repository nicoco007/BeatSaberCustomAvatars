using System;
using UnityEngine;

// Token: 0x02000004 RID: 4
[AddComponentMenu("Dynamic Bone/Dynamic Bone Collider")]
public class DynamicBoneCollider : MonoBehaviour
{
	// Token: 0x06000016 RID: 22 RVA: 0x00003485 File Offset: 0x00001685
	private void OnValidate()
	{
		this.m_Radius = Mathf.Max(this.m_Radius, 0f);
		this.m_Height = Mathf.Max(this.m_Height, 0f);
	}

	// Token: 0x06000017 RID: 23 RVA: 0x000034B4 File Offset: 0x000016B4
	public void Collide(ref Vector3 particlePosition, float particleRadius)
	{
		float num = this.m_Radius * Mathf.Abs(base.transform.lossyScale.x);
		float num2 = this.m_Height * 0.5f - this.m_Radius;
		bool flag = num2 <= 0f;
		if (flag)
		{
			bool flag2 = this.m_Bound == DynamicBoneCollider.Bound.Outside;
			if (flag2)
			{
				DynamicBoneCollider.OutsideSphere(ref particlePosition, particleRadius, base.transform.TransformPoint(this.m_Center), num);
			}
			else
			{
				DynamicBoneCollider.InsideSphere(ref particlePosition, particleRadius, base.transform.TransformPoint(this.m_Center), num);
			}
		}
		else
		{
			Vector3 center = this.m_Center;
			Vector3 center2 = this.m_Center;
			switch (this.m_Direction)
			{
			case DynamicBoneCollider.Direction.X:
				center.x -= num2;
				center2.x += num2;
				break;
			case DynamicBoneCollider.Direction.Y:
				center.y -= num2;
				center2.y += num2;
				break;
			case DynamicBoneCollider.Direction.Z:
				center.z -= num2;
				center2.z += num2;
				break;
			}
			bool flag3 = this.m_Bound == DynamicBoneCollider.Bound.Outside;
			if (flag3)
			{
				DynamicBoneCollider.OutsideCapsule(ref particlePosition, particleRadius, base.transform.TransformPoint(center), base.transform.TransformPoint(center2), num);
			}
			else
			{
				DynamicBoneCollider.InsideCapsule(ref particlePosition, particleRadius, base.transform.TransformPoint(center), base.transform.TransformPoint(center2), num);
			}
		}
	}

	// Token: 0x06000018 RID: 24 RVA: 0x00003620 File Offset: 0x00001820
	private static void OutsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
	{
		float num = sphereRadius + particleRadius;
		float num2 = num * num;
		Vector3 vector = particlePosition - sphereCenter;
		float sqrMagnitude = vector.sqrMagnitude;
		bool flag = sqrMagnitude > 0f && sqrMagnitude < num2;
		if (flag)
		{
			float num3 = Mathf.Sqrt(sqrMagnitude);
			particlePosition = sphereCenter + vector * (num / num3);
		}
	}

	// Token: 0x06000019 RID: 25 RVA: 0x00003680 File Offset: 0x00001880
	private static void InsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
	{
		float num = sphereRadius + particleRadius;
		float num2 = num * num;
		Vector3 vector = particlePosition - sphereCenter;
		float sqrMagnitude = vector.sqrMagnitude;
		bool flag = sqrMagnitude > num2;
		if (flag)
		{
			float num3 = Mathf.Sqrt(sqrMagnitude);
			particlePosition = sphereCenter + vector * (num / num3);
		}
	}

	// Token: 0x0600001A RID: 26 RVA: 0x000036D8 File Offset: 0x000018D8
	private static void OutsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius)
	{
		float num = capsuleRadius + particleRadius;
		float num2 = num * num;
		Vector3 vector = capsuleP1 - capsuleP0;
		Vector3 vector2 = particlePosition - capsuleP0;
		float num3 = Vector3.Dot(vector2, vector);
		bool flag = num3 <= 0f;
		if (flag)
		{
			float sqrMagnitude = vector2.sqrMagnitude;
			bool flag2 = sqrMagnitude > 0f && sqrMagnitude < num2;
			if (flag2)
			{
				float num4 = Mathf.Sqrt(sqrMagnitude);
				particlePosition = capsuleP0 + vector2 * (num / num4);
			}
		}
		else
		{
			float sqrMagnitude2 = vector.sqrMagnitude;
			bool flag3 = num3 >= sqrMagnitude2;
			if (flag3)
			{
				vector2 = particlePosition - capsuleP1;
				float sqrMagnitude3 = vector2.sqrMagnitude;
				bool flag4 = sqrMagnitude3 > 0f && sqrMagnitude3 < num2;
				if (flag4)
				{
					float num5 = Mathf.Sqrt(sqrMagnitude3);
					particlePosition = capsuleP1 + vector2 * (num / num5);
				}
			}
			else
			{
				bool flag5 = sqrMagnitude2 > 0f;
				if (flag5)
				{
					num3 /= sqrMagnitude2;
					vector2 -= vector * num3;
					float sqrMagnitude4 = vector2.sqrMagnitude;
					bool flag6 = sqrMagnitude4 > 0f && sqrMagnitude4 < num2;
					if (flag6)
					{
						float num6 = Mathf.Sqrt(sqrMagnitude4);
						particlePosition += vector2 * ((num - num6) / num6);
					}
				}
			}
		}
	}

	// Token: 0x0600001B RID: 27 RVA: 0x00003844 File Offset: 0x00001A44
	private static void InsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius)
	{
		float num = capsuleRadius + particleRadius;
		float num2 = num * num;
		Vector3 vector = capsuleP1 - capsuleP0;
		Vector3 vector2 = particlePosition - capsuleP0;
		float num3 = Vector3.Dot(vector2, vector);
		bool flag = num3 <= 0f;
		if (flag)
		{
			float sqrMagnitude = vector2.sqrMagnitude;
			bool flag2 = sqrMagnitude > num2;
			if (flag2)
			{
				float num4 = Mathf.Sqrt(sqrMagnitude);
				particlePosition = capsuleP0 + vector2 * (num / num4);
			}
		}
		else
		{
			float sqrMagnitude2 = vector.sqrMagnitude;
			bool flag3 = num3 >= sqrMagnitude2;
			if (flag3)
			{
				vector2 = particlePosition - capsuleP1;
				float sqrMagnitude3 = vector2.sqrMagnitude;
				bool flag4 = sqrMagnitude3 > num2;
				if (flag4)
				{
					float num5 = Mathf.Sqrt(sqrMagnitude3);
					particlePosition = capsuleP1 + vector2 * (num / num5);
				}
			}
			else
			{
				bool flag5 = sqrMagnitude2 > 0f;
				if (flag5)
				{
					num3 /= sqrMagnitude2;
					vector2 -= vector * num3;
					float sqrMagnitude4 = vector2.sqrMagnitude;
					bool flag6 = sqrMagnitude4 > num2;
					if (flag6)
					{
						float num6 = Mathf.Sqrt(sqrMagnitude4);
						particlePosition += vector2 * ((num - num6) / num6);
					}
				}
			}
		}
	}

	// Token: 0x0600001C RID: 28 RVA: 0x0000398C File Offset: 0x00001B8C
	private void OnDrawGizmosSelected()
	{
		bool flag = !base.enabled;
		if (!flag)
		{
			bool flag2 = this.m_Bound == DynamicBoneCollider.Bound.Outside;
			if (flag2)
			{
				Gizmos.color = Color.yellow;
			}
			else
			{
				Gizmos.color = Color.magenta;
			}
			float num = this.m_Radius * Mathf.Abs(base.transform.lossyScale.x);
			float num2 = this.m_Height * 0.5f - this.m_Radius;
			bool flag3 = num2 <= 0f;
			if (flag3)
			{
				Gizmos.DrawWireSphere(base.transform.TransformPoint(this.m_Center), num);
			}
			else
			{
				Vector3 center = this.m_Center;
				Vector3 center2 = this.m_Center;
				switch (this.m_Direction)
				{
				case DynamicBoneCollider.Direction.X:
					center.x -= num2;
					center2.x += num2;
					break;
				case DynamicBoneCollider.Direction.Y:
					center.y -= num2;
					center2.y += num2;
					break;
				case DynamicBoneCollider.Direction.Z:
					center.z -= num2;
					center2.z += num2;
					break;
				}
				Gizmos.DrawWireSphere(base.transform.TransformPoint(center), num);
				Gizmos.DrawWireSphere(base.transform.TransformPoint(center2), num);
			}
		}
	}

	// Token: 0x04000022 RID: 34
	public Vector3 m_Center = Vector3.zero;

	// Token: 0x04000023 RID: 35
	public float m_Radius = 0.5f;

	// Token: 0x04000024 RID: 36
	public float m_Height = 0f;

	// Token: 0x04000025 RID: 37
	public DynamicBoneCollider.Direction m_Direction = DynamicBoneCollider.Direction.X;

	// Token: 0x04000026 RID: 38
	public DynamicBoneCollider.Bound m_Bound = DynamicBoneCollider.Bound.Outside;

	// Token: 0x02000013 RID: 19
	public enum Direction
	{
		// Token: 0x0400007E RID: 126
		X,
		// Token: 0x0400007F RID: 127
		Y,
		// Token: 0x04000080 RID: 128
		Z
	}

	// Token: 0x02000014 RID: 20
	public enum Bound
	{
		// Token: 0x04000082 RID: 130
		Outside,
		// Token: 0x04000083 RID: 131
		Inside
	}
}
