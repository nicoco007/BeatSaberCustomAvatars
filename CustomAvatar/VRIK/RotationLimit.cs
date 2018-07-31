using System;
using UnityEngine;

namespace AvatarScriptPack
{
	// Token: 0x02000088 RID: 136
	public abstract class RotationLimit : MonoBehaviour
	{
		// Token: 0x060004B3 RID: 1203 RVA: 0x0002200D File Offset: 0x0002040D
		public void SetDefaultLocalRotation()
		{
			this.defaultLocalRotation = base.transform.localRotation;
			this.defaultLocalRotationSet = true;
		}

		// Token: 0x060004B4 RID: 1204 RVA: 0x00022028 File Offset: 0x00020428
		public Quaternion GetLimitedLocalRotation(Quaternion localRotation, out bool changed)
		{
			if (!this.initiated)
			{
				this.Awake();
			}
			Quaternion quaternion = Quaternion.Inverse(this.defaultLocalRotation) * localRotation;
			Quaternion quaternion2 = this.LimitRotation(quaternion);
			changed = (quaternion2 != quaternion);
			if (!changed)
			{
				return localRotation;
			}
			return this.defaultLocalRotation * quaternion2;
		}

		// Token: 0x060004B5 RID: 1205 RVA: 0x00022080 File Offset: 0x00020480
		public bool Apply()
		{
			bool result = false;
			base.transform.localRotation = this.GetLimitedLocalRotation(base.transform.localRotation, out result);
			return result;
		}

		// Token: 0x060004B6 RID: 1206 RVA: 0x000220AE File Offset: 0x000204AE
		public void Disable()
		{
			if (this.initiated)
			{
				base.enabled = false;
				return;
			}
			this.Awake();
			base.enabled = false;
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x060004B7 RID: 1207 RVA: 0x000220D0 File Offset: 0x000204D0
		public Vector3 secondaryAxis
		{
			get
			{
				return new Vector3(this.axis.y, this.axis.z, this.axis.x);
			}
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x060004B8 RID: 1208 RVA: 0x000220F8 File Offset: 0x000204F8
		public Vector3 crossAxis
		{
			get
			{
				return Vector3.Cross(this.axis, this.secondaryAxis);
			}
		}

		// Token: 0x060004B9 RID: 1209
		protected abstract Quaternion LimitRotation(Quaternion rotation);

		// Token: 0x060004BA RID: 1210 RVA: 0x0002210B File Offset: 0x0002050B
		private void Awake()
		{
			if (!this.defaultLocalRotationSet)
			{
				this.SetDefaultLocalRotation();
			}
			if (this.axis == Vector3.zero)
			{
				Debug.LogError("Axis is Vector3.zero.");
			}
			this.initiated = true;
		}

		// Token: 0x060004BB RID: 1211 RVA: 0x00022144 File Offset: 0x00020544
		private void LateUpdate()
		{
			this.Apply();
		}

		// Token: 0x060004BC RID: 1212 RVA: 0x0002214D File Offset: 0x0002054D
		public void LogWarning(string message)
		{
			Warning.Log(message, base.transform, false);
		}

		// Token: 0x060004BD RID: 1213 RVA: 0x0002215C File Offset: 0x0002055C
		protected static Quaternion Limit1DOF(Quaternion rotation, Vector3 axis)
		{
			return Quaternion.FromToRotation(rotation * axis, axis) * rotation;
		}

		// Token: 0x060004BE RID: 1214 RVA: 0x00022174 File Offset: 0x00020574
		protected static Quaternion LimitTwist(Quaternion rotation, Vector3 axis, Vector3 orthoAxis, float twistLimit)
		{
			twistLimit = Mathf.Clamp(twistLimit, 0f, 180f);
			if (twistLimit >= 180f)
			{
				return rotation;
			}
			Vector3 vector = rotation * axis;
			Vector3 toDirection = orthoAxis;
			Vector3.OrthoNormalize(ref vector, ref toDirection);
			Vector3 fromDirection = rotation * orthoAxis;
			Vector3.OrthoNormalize(ref vector, ref fromDirection);
			Quaternion quaternion = Quaternion.FromToRotation(fromDirection, toDirection) * rotation;
			if (twistLimit <= 0f)
			{
				return quaternion;
			}
			return Quaternion.RotateTowards(quaternion, rotation, twistLimit);
		}

		// Token: 0x060004BF RID: 1215 RVA: 0x000221E7 File Offset: 0x000205E7
		protected static float GetOrthogonalAngle(Vector3 v1, Vector3 v2, Vector3 normal)
		{
			Vector3.OrthoNormalize(ref normal, ref v1);
			Vector3.OrthoNormalize(ref normal, ref v2);
			return Vector3.Angle(v1, v2);
		}

		// Token: 0x04000419 RID: 1049
		public Vector3 axis = Vector3.forward;

		// Token: 0x0400041A RID: 1050
		[HideInInspector]
		public Quaternion defaultLocalRotation;

		// Token: 0x0400041B RID: 1051
		private bool initiated;

		// Token: 0x0400041C RID: 1052
		private bool applicationQuit;

		// Token: 0x0400041D RID: 1053
		private bool defaultLocalRotationSet;
	}
}
