using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AvatarScriptPack {
	
	/// <summary>
	/// A full-body IK solver designed specifically for a VR HMD and hand controllers.
	/// </summary>
	//[HelpURL("http://www.root-motion.com/finalikdox/html/page7.html")]
	[AddComponentMenu("Scripts/AvatarScriptPack/IK/VR IK")]
	public class VRIK : IK {

		/// <summary>
		/// VRIK-specific definition of a humanoid biped.
		/// </summary>
		[System.Serializable]
		public class References {
			public Transform root;
			public Transform pelvis;
			public Transform spine;
			public Transform chest; // Optional
			public Transform neck; // Optional
			public Transform head;
			public Transform leftShoulder; // Optional
			public Transform leftUpperArm;
			public Transform leftForearm;
			public Transform leftHand;
			public Transform rightShoulder; // Optional
			public Transform rightUpperArm;
			public Transform rightForearm;
			public Transform rightHand;
			public Transform leftThigh;
			public Transform leftCalf;
			public Transform leftFoot;
			public Transform leftToes; // Optional
			public Transform rightThigh;
			public Transform rightCalf;
			public Transform rightFoot;
			public Transform rightToes; // Optional

			/// <summary>
			/// Returns an array of all the Transforms in the definition.
			/// </summary>
			public Transform[] GetTransforms() {
				return new Transform[22] {
					root, pelvis, spine, chest, neck, head, leftShoulder, leftUpperArm, leftForearm, leftHand, rightShoulder, rightUpperArm, rightForearm, rightHand, leftThigh, leftCalf, leftFoot, leftToes, rightThigh, rightCalf, rightFoot, rightToes
				};
			}

			/// <summary>
			/// Returns true if all required Transforms have been assigned (shoulder, toe and neck bones are optional).
			/// </summary>
			public bool isFilled {
				get {
					if (
						root == null ||
						pelvis == null ||
						spine == null ||
						head == null ||
						leftUpperArm == null ||
						leftForearm == null ||
						leftHand == null ||
						rightUpperArm == null ||
						rightForearm == null ||
						rightHand == null ||
						leftThigh == null ||
						leftCalf == null ||
						leftFoot == null ||
						rightThigh == null ||
						rightCalf == null ||
						rightFoot == null
					) return false;

					// Shoulder, toe and neck bones are optional
					return true;
				}
			}

			/// <summary>
			/// Returns true if none of the Transforms have been assigned.
			/// </summary>
			public bool isEmpty {
				get {
					if (
						root != null ||
						pelvis != null ||
						spine != null ||
						chest != null ||
						neck != null ||
						head != null ||
						leftShoulder != null ||
						leftUpperArm != null ||
						leftForearm != null ||
						leftHand != null ||
						rightShoulder != null ||
						rightUpperArm != null ||
						rightForearm != null ||
						rightHand != null ||
						leftThigh != null ||
						leftCalf != null ||
						leftFoot != null ||
						leftToes != null ||
						rightThigh != null ||
						rightCalf != null ||
						rightFoot != null ||
						rightToes != null
					) return false;

					return true;
				}
			}

			/// <summary>
			/// Auto-detects VRIK references. Works with a Humanoid Animator on the root gameobject only.
			/// </summary>
			public static bool AutoDetectReferences(Transform root, out References references) {
				references = new References();

				var animator = root.GetComponentInChildren<Animator>();
                Debug.Log("Root: " + root + " " + root.name);
				if (animator == null || !animator.isHuman) {
					Debug.LogWarning("VRIK needs a Humanoid Animator to auto-detect biped references. Please assign references manually.");
					return false;
				}

				references.root = root;
				references.pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
				references.spine = animator.GetBoneTransform(HumanBodyBones.Spine);
				references.chest = animator.GetBoneTransform(HumanBodyBones.Chest);
				references.neck = animator.GetBoneTransform(HumanBodyBones.Neck);
				references.head = animator.GetBoneTransform(HumanBodyBones.Head);
				references.leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
				references.leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
				references.leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
				references.leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
				references.rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
				references.rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
				references.rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
				references.rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
				references.leftThigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
				references.leftCalf = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
				references.leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
				references.leftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);
				references.rightThigh = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
				references.rightCalf = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
				references.rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
				references.rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);

				return true;
			}
		}

		// Open the User Manual URL
		[ContextMenu("User Manual")]
		protected override void OpenUserManual() {
			Debug.Log ("Sorry, VRIK User Manual is not finished yet.");
			// TODO Application.OpenURL("http://www.root-motion.com/finalikdox/html/page6.html");
		}
		
		// Open the Script Reference URL
		[ContextMenu("Scrpt Reference")]
		protected override void OpenScriptReference() {
			Debug.Log ("Sorry, VRIK Script reference is not finished yet.");
			// TODO Application.OpenURL("http://www.root-motion.com/finalikdox/html/class_root_motion_1_1_final_i_k_1_1_full_body_biped_i_k.html");
		}

		// Open a video tutorial about setting up the component
		[ContextMenu("TUTORIAL VIDEO (STEAMVR SETUP)")]
		void OpenSetupTutorial() {
			Application.OpenURL("https://www.youtube.com/watch?v=6Pfx7lYQiIA&feature=youtu.be");
		}

		/// <summary>
		/// The biped definition.
		/// </summary>
		[ContextMenuItem("Auto-detect References", "AutoDetectReferences")]
		[Tooltip("Bone mapping. Right-click on the component header and select 'Auto-detect References' of fill in manually if not a Humanoid character.")]
		public References references = new References();
		
		/// <summary>
		/// The solver.
		/// </summary>
		[Tooltip("The VRIK solver.")]
		public IKSolverVR solver = new IKSolverVR();

		/// <summary>
		/// Auto-detects bone references for this VRIK. Works with a Humanoid Animator on the gameobject only.
		/// </summary>
		[ContextMenu("Auto-detect References")]
		public void AutoDetectReferences() {
			References.AutoDetectReferences(transform, out references);
		}

		/// <summary>
		/// Fills in arm wristToPalmAxis and palmToThumbAxis.
		/// </summary>
		[ContextMenu("Guess Hand Orientations")]
		public void GuessHandOrientations() {
			solver.GuessHandOrientations(references, false);
		}

		public override IKSolver GetIKSolver() {
			return solver as IKSolver;
		}

		protected override void InitiateSolver() {
			if (references.isEmpty) AutoDetectReferences();
			if (references.isFilled) solver.SetToReferences(references);

			base.InitiateSolver();
		}
	}
}
