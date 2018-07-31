using System;
using UnityEngine;

namespace AvatarScriptPack
{
	// Token: 0x0200003A RID: 58
	[AddComponentMenu("Scripts/AvatarScriptPack/IK/VR IK")]
	public class VRIK : IK
	{
		// Token: 0x060001A5 RID: 421 RVA: 0x0000A230 File Offset: 0x00008630
		[ContextMenu("User Manual")]
		protected override void OpenUserManual()
		{
			Debug.Log("Sorry, VRIK User Manual is not finished yet.");
		}

		// Token: 0x060001A6 RID: 422 RVA: 0x0000A23C File Offset: 0x0000863C
		[ContextMenu("Scrpt Reference")]
		protected override void OpenScriptReference()
		{
			Debug.Log("Sorry, VRIK Script reference is not finished yet.");
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x0000A248 File Offset: 0x00008648
		[ContextMenu("TUTORIAL VIDEO (STEAMVR SETUP)")]
		private void OpenSetupTutorial()
		{
			Application.OpenURL("https://www.youtube.com/watch?v=6Pfx7lYQiIA&feature=youtu.be");
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x0000A254 File Offset: 0x00008654
		[ContextMenu("Auto-detect References")]
		public void AutoDetectReferences()
		{
			VRIK.References.AutoDetectReferences(base.transform, out this.references);
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x0000A268 File Offset: 0x00008668
		[ContextMenu("Guess Hand Orientations")]
		public void GuessHandOrientations()
		{
			this.solver.GuessHandOrientations(this.references, false);
		}

		// Token: 0x060001AA RID: 426 RVA: 0x0000A27C File Offset: 0x0000867C
		public override IKSolver GetIKSolver()
		{
			return this.solver;
		}

		// Token: 0x060001AB RID: 427 RVA: 0x0000A284 File Offset: 0x00008684
		protected override void InitiateSolver()
		{
			if (this.references.isEmpty)
			{
				this.AutoDetectReferences();
			}
			if (this.references.isFilled)
			{
				this.solver.SetToReferences(this.references);
			}
			base.InitiateSolver();
		}

		// Token: 0x04000116 RID: 278
		[ContextMenuItem("Auto-detect References", "AutoDetectReferences")]
		[Tooltip("Bone mapping. Right-click on the component header and select 'Auto-detect References' of fill in manually if not a Humanoid character.")]
		public VRIK.References references = new VRIK.References();

		// Token: 0x04000117 RID: 279
		[Tooltip("The VRIK solver.")]
		public IKSolverVR solver = new IKSolverVR();

		// Token: 0x0200003B RID: 59
		[Serializable]
		public class References
		{
			// Token: 0x060001AD RID: 429 RVA: 0x0000A2CC File Offset: 0x000086CC
			public Transform[] GetTransforms()
			{
				return new Transform[]
				{
					this.root,
					this.pelvis,
					this.spine,
					this.chest,
					this.neck,
					this.head,
					this.leftShoulder,
					this.leftUpperArm,
					this.leftForearm,
					this.leftHand,
					this.rightShoulder,
					this.rightUpperArm,
					this.rightForearm,
					this.rightHand,
					this.leftThigh,
					this.leftCalf,
					this.leftFoot,
					this.leftToes,
					this.rightThigh,
					this.rightCalf,
					this.rightFoot,
					this.rightToes
				};
			}

			// Token: 0x1700002C RID: 44
			// (get) Token: 0x060001AE RID: 430 RVA: 0x0000A3B4 File Offset: 0x000087B4
			public bool isFilled
			{
				get
				{
					return !(this.root == null) && !(this.pelvis == null) && !(this.spine == null) && !(this.head == null) && !(this.leftUpperArm == null) && !(this.leftForearm == null) && !(this.leftHand == null) && !(this.rightUpperArm == null) && !(this.rightForearm == null) && !(this.rightHand == null) && !(this.leftThigh == null) && !(this.leftCalf == null) && !(this.leftFoot == null) && !(this.rightThigh == null) && !(this.rightCalf == null) && !(this.rightFoot == null);
				}
			}

			// Token: 0x1700002D RID: 45
			// (get) Token: 0x060001AF RID: 431 RVA: 0x0000A4D4 File Offset: 0x000088D4
			public bool isEmpty
			{
				get
				{
					return !(this.root != null) && !(this.pelvis != null) && !(this.spine != null) && !(this.chest != null) && !(this.neck != null) && !(this.head != null) && !(this.leftShoulder != null) && !(this.leftUpperArm != null) && !(this.leftForearm != null) && !(this.leftHand != null) && !(this.rightShoulder != null) && !(this.rightUpperArm != null) && !(this.rightForearm != null) && !(this.rightHand != null) && !(this.leftThigh != null) && !(this.leftCalf != null) && !(this.leftFoot != null) && !(this.leftToes != null) && !(this.rightThigh != null) && !(this.rightCalf != null) && !(this.rightFoot != null) && !(this.rightToes != null);
				}
			}

			// Token: 0x060001B0 RID: 432 RVA: 0x0000A65C File Offset: 0x00008A5C
			public static bool AutoDetectReferences(Transform root, out VRIK.References references)
			{
				references = new VRIK.References();
				Animator componentInChildren = root.GetComponentInChildren<Animator>();
				if (componentInChildren == null || !componentInChildren.isHuman)
				{
					Debug.LogWarning("VRIK needs a Humanoid Animator to auto-detect biped references. Please assign references manually.");
					return false;
				}
				references.root = root;
				references.pelvis = componentInChildren.GetBoneTransform(HumanBodyBones.Hips);
				references.spine = componentInChildren.GetBoneTransform(HumanBodyBones.Spine);
				references.chest = componentInChildren.GetBoneTransform(HumanBodyBones.Chest);
				references.neck = componentInChildren.GetBoneTransform(HumanBodyBones.Neck);
				references.head = componentInChildren.GetBoneTransform(HumanBodyBones.Head);
				references.leftShoulder = componentInChildren.GetBoneTransform(HumanBodyBones.LeftShoulder);
				references.leftUpperArm = componentInChildren.GetBoneTransform(HumanBodyBones.LeftUpperArm);
				references.leftForearm = componentInChildren.GetBoneTransform(HumanBodyBones.LeftLowerArm);
				references.leftHand = componentInChildren.GetBoneTransform(HumanBodyBones.LeftHand);
				references.rightShoulder = componentInChildren.GetBoneTransform(HumanBodyBones.RightShoulder);
				references.rightUpperArm = componentInChildren.GetBoneTransform(HumanBodyBones.RightUpperArm);
				references.rightForearm = componentInChildren.GetBoneTransform(HumanBodyBones.RightLowerArm);
				references.rightHand = componentInChildren.GetBoneTransform(HumanBodyBones.RightHand);
				references.leftThigh = componentInChildren.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
				references.leftCalf = componentInChildren.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
				references.leftFoot = componentInChildren.GetBoneTransform(HumanBodyBones.LeftFoot);
				references.leftToes = componentInChildren.GetBoneTransform(HumanBodyBones.LeftToes);
				references.rightThigh = componentInChildren.GetBoneTransform(HumanBodyBones.RightUpperLeg);
				references.rightCalf = componentInChildren.GetBoneTransform(HumanBodyBones.RightLowerLeg);
				references.rightFoot = componentInChildren.GetBoneTransform(HumanBodyBones.RightFoot);
				references.rightToes = componentInChildren.GetBoneTransform(HumanBodyBones.RightToes);
				return true;
			}

			// Token: 0x04000118 RID: 280
			public Transform root;

			// Token: 0x04000119 RID: 281
			public Transform pelvis;

			// Token: 0x0400011A RID: 282
			public Transform spine;

			// Token: 0x0400011B RID: 283
			public Transform chest;

			// Token: 0x0400011C RID: 284
			public Transform neck;

			// Token: 0x0400011D RID: 285
			public Transform head;

			// Token: 0x0400011E RID: 286
			public Transform leftShoulder;

			// Token: 0x0400011F RID: 287
			public Transform leftUpperArm;

			// Token: 0x04000120 RID: 288
			public Transform leftForearm;

			// Token: 0x04000121 RID: 289
			public Transform leftHand;

			// Token: 0x04000122 RID: 290
			public Transform rightShoulder;

			// Token: 0x04000123 RID: 291
			public Transform rightUpperArm;

			// Token: 0x04000124 RID: 292
			public Transform rightForearm;

			// Token: 0x04000125 RID: 293
			public Transform rightHand;

			// Token: 0x04000126 RID: 294
			public Transform leftThigh;

			// Token: 0x04000127 RID: 295
			public Transform leftCalf;

			// Token: 0x04000128 RID: 296
			public Transform leftFoot;

			// Token: 0x04000129 RID: 297
			public Transform leftToes;

			// Token: 0x0400012A RID: 298
			public Transform rightThigh;

			// Token: 0x0400012B RID: 299
			public Transform rightCalf;

			// Token: 0x0400012C RID: 300
			public Transform rightFoot;

			// Token: 0x0400012D RID: 301
			public Transform rightToes;
		}
	}
}
