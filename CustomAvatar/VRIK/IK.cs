using System;

namespace AvatarScriptPack
{
	// Token: 0x02000034 RID: 52
	public abstract class IK : SolverManager
	{
		// Token: 0x0600017F RID: 383
		public abstract IKSolver GetIKSolver();

		// Token: 0x06000180 RID: 384 RVA: 0x00009B21 File Offset: 0x00007F21
		protected override void UpdateSolver()
		{
			if (!this.GetIKSolver().initiated)
			{
				this.InitiateSolver();
			}
			if (!this.GetIKSolver().initiated)
			{
				return;
			}
			this.GetIKSolver().Update();
		}

		// Token: 0x06000181 RID: 385 RVA: 0x00009B55 File Offset: 0x00007F55
		protected override void InitiateSolver()
		{
			if (this.GetIKSolver().initiated)
			{
				return;
			}
			this.GetIKSolver().Initiate(base.transform);
		}

		// Token: 0x06000182 RID: 386 RVA: 0x00009B79 File Offset: 0x00007F79
		protected override void FixTransforms()
		{
			if (!this.GetIKSolver().initiated)
			{
				return;
			}
			this.GetIKSolver().FixTransforms();
		}

		// Token: 0x06000183 RID: 387
		protected abstract void OpenUserManual();

		// Token: 0x06000184 RID: 388
		protected abstract void OpenScriptReference();
	}
}
