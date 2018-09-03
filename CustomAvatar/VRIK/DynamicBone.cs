using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000003 RID: 3
[AddComponentMenu("Dynamic Bone/Dynamic Bone")]
public class DynamicBone : MonoBehaviour
{
	// Token: 0x06000002 RID: 2 RVA: 0x0000206F File Offset: 0x0000026F
	private void Start()
	{
		this.SetupParticles();
	}

	// Token: 0x06000003 RID: 3 RVA: 0x0000207C File Offset: 0x0000027C
	private void Update()
	{
		bool flag = this.m_Weight > 0f && (!this.m_DistantDisable || !this.m_DistantDisabled);
		if (flag)
		{
			this.InitTransforms();
		}
	}

	// Token: 0x06000004 RID: 4 RVA: 0x000020BC File Offset: 0x000002BC
	private void LateUpdate()
	{
		bool distantDisable = this.m_DistantDisable;
		if (distantDisable)
		{
			this.CheckDistance();
		}
		bool flag = this.m_Weight > 0f && (!this.m_DistantDisable || !this.m_DistantDisabled);
		if (flag)
		{
			this.UpdateDynamicBones(Time.deltaTime);
		}
	}

	// Token: 0x06000005 RID: 5 RVA: 0x00002110 File Offset: 0x00000310
	private void CheckDistance()
	{
		Transform transform = this.m_ReferenceObject;
		bool flag = transform == null && Camera.main != null;
		if (flag)
		{
			transform = Camera.main.transform;
		}
		bool flag2 = transform != null;
		if (flag2)
		{
			float sqrMagnitude = (transform.position - base.transform.position).sqrMagnitude;
			bool flag3 = sqrMagnitude > this.m_DistanceToObject * this.m_DistanceToObject;
			bool flag4 = flag3 != this.m_DistantDisabled;
			if (flag4)
			{
				bool flag5 = !flag3;
				if (flag5)
				{
					this.ResetParticlesPosition();
				}
				this.m_DistantDisabled = flag3;
			}
		}
	}

	// Token: 0x06000006 RID: 6 RVA: 0x000021B9 File Offset: 0x000003B9
	private void OnEnable()
	{
		this.ResetParticlesPosition();
	}

	// Token: 0x06000007 RID: 7 RVA: 0x000021C3 File Offset: 0x000003C3
	private void OnDisable()
	{
		this.InitTransforms();
	}

	// Token: 0x06000008 RID: 8 RVA: 0x000021D0 File Offset: 0x000003D0
	private void OnValidate()
	{
		this.m_UpdateRate = Mathf.Max(this.m_UpdateRate, 0f);
		this.m_Damping = Mathf.Clamp01(this.m_Damping);
		this.m_Elasticity = Mathf.Clamp01(this.m_Elasticity);
		this.m_Stiffness = Mathf.Clamp01(this.m_Stiffness);
		this.m_Inert = Mathf.Clamp01(this.m_Inert);
		this.m_Radius = Mathf.Max(this.m_Radius, 0f);
		bool flag = Application.isEditor && Application.isPlaying;
		if (flag)
		{
			this.InitTransforms();
			this.SetupParticles();
		}
	}

	// Token: 0x06000009 RID: 9 RVA: 0x00002274 File Offset: 0x00000474
	private void OnDrawGizmosSelected()
	{
		bool flag = !base.enabled || this.m_Root == null;
		if (!flag)
		{
			bool flag2 = Application.isEditor && !Application.isPlaying && base.transform.hasChanged;
			if (flag2)
			{
				this.InitTransforms();
				this.SetupParticles();
			}
			Gizmos.color = Color.white;
			for (int i = 0; i < this.m_Particles.Count; i++)
			{
				DynamicBone.Particle particle = this.m_Particles[i];
				bool flag3 = particle.m_ParentIndex >= 0;
				if (flag3)
				{
					DynamicBone.Particle particle2 = this.m_Particles[particle.m_ParentIndex];
					Gizmos.DrawLine(particle.m_Position, particle2.m_Position);
				}
				bool flag4 = particle.m_Radius > 0f;
				if (flag4)
				{
					Gizmos.DrawWireSphere(particle.m_Position, particle.m_Radius * this.m_ObjectScale);
				}
			}
		}
	}

	// Token: 0x0600000A RID: 10 RVA: 0x00002374 File Offset: 0x00000574
	public void SetWeight(float w)
	{
		bool flag = this.m_Weight != w;
		if (flag)
		{
			bool flag2 = w == 0f;
			if (flag2)
			{
				this.InitTransforms();
			}
			else
			{
				bool flag3 = this.m_Weight == 0f;
				if (flag3)
				{
					this.ResetParticlesPosition();
				}
			}
			this.m_Weight = w;
		}
	}

	// Token: 0x0600000B RID: 11 RVA: 0x000023C8 File Offset: 0x000005C8
	public float GetWeight()
	{
		return this.m_Weight;
	}

	// Token: 0x0600000C RID: 12 RVA: 0x000023E0 File Offset: 0x000005E0
	private void UpdateDynamicBones(float t)
	{
		bool flag = this.m_Root == null;
		if (!flag)
		{
			this.m_ObjectScale = Mathf.Abs(base.transform.lossyScale.x);
			this.m_ObjectMove = base.transform.position - this.m_ObjectPrevPosition;
			this.m_ObjectPrevPosition = base.transform.position;
			int num = 1;
			bool flag2 = this.m_UpdateRate > 0f;
			if (flag2)
			{
				float num2 = 1f / this.m_UpdateRate;
				this.m_Time += t;
				num = 0;
				while (this.m_Time >= num2)
				{
					this.m_Time -= num2;
					bool flag3 = ++num >= 3;
					if (flag3)
					{
						this.m_Time = 0f;
						break;
					}
				}
			}
			bool flag4 = num > 0;
			if (flag4)
			{
				for (int i = 0; i < num; i++)
				{
					this.UpdateParticles1();
					this.UpdateParticles2();
					this.m_ObjectMove = Vector3.zero;
				}
			}
			else
			{
				this.SkipUpdateParticles();
			}
			this.ApplyParticlesToTransforms();
		}
	}

	// Token: 0x0600000D RID: 13 RVA: 0x00002510 File Offset: 0x00000710
	private void SetupParticles()
	{
		this.m_Particles.Clear();
		bool flag = this.m_Root == null;
		if (!flag)
		{
			this.m_LocalGravity = this.m_Root.InverseTransformDirection(this.m_Gravity);
			this.m_ObjectScale = base.transform.lossyScale.x;
			this.m_ObjectPrevPosition = base.transform.position;
			this.m_ObjectMove = Vector3.zero;
			this.m_BoneTotalLength = 0f;
			this.AppendParticles(this.m_Root, -1, 0f);
			for (int i = 0; i < this.m_Particles.Count; i++)
			{
				DynamicBone.Particle particle = this.m_Particles[i];
				particle.m_Damping = this.m_Damping;
				particle.m_Elasticity = this.m_Elasticity;
				particle.m_Stiffness = this.m_Stiffness;
				particle.m_Inert = this.m_Inert;
				particle.m_Radius = this.m_Radius;
				bool flag2 = this.m_BoneTotalLength > 0f;
				if (flag2)
				{
					float num = particle.m_BoneLength / this.m_BoneTotalLength;
					bool flag3 = this.m_DampingDistrib != null && this.m_DampingDistrib.keys.Length != 0;
					if (flag3)
					{
						particle.m_Damping *= this.m_DampingDistrib.Evaluate(num);
					}
					bool flag4 = this.m_ElasticityDistrib != null && this.m_ElasticityDistrib.keys.Length != 0;
					if (flag4)
					{
						particle.m_Elasticity *= this.m_ElasticityDistrib.Evaluate(num);
					}
					bool flag5 = this.m_StiffnessDistrib != null && this.m_StiffnessDistrib.keys.Length != 0;
					if (flag5)
					{
						particle.m_Stiffness *= this.m_StiffnessDistrib.Evaluate(num);
					}
					bool flag6 = this.m_InertDistrib != null && this.m_InertDistrib.keys.Length != 0;
					if (flag6)
					{
						particle.m_Inert *= this.m_InertDistrib.Evaluate(num);
					}
					bool flag7 = this.m_RadiusDistrib != null && this.m_RadiusDistrib.keys.Length != 0;
					if (flag7)
					{
						particle.m_Radius *= this.m_RadiusDistrib.Evaluate(num);
					}
				}
				particle.m_Damping = Mathf.Clamp01(particle.m_Damping);
				particle.m_Elasticity = Mathf.Clamp01(particle.m_Elasticity);
				particle.m_Stiffness = Mathf.Clamp01(particle.m_Stiffness);
				particle.m_Inert = Mathf.Clamp01(particle.m_Inert);
				particle.m_Radius = Mathf.Max(particle.m_Radius, 0f);
			}
		}
	}

	// Token: 0x0600000E RID: 14 RVA: 0x000027B4 File Offset: 0x000009B4
	private void AppendParticles(Transform b, int parentIndex, float boneLength)
	{
		DynamicBone.Particle particle = new DynamicBone.Particle();
		particle.m_Transform = b;
		particle.m_ParentIndex = parentIndex;
		bool flag = b != null;
		if (flag)
		{
			particle.m_Position = (particle.m_PrevPosition = b.position);
			particle.m_InitLocalPosition = b.localPosition;
			particle.m_InitLocalRotation = b.localRotation;
		}
		else
		{
			Transform transform = this.m_Particles[parentIndex].m_Transform;
			bool flag2 = this.m_EndLength > 0f;
			if (flag2)
			{
				Transform parent = transform.parent;
				bool flag3 = parent != null;
				if (flag3)
				{
					particle.m_EndOffset = transform.InverseTransformPoint(transform.position * 2f - parent.position) * this.m_EndLength;
				}
				else
				{
					particle.m_EndOffset = new Vector3(this.m_EndLength, 0f, 0f);
				}
			}
			else
			{
				particle.m_EndOffset = transform.InverseTransformPoint(base.transform.TransformDirection(this.m_EndOffset) + transform.position);
			}
			particle.m_Position = (particle.m_PrevPosition = transform.TransformPoint(particle.m_EndOffset));
		}
		bool flag4 = parentIndex >= 0;
		if (flag4)
		{
			boneLength += (this.m_Particles[parentIndex].m_Transform.position - particle.m_Position).magnitude;
			particle.m_BoneLength = boneLength;
			this.m_BoneTotalLength = Mathf.Max(this.m_BoneTotalLength, boneLength);
		}
		int count = this.m_Particles.Count;
		this.m_Particles.Add(particle);
		bool flag5 = b != null;
		if (flag5)
		{
			for (int i = 0; i < b.childCount; i++)
			{
				bool flag6 = false;
				bool flag7 = this.m_Exclusions != null;
				if (flag7)
				{
					for (int j = 0; j < this.m_Exclusions.Count; j++)
					{
						Transform transform2 = this.m_Exclusions[j];
						bool flag8 = transform2 == b.GetChild(i);
						if (flag8)
						{
							flag6 = true;
							break;
						}
					}
				}
				bool flag9 = !flag6;
				if (flag9)
				{
					this.AppendParticles(b.GetChild(i), count, boneLength);
				}
			}
			bool flag10 = b.childCount == 0 && (this.m_EndLength > 0f || this.m_EndOffset != Vector3.zero);
			if (flag10)
			{
				this.AppendParticles(null, count, boneLength);
			}
		}
	}

	// Token: 0x0600000F RID: 15 RVA: 0x00002A4C File Offset: 0x00000C4C
	private void InitTransforms()
	{
		for (int i = 0; i < this.m_Particles.Count; i++)
		{
			DynamicBone.Particle particle = this.m_Particles[i];
			bool flag = particle.m_Transform != null;
			if (flag)
			{
				particle.m_Transform.localPosition = particle.m_InitLocalPosition;
				particle.m_Transform.localRotation = particle.m_InitLocalRotation;
			}
		}
	}

	// Token: 0x06000010 RID: 16 RVA: 0x00002ABC File Offset: 0x00000CBC
	private void ResetParticlesPosition()
	{
		for (int i = 0; i < this.m_Particles.Count; i++)
		{
			DynamicBone.Particle particle = this.m_Particles[i];
			bool flag = particle.m_Transform != null;
			if (flag)
			{
				particle.m_Position = (particle.m_PrevPosition = particle.m_Transform.position);
			}
			else
			{
				Transform transform = this.m_Particles[particle.m_ParentIndex].m_Transform;
				particle.m_Position = (particle.m_PrevPosition = transform.TransformPoint(particle.m_EndOffset));
			}
		}
		this.m_ObjectPrevPosition = base.transform.position;
	}

	// Token: 0x06000011 RID: 17 RVA: 0x00002B70 File Offset: 0x00000D70
	private void UpdateParticles1()
	{
		Vector3 vector = this.m_Gravity;
		Vector3 normalized = this.m_Gravity.normalized;
		Vector3 vector2 = this.m_Root.TransformDirection(this.m_LocalGravity);
		Vector3 vector3 = normalized * Mathf.Max(Vector3.Dot(vector2, normalized), 0f);
		vector -= vector3;
		vector = (vector + this.m_Force) * this.m_ObjectScale;
		for (int i = 0; i < this.m_Particles.Count; i++)
		{
			DynamicBone.Particle particle = this.m_Particles[i];
			bool flag = particle.m_ParentIndex >= 0;
			if (flag)
			{
				Vector3 vector4 = particle.m_Position - particle.m_PrevPosition;
				Vector3 vector5 = this.m_ObjectMove * particle.m_Inert;
				particle.m_PrevPosition = particle.m_Position + vector5;
				particle.m_Position += vector4 * (1f - particle.m_Damping) + vector + vector5;
			}
			else
			{
				particle.m_PrevPosition = particle.m_Position;
				particle.m_Position = particle.m_Transform.position;
			}
		}
	}

	// Token: 0x06000012 RID: 18 RVA: 0x00002CC0 File Offset: 0x00000EC0
	private void UpdateParticles2()
	{
		Plane plane = default(Plane);
		for (int i = 1; i < this.m_Particles.Count; i++)
		{
			DynamicBone.Particle particle = this.m_Particles[i];
			DynamicBone.Particle particle2 = this.m_Particles[particle.m_ParentIndex];
			bool flag = particle.m_Transform != null;
			float magnitude;
			if (flag)
			{
				magnitude = (particle2.m_Transform.position - particle.m_Transform.position).magnitude;
			}
			else
			{
				magnitude = particle2.m_Transform.localToWorldMatrix.MultiplyVector(particle.m_EndOffset).magnitude;
			}
			float num = Mathf.Lerp(1f, particle.m_Stiffness, this.m_Weight);
			bool flag2 = num > 0f || particle.m_Elasticity > 0f;
			if (flag2)
			{
				Matrix4x4 localToWorldMatrix = particle2.m_Transform.localToWorldMatrix;
				localToWorldMatrix.SetColumn(3, particle2.m_Position);
				bool flag3 = particle.m_Transform != null;
				Vector3 vector;
				if (flag3)
				{
					vector = localToWorldMatrix.MultiplyPoint3x4(particle.m_Transform.localPosition);
				}
				else
				{
					vector = localToWorldMatrix.MultiplyPoint3x4(particle.m_EndOffset);
				}
				Vector3 vector2 = vector - particle.m_Position;
				particle.m_Position += vector2 * particle.m_Elasticity;
				bool flag4 = num > 0f;
				if (flag4)
				{
					vector2 = vector - particle.m_Position;
					float magnitude2 = vector2.magnitude;
					float num2 = magnitude * (1f - num) * 2f;
					bool flag5 = magnitude2 > num2;
					if (flag5)
					{
						particle.m_Position += vector2 * ((magnitude2 - num2) / magnitude2);
					}
				}
			}
			bool flag6 = this.m_Colliders != null;
			if (flag6)
			{
				float particleRadius = particle.m_Radius * this.m_ObjectScale;
				for (int j = 0; j < this.m_Colliders.Count; j++)
				{
					DynamicBoneCollider dynamicBoneCollider = this.m_Colliders[j];
					bool flag7 = dynamicBoneCollider != null && dynamicBoneCollider.enabled;
					if (flag7)
					{
						dynamicBoneCollider.Collide(ref particle.m_Position, particleRadius);
					}
				}
			}
			bool flag8 = this.m_FreezeAxis > DynamicBone.FreezeAxis.None;
			if (flag8)
			{
				switch (this.m_FreezeAxis)
				{
				case DynamicBone.FreezeAxis.X:
					plane.SetNormalAndPosition(particle2.m_Transform.right, particle2.m_Position);
					break;
				case DynamicBone.FreezeAxis.Y:
					plane.SetNormalAndPosition(particle2.m_Transform.up, particle2.m_Position);
					break;
				case DynamicBone.FreezeAxis.Z:
					plane.SetNormalAndPosition(particle2.m_Transform.forward, particle2.m_Position);
					break;
				}
				particle.m_Position -= plane.normal * plane.GetDistanceToPoint(particle.m_Position);
			}
			Vector3 vector3 = particle2.m_Position - particle.m_Position;
			float magnitude3 = vector3.magnitude;
			bool flag9 = magnitude3 > 0f;
			if (flag9)
			{
				particle.m_Position += vector3 * ((magnitude3 - magnitude) / magnitude3);
			}
		}
	}

	// Token: 0x06000013 RID: 19 RVA: 0x00003018 File Offset: 0x00001218
	private void SkipUpdateParticles()
	{
		for (int i = 0; i < this.m_Particles.Count; i++)
		{
			DynamicBone.Particle particle = this.m_Particles[i];
			bool flag = particle.m_ParentIndex >= 0;
			if (flag)
			{
			}
			else
			{
				particle.m_PrevPosition = particle.m_Position;
				particle.m_Position = particle.m_Transform.position;
			}
		}
	}

	// Token: 0x06000014 RID: 20 RVA: 0x00003254 File Offset: 0x00001454
	private void ApplyParticlesToTransforms()
	{
		for (int i = 1; i < this.m_Particles.Count; i++)
		{
			DynamicBone.Particle particle = this.m_Particles[i];
			DynamicBone.Particle particle2 = this.m_Particles[particle.m_ParentIndex];
			bool flag = particle2.m_Transform.childCount <= 1;
			if (flag)
			{
				bool flag2 = particle.m_Transform != null;
				Vector3 vector;
				if (flag2)
				{
					vector = particle.m_Transform.localPosition;
				}
				else
				{
					vector = particle.m_EndOffset;
				}
				Quaternion quaternion = Quaternion.FromToRotation(particle2.m_Transform.TransformDirection(vector), particle.m_Position - particle2.m_Position);
				particle2.m_Transform.rotation = quaternion * particle2.m_Transform.rotation;
			}
			bool flag3 = particle.m_Transform != null;
			if (flag3)
			{
				particle.m_Transform.position = particle.m_Position;
			}
		}
	}

	// Token: 0x04000003 RID: 3
	public Transform m_Root = null;

	// Token: 0x04000004 RID: 4
	public float m_UpdateRate = 60f;

	// Token: 0x04000005 RID: 5
	[Range(0f, 1f)]
	public float m_Damping = 0.1f;

	// Token: 0x04000006 RID: 6
	public AnimationCurve m_DampingDistrib = null;

	// Token: 0x04000007 RID: 7
	[Range(0f, 1f)]
	public float m_Elasticity = 0.1f;

	// Token: 0x04000008 RID: 8
	public AnimationCurve m_ElasticityDistrib = null;

	// Token: 0x04000009 RID: 9
	[Range(0f, 1f)]
	public float m_Stiffness = 0.1f;

	// Token: 0x0400000A RID: 10
	public AnimationCurve m_StiffnessDistrib = null;

	// Token: 0x0400000B RID: 11
	[Range(0f, 1f)]
	public float m_Inert = 0f;

	// Token: 0x0400000C RID: 12
	public AnimationCurve m_InertDistrib = null;

	// Token: 0x0400000D RID: 13
	public float m_Radius = 0f;

	// Token: 0x0400000E RID: 14
	public AnimationCurve m_RadiusDistrib = null;

	// Token: 0x0400000F RID: 15
	public float m_EndLength = 0f;

	// Token: 0x04000010 RID: 16
	public Vector3 m_EndOffset = Vector3.zero;

	// Token: 0x04000011 RID: 17
	public Vector3 m_Gravity = Vector3.zero;

	// Token: 0x04000012 RID: 18
	public Vector3 m_Force = Vector3.zero;

	// Token: 0x04000013 RID: 19
	public List<DynamicBoneCollider> m_Colliders = null;

	// Token: 0x04000014 RID: 20
	public List<Transform> m_Exclusions = null;

	// Token: 0x04000015 RID: 21
	public DynamicBone.FreezeAxis m_FreezeAxis = DynamicBone.FreezeAxis.None;

	// Token: 0x04000016 RID: 22
	public bool m_DistantDisable = false;

	// Token: 0x04000017 RID: 23
	public Transform m_ReferenceObject = null;

	// Token: 0x04000018 RID: 24
	public float m_DistanceToObject = 20f;

	// Token: 0x04000019 RID: 25
	private Vector3 m_LocalGravity = Vector3.zero;

	// Token: 0x0400001A RID: 26
	private Vector3 m_ObjectMove = Vector3.zero;

	// Token: 0x0400001B RID: 27
	private Vector3 m_ObjectPrevPosition = Vector3.zero;

	// Token: 0x0400001C RID: 28
	private float m_BoneTotalLength = 0f;

	// Token: 0x0400001D RID: 29
	private float m_ObjectScale = 1f;

	// Token: 0x0400001E RID: 30
	private float m_Time = 0f;

	// Token: 0x0400001F RID: 31
	private float m_Weight = 1f;

	// Token: 0x04000020 RID: 32
	private bool m_DistantDisabled = false;

	// Token: 0x04000021 RID: 33
	private List<DynamicBone.Particle> m_Particles = new List<DynamicBone.Particle>();

	// Token: 0x02000011 RID: 17
	public enum FreezeAxis
	{
		// Token: 0x0400006C RID: 108
		None,
		// Token: 0x0400006D RID: 109
		X,
		// Token: 0x0400006E RID: 110
		Y,
		// Token: 0x0400006F RID: 111
		Z
	}

	// Token: 0x02000012 RID: 18
	private class Particle
	{
		// Token: 0x04000070 RID: 112
		public Transform m_Transform = null;

		// Token: 0x04000071 RID: 113
		public int m_ParentIndex = -1;

		// Token: 0x04000072 RID: 114
		public float m_Damping = 0f;

		// Token: 0x04000073 RID: 115
		public float m_Elasticity = 0f;

		// Token: 0x04000074 RID: 116
		public float m_Stiffness = 0f;

		// Token: 0x04000075 RID: 117
		public float m_Inert = 0f;

		// Token: 0x04000076 RID: 118
		public float m_Radius = 0f;

		// Token: 0x04000077 RID: 119
		public float m_BoneLength = 0f;

		// Token: 0x04000078 RID: 120
		public Vector3 m_Position = Vector3.zero;

		// Token: 0x04000079 RID: 121
		public Vector3 m_PrevPosition = Vector3.zero;

		// Token: 0x0400007A RID: 122
		public Vector3 m_EndOffset = Vector3.zero;

		// Token: 0x0400007B RID: 123
		public Vector3 m_InitLocalPosition = Vector3.zero;

		// Token: 0x0400007C RID: 124
		public Quaternion m_InitLocalRotation = Quaternion.identity;
	}
}
