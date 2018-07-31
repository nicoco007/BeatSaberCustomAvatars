using System;
using UnityEngine;

namespace AvatarScriptPack
{
	// Token: 0x02000175 RID: 373
	public class SolverManager : MonoBehaviour
	{
		// Token: 0x060009C7 RID: 2503 RVA: 0x00004DD8 File Offset: 0x000031D8
		public void Disable()
		{
			Debug.Log("IK.Disable() is deprecated. Use enabled = false instead", base.transform);
			base.enabled = false;
		}

		// Token: 0x060009C8 RID: 2504 RVA: 0x00004DF1 File Offset: 0x000031F1
		protected virtual void InitiateSolver()
		{
		}

		// Token: 0x060009C9 RID: 2505 RVA: 0x00004DF3 File Offset: 0x000031F3
		protected virtual void UpdateSolver()
		{
		}

		// Token: 0x060009CA RID: 2506 RVA: 0x00004DF5 File Offset: 0x000031F5
		protected virtual void FixTransforms()
		{
		}

		// Token: 0x060009CB RID: 2507 RVA: 0x00004DF7 File Offset: 0x000031F7
		private void OnDisable()
		{
			if (!Application.isPlaying)
			{
				return;
			}
			this.Initiate();
		}

		// Token: 0x060009CC RID: 2508 RVA: 0x00004E0A File Offset: 0x0000320A
		private void Start()
		{
			this.Initiate();
		}

		// Token: 0x17000101 RID: 257
		// (get) Token: 0x060009CD RID: 2509 RVA: 0x00004E12 File Offset: 0x00003212
		private bool animatePhysics
		{
			get
			{
				if (this.animator != null)
				{
					return this.animator.updateMode == AnimatorUpdateMode.AnimatePhysics;
				}
				return this.legacy != null && this.legacy.animatePhysics;
			}
		}

		// Token: 0x060009CE RID: 2510 RVA: 0x00004E52 File Offset: 0x00003252
		private void Initiate()
		{
			if (this.componentInitiated)
			{
				return;
			}
			this.FindAnimatorRecursive(base.transform, true);
			this.InitiateSolver();
			this.componentInitiated = true;
		}

		// Token: 0x060009CF RID: 2511 RVA: 0x00004E7A File Offset: 0x0000327A
		private void Update()
		{
			if (this.skipSolverUpdate)
			{
				return;
			}
			if (this.animatePhysics)
			{
				return;
			}
			if (this.fixTransforms)
			{
				this.FixTransforms();
			}
		}

		// Token: 0x060009D0 RID: 2512 RVA: 0x00004EA8 File Offset: 0x000032A8
		private void FindAnimatorRecursive(Transform t, bool findInChildren)
		{
			if (this.isAnimated)
			{
				return;
			}
			this.animator = t.GetComponent<Animator>();
			this.legacy = t.GetComponent<Animation>();
			if (this.isAnimated)
			{
				return;
			}
			if (this.animator == null && findInChildren)
			{
				this.animator = t.GetComponentInChildren<Animator>();
			}
			if (this.legacy == null && findInChildren)
			{
				this.legacy = t.GetComponentInChildren<Animation>();
			}
			if (!this.isAnimated && t.parent != null)
			{
				this.FindAnimatorRecursive(t.parent, false);
			}
		}

		// Token: 0x17000102 RID: 258
		// (get) Token: 0x060009D1 RID: 2513 RVA: 0x00004F54 File Offset: 0x00003354
		private bool isAnimated
		{
			get
			{
				return this.animator != null || this.legacy != null;
			}
		}

		// Token: 0x060009D2 RID: 2514 RVA: 0x00004F76 File Offset: 0x00003376
		private void FixedUpdate()
		{
			if (this.skipSolverUpdate)
			{
				this.skipSolverUpdate = false;
			}
			this.updateFrame = true;
			if (this.animatePhysics && this.fixTransforms)
			{
				this.FixTransforms();
			}
		}

		// Token: 0x060009D3 RID: 2515 RVA: 0x00004FAD File Offset: 0x000033AD
		private void LateUpdate()
		{
			if (this.skipSolverUpdate)
			{
				return;
			}
			if (!this.animatePhysics)
			{
				this.updateFrame = true;
			}
			if (!this.updateFrame)
			{
				return;
			}
			this.updateFrame = false;
			this.UpdateSolver();
		}

		// Token: 0x060009D4 RID: 2516 RVA: 0x00004FE6 File Offset: 0x000033E6
		public void UpdateSolverExternal()
		{
			if (!base.enabled)
			{
				return;
			}
			this.skipSolverUpdate = true;
			this.UpdateSolver();
		}

		// Token: 0x04000A0D RID: 2573
		[Tooltip("If true, will fix all the Transforms used by the solver to their initial state in each Update. This prevents potential problems with unanimated bones and animator culling with a small cost of performance. Not recommended for CCD and FABRIK solvers.")]
		public bool fixTransforms = true;

		// Token: 0x04000A0E RID: 2574
		private Animator animator;

		// Token: 0x04000A0F RID: 2575
		private Animation legacy;

		// Token: 0x04000A10 RID: 2576
		private bool updateFrame;

		// Token: 0x04000A11 RID: 2577
		private bool componentInitiated;

		// Token: 0x04000A12 RID: 2578
		private bool skipSolverUpdate;
	}
}
