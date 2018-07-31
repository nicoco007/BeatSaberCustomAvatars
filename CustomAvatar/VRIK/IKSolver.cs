using System;
using UnityEngine;

namespace AvatarScriptPack
{
	// Token: 0x0200004B RID: 75
	[Serializable]
	public abstract class IKSolver
	{
		// Token: 0x06000245 RID: 581 RVA: 0x0000FDDC File Offset: 0x0000E1DC
		public bool IsValid()
		{
			string empty = string.Empty;
			return this.IsValid(ref empty);
		}

		// Token: 0x06000246 RID: 582
		public abstract bool IsValid(ref string message);

		// Token: 0x06000247 RID: 583 RVA: 0x0000FDF8 File Offset: 0x0000E1F8
		public void Initiate(Transform root)
		{
			if (this.OnPreInitiate != null)
			{
				this.OnPreInitiate();
			}
			if (root == null)
			{
				Debug.LogError("Initiating IKSolver with null root Transform.");
			}
			this.root = root;
			this.initiated = false;
			string empty = string.Empty;
			if (!this.IsValid(ref empty))
			{
				Warning.Log(empty, root, false);
				return;
			}
			this.OnInitiate();
			this.StoreDefaultLocalState();
			this.initiated = true;
			this.firstInitiation = false;
			if (this.OnPostInitiate != null)
			{
				this.OnPostInitiate();
			}
		}

		// Token: 0x06000248 RID: 584 RVA: 0x0000FE8C File Offset: 0x0000E28C
		public void Update()
		{
			if (this.OnPreUpdate != null)
			{
				this.OnPreUpdate();
			}
			if (this.firstInitiation)
			{
				this.Initiate(this.root);
			}
			if (!this.initiated)
			{
				return;
			}
			this.OnUpdate();
			if (this.OnPostUpdate != null)
			{
				this.OnPostUpdate();
			}
		}

		// Token: 0x06000249 RID: 585 RVA: 0x0000FEEE File Offset: 0x0000E2EE
		public virtual Vector3 GetIKPosition()
		{
			return this.IKPosition;
		}

		// Token: 0x0600024A RID: 586 RVA: 0x0000FEF6 File Offset: 0x0000E2F6
		public void SetIKPosition(Vector3 position)
		{
			this.IKPosition = position;
		}

		// Token: 0x0600024B RID: 587 RVA: 0x0000FEFF File Offset: 0x0000E2FF
		public float GetIKPositionWeight()
		{
			return this.IKPositionWeight;
		}

		// Token: 0x0600024C RID: 588 RVA: 0x0000FF07 File Offset: 0x0000E307
		public void SetIKPositionWeight(float weight)
		{
			this.IKPositionWeight = Mathf.Clamp(weight, 0f, 1f);
		}

		// Token: 0x0600024D RID: 589 RVA: 0x0000FF1F File Offset: 0x0000E31F
		public Transform GetRoot()
		{
			return this.root;
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x0600024E RID: 590 RVA: 0x0000FF27 File Offset: 0x0000E327
		// (set) Token: 0x0600024F RID: 591 RVA: 0x0000FF2F File Offset: 0x0000E32F
		public bool initiated { get; private set; }

		// Token: 0x06000250 RID: 592
		public abstract IKSolver.Point[] GetPoints();

		// Token: 0x06000251 RID: 593
		public abstract IKSolver.Point GetPoint(Transform transform);

		// Token: 0x06000252 RID: 594
		public abstract void FixTransforms();

		// Token: 0x06000253 RID: 595
		public abstract void StoreDefaultLocalState();

		// Token: 0x06000254 RID: 596
		protected abstract void OnInitiate();

		// Token: 0x06000255 RID: 597
		protected abstract void OnUpdate();

		// Token: 0x06000256 RID: 598 RVA: 0x0000FF38 File Offset: 0x0000E338
		protected void LogWarning(string message)
		{
			Warning.Log(message, this.root, true);
		}

		// Token: 0x06000257 RID: 599 RVA: 0x0000FF48 File Offset: 0x0000E348
		public static Transform ContainsDuplicateBone(IKSolver.Bone[] bones)
		{
			for (int i = 0; i < bones.Length; i++)
			{
				for (int j = 0; j < bones.Length; j++)
				{
					if (i != j && bones[i].transform == bones[j].transform)
					{
						return bones[i].transform;
					}
				}
			}
			return null;
		}

		// Token: 0x06000258 RID: 600 RVA: 0x0000FFA8 File Offset: 0x0000E3A8
		public static bool HierarchyIsValid(IKSolver.Bone[] bones)
		{
			for (int i = 1; i < bones.Length; i++)
			{
				if (!Hierarchy.IsAncestor(bones[i].transform, bones[i - 1].transform))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000259 RID: 601 RVA: 0x0000FFE8 File Offset: 0x0000E3E8
		protected static float PreSolveBones(ref IKSolver.Bone[] bones)
		{
			float num = 0f;
			for (int i = 0; i < bones.Length; i++)
			{
				bones[i].solverPosition = bones[i].transform.position;
				bones[i].solverRotation = bones[i].transform.rotation;
			}
			for (int j = 0; j < bones.Length; j++)
			{
				if (j < bones.Length - 1)
				{
					bones[j].sqrMag = (bones[j + 1].solverPosition - bones[j].solverPosition).sqrMagnitude;
					bones[j].length = Mathf.Sqrt(bones[j].sqrMag);
					num += bones[j].length;
					bones[j].axis = Quaternion.Inverse(bones[j].solverRotation) * (bones[j + 1].solverPosition - bones[j].solverPosition);
				}
				else
				{
					bones[j].sqrMag = 0f;
					bones[j].length = 0f;
				}
			}
			return num;
		}

		// Token: 0x040001F8 RID: 504
		[HideInInspector]
		public Vector3 IKPosition;

		// Token: 0x040001F9 RID: 505
		[Tooltip("The positional or the master weight of the solver.")]
		[Range(0f, 1f)]
		public float IKPositionWeight = 1f;

		// Token: 0x040001FB RID: 507
		public IKSolver.UpdateDelegate OnPreInitiate;

		// Token: 0x040001FC RID: 508
		public IKSolver.UpdateDelegate OnPostInitiate;

		// Token: 0x040001FD RID: 509
		public IKSolver.UpdateDelegate OnPreUpdate;

		// Token: 0x040001FE RID: 510
		public IKSolver.UpdateDelegate OnPostUpdate;

		// Token: 0x040001FF RID: 511
		protected bool firstInitiation = true;

		// Token: 0x04000200 RID: 512
		[SerializeField]
		[HideInInspector]
		protected Transform root;

		// Token: 0x0200004C RID: 76
		[Serializable]
		public class Point
		{
			// Token: 0x0600025B RID: 603 RVA: 0x00010121 File Offset: 0x0000E521
			public void StoreDefaultLocalState()
			{
				this.defaultLocalPosition = this.transform.localPosition;
				this.defaultLocalRotation = this.transform.localRotation;
			}

			// Token: 0x0600025C RID: 604 RVA: 0x00010148 File Offset: 0x0000E548
			public void FixTransform()
			{
				if (this.transform.localPosition != this.defaultLocalPosition)
				{
					this.transform.localPosition = this.defaultLocalPosition;
				}
				if (this.transform.localRotation != this.defaultLocalRotation)
				{
					this.transform.localRotation = this.defaultLocalRotation;
				}
			}

			// Token: 0x0600025D RID: 605 RVA: 0x000101AD File Offset: 0x0000E5AD
			public void UpdateSolverPosition()
			{
				this.solverPosition = this.transform.position;
			}

			// Token: 0x0600025E RID: 606 RVA: 0x000101C0 File Offset: 0x0000E5C0
			public void UpdateSolverLocalPosition()
			{
				this.solverPosition = this.transform.localPosition;
			}

			// Token: 0x0600025F RID: 607 RVA: 0x000101D3 File Offset: 0x0000E5D3
			public void UpdateSolverState()
			{
				this.solverPosition = this.transform.position;
				this.solverRotation = this.transform.rotation;
			}

			// Token: 0x06000260 RID: 608 RVA: 0x000101F7 File Offset: 0x0000E5F7
			public void UpdateSolverLocalState()
			{
				this.solverPosition = this.transform.localPosition;
				this.solverRotation = this.transform.localRotation;
			}

			// Token: 0x04000201 RID: 513
			public Transform transform;

			// Token: 0x04000202 RID: 514
			[Range(0f, 1f)]
			public float weight = 1f;

			// Token: 0x04000203 RID: 515
			public Vector3 solverPosition;

			// Token: 0x04000204 RID: 516
			public Quaternion solverRotation = Quaternion.identity;

			// Token: 0x04000205 RID: 517
			public Vector3 defaultLocalPosition;

			// Token: 0x04000206 RID: 518
			public Quaternion defaultLocalRotation;
		}

		// Token: 0x0200004D RID: 77
		[Serializable]
		public class Bone : IKSolver.Point
		{
			// Token: 0x06000261 RID: 609 RVA: 0x0001021B File Offset: 0x0000E61B
			public Bone()
			{
			}

			// Token: 0x06000262 RID: 610 RVA: 0x0001023A File Offset: 0x0000E63A
			public Bone(Transform transform)
			{
				this.transform = transform;
			}

			// Token: 0x06000263 RID: 611 RVA: 0x00010260 File Offset: 0x0000E660
			public Bone(Transform transform, float weight)
			{
				this.transform = transform;
				this.weight = weight;
			}

			// Token: 0x17000036 RID: 54
			// (get) Token: 0x06000264 RID: 612 RVA: 0x00010290 File Offset: 0x0000E690
			// (set) Token: 0x06000265 RID: 613 RVA: 0x000102E4 File Offset: 0x0000E6E4
			public RotationLimit rotationLimit
			{
				get
				{
					if (!this.isLimited)
					{
						return null;
					}
					if (this._rotationLimit == null)
					{
						this._rotationLimit = this.transform.GetComponent<RotationLimit>();
					}
					this.isLimited = (this._rotationLimit != null);
					return this._rotationLimit;
				}
				set
				{
					this._rotationLimit = value;
					this.isLimited = (value != null);
				}
			}

			// Token: 0x06000266 RID: 614 RVA: 0x000102FC File Offset: 0x0000E6FC
			public void Swing(Vector3 swingTarget, float weight = 1f)
			{
				if (weight <= 0f)
				{
					return;
				}
				Quaternion quaternion = Quaternion.FromToRotation(this.transform.rotation * this.axis, swingTarget - this.transform.position);
				if (weight >= 1f)
				{
					this.transform.rotation = quaternion * this.transform.rotation;
					return;
				}
				this.transform.rotation = Quaternion.Lerp(Quaternion.identity, quaternion, weight) * this.transform.rotation;
			}

			// Token: 0x06000267 RID: 615 RVA: 0x00010394 File Offset: 0x0000E794
			public static void SolverSwing(IKSolver.Bone[] bones, int index, Vector3 swingTarget, float weight = 1f)
			{
				if (weight <= 0f)
				{
					return;
				}
				Quaternion quaternion = Quaternion.FromToRotation(bones[index].solverRotation * bones[index].axis, swingTarget - bones[index].solverPosition);
				if (weight >= 1f)
				{
					for (int i = index; i < bones.Length; i++)
					{
						bones[i].solverRotation = quaternion * bones[i].solverRotation;
					}
					return;
				}
				for (int j = index; j < bones.Length; j++)
				{
					bones[j].solverRotation = Quaternion.Lerp(Quaternion.identity, quaternion, weight) * bones[j].solverRotation;
				}
			}

			// Token: 0x06000268 RID: 616 RVA: 0x00010444 File Offset: 0x0000E844
			public void Swing2D(Vector3 swingTarget, float weight = 1f)
			{
				if (weight <= 0f)
				{
					return;
				}
				Vector3 vector = this.transform.rotation * this.axis;
				Vector3 vector2 = swingTarget - this.transform.position;
				float current = Mathf.Atan2(vector.x, vector.y) * 57.29578f;
				float target = Mathf.Atan2(vector2.x, vector2.y) * 57.29578f;
				this.transform.rotation = Quaternion.AngleAxis(Mathf.DeltaAngle(current, target) * weight, Vector3.back) * this.transform.rotation;
			}

			// Token: 0x06000269 RID: 617 RVA: 0x000104E8 File Offset: 0x0000E8E8
			public void SetToSolverPosition()
			{
				this.transform.position = this.solverPosition;
			}

			// Token: 0x04000207 RID: 519
			public float length;

			// Token: 0x04000208 RID: 520
			public float sqrMag;

			// Token: 0x04000209 RID: 521
			public Vector3 axis = -Vector3.right;

			// Token: 0x0400020A RID: 522
			private RotationLimit _rotationLimit;

			// Token: 0x0400020B RID: 523
			private bool isLimited = true;
		}

		// Token: 0x0200004E RID: 78
		[Serializable]
		public class Node : IKSolver.Point
		{
			// Token: 0x0600026A RID: 618 RVA: 0x000104FB File Offset: 0x0000E8FB
			public Node()
			{
			}

			// Token: 0x0600026B RID: 619 RVA: 0x00010503 File Offset: 0x0000E903
			public Node(Transform transform)
			{
				this.transform = transform;
			}

			// Token: 0x0600026C RID: 620 RVA: 0x00010512 File Offset: 0x0000E912
			public Node(Transform transform, float weight)
			{
				this.transform = transform;
				this.weight = weight;
			}

			// Token: 0x0400020C RID: 524
			public float length;

			// Token: 0x0400020D RID: 525
			public float effectorPositionWeight;

			// Token: 0x0400020E RID: 526
			public float effectorRotationWeight;

			// Token: 0x0400020F RID: 527
			public Vector3 offset;
		}

		// Token: 0x0200004F RID: 79
		// (Invoke) Token: 0x0600026E RID: 622
		public delegate void UpdateDelegate();

		// Token: 0x02000050 RID: 80
		// (Invoke) Token: 0x06000272 RID: 626
		public delegate void IterationDelegate(int i);
	}
}
