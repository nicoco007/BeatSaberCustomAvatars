using UnityEngine;
using System.Collections;
using System;
using AvatarScriptPack;

namespace AvatarScriptPack {
	
	/// <summary>
	/// Hybrid %IK solver designed for mapping a character to a VR headset and 2 hand controllers 
	/// </summary>
	public partial class IKSolverVR: IKSolver {

		/// <summary>
		/// 4-segmented analytic leg chain.
		/// </summary>
		[System.Serializable]
		public class Leg: BodyPart {

			[Tooltip("The toe/foot target.")]
			/// <summary>
			/// The toe/foot target.
			/// </summary>
			public Transform target;

			[Tooltip("The knee will be bent towards this Transform if 'Bend Goal Weight' > 0.")]
			/// <summary>
			/// The knee will be bent towards this Transform if 'Bend Goal Weight' > 0.
			/// </summary>
			public Transform bendGoal;

			[Tooltip("Positional weight of the toe/foot target.")]
			/// <summary>
			/// Positional weight of the toe/foot target.
			/// </summary>
			[Range(0f, 1f)] public float positionWeight;

			[Tooltip("Rotational weight of the toe/foot target.")]
			/// <summary>
			/// Rotational weight of the toe/foot target.
			/// </summary>
			[Range(0f, 1f)] public float rotationWeight;

			[Tooltip("If greater than 0, will bend the knee towards the 'Bend Goal' Transform.")]
			/// <summary>
			/// If greater than 0, will bend the knee towards the 'Bend Goal' Transform.
			/// </summary>
			[Range(0f, 1f)] public float bendGoalWeight;

			[Tooltip("Angular offset of the knee bending direction.")]
			/// <summary>
			/// Angular offset of the knee bending direction.
			/// </summary>
			[Range(-180f, 180f)] public float swivelOffset;

			/// <summary>
			/// Target position of the toe/foot. Will be overwritten if target is assigned.
			/// </summary>
			[NonSerialized][HideInInspector] public Vector3 IKPosition;

			/// <summary>
			/// Target rotation of the toe/foot. Will be overwritten if target is assigned.
			/// </summary>
			[NonSerialized][HideInInspector] public Quaternion IKRotation = Quaternion.identity;

			/// <summary>
			/// Position offset of the toe/foot. Will be applied on top of target position and reset to Vector3.zero after each update.
			/// </summary>
			[NonSerialized][HideInInspector] public Vector3 footPositionOffset;

			/// <summary>
			/// Position offset of the heel. Will be reset to Vector3.zero after each update.
			/// </summary>
			[NonSerialized][HideInInspector] public Vector3 heelPositionOffset;

			/// <summary>
			/// Rotation offset of the toe/foot. Will be reset to Quaternion.identity after each update.
			/// </summary>
			[NonSerialized][HideInInspector] public Quaternion footRotationOffset = Quaternion.identity;

			/// <summary>
			/// The length of the leg (calculated in last read).
			/// </summary>
			[NonSerialized][HideInInspector] public float currentMag;

			public Vector3 position { get; private set; }
			public Quaternion rotation { get; private set; }
			public bool hasToes { get; private set; }
			public VirtualBone thigh { get { return bones[0]; }}
			private VirtualBone calf { get { return bones[1]; }}
			private VirtualBone foot { get { return bones[2]; }}
			private VirtualBone toes { get { return bones[3]; }}
			public VirtualBone lastBone { get { return bones[bones.Length - 1]; }}
			public Vector3 thighRelativeToPelvis { get; private set; }

			private Vector3 footPosition;
			private Quaternion footRotation = Quaternion.identity;
			private Vector3 bendNormal;
			private Quaternion calfRelToThigh = Quaternion.identity;

			protected override void OnRead(Vector3[] positions, Quaternion[] rotations, bool hasChest, bool hasNeck, bool hasShoulders, bool hasToes, int rootIndex, int index) {
				Vector3 thighPos = positions[index];
				Quaternion thighRot = rotations[index];
				Vector3 calfPos = positions[index + 1];
				Quaternion calfRot = rotations[index + 1];
				Vector3 footPos = positions[index + 2];
				Quaternion footRot = rotations[index + 2];
				Vector3 toePos = positions[index + 3];
				Quaternion toeRot = rotations[index + 3];

				if (!initiated) {
					this.hasToes = hasToes;
					bones = new VirtualBone[hasToes? 4: 3];

					if (hasToes) {
						bones[0] = new VirtualBone(thighPos, thighRot);
						bones[1] = new VirtualBone(calfPos, calfRot);
						bones[2] = new VirtualBone(footPos, footRot);
						bones[3] = new VirtualBone(toePos, toeRot);

						IKPosition = toePos;
						IKRotation = toeRot;
					} else {
						bones[0] = new VirtualBone(thighPos, thighRot);
						bones[1] = new VirtualBone(calfPos, calfRot);
						bones[2] = new VirtualBone(footPos, footRot);

						IKPosition = footPos;
						IKRotation = footRot;
					}

					rotation = IKRotation;
				}

				if (hasToes) {
					bones[0].Read(thighPos, thighRot);
					bones[1].Read(calfPos, calfRot);
					bones[2].Read(footPos, footRot);
					bones[3].Read(toePos, toeRot);
				} else {
					bones[0].Read(thighPos, thighRot);
					bones[1].Read(calfPos, calfRot);
					bones[2].Read(footPos, footRot);
				}
			}

			public override void PreSolve() {
				if (target != null) {
					IKPosition = target.position;
					IKRotation = target.rotation;
				}

				footPosition = foot.solverPosition;
				footRotation = foot.solverRotation;

				position = lastBone.solverPosition;
				rotation = lastBone.solverRotation;

				if (rotationWeight > 0f) {
					ApplyRotationOffset(AvatarScriptPack.QuaTools.FromToRotation(rotation, IKRotation), rotationWeight);
				}

				if (positionWeight > 0f) {
					ApplyPositionOffset(IKPosition - position, positionWeight);
				}

				thighRelativeToPelvis = Quaternion.Inverse(rootRotation) * (thigh.solverPosition - rootPosition);
				calfRelToThigh = Quaternion.Inverse(thigh.solverRotation) * calf.solverRotation;

				// Calculate bend plane normal
				bendNormal = Vector3.Cross(calf.solverPosition - thigh.solverPosition, foot.solverPosition - calf.solverPosition);
			}

			public override void ApplyOffsets() {
				ApplyPositionOffset(footPositionOffset, 1f);
				ApplyRotationOffset(footRotationOffset, 1f);

				// Heel position offset
				Quaternion fromTo = Quaternion.FromToRotation(footPosition - position, footPosition + heelPositionOffset - position);
				footPosition = position + fromTo * (footPosition - position);
				footRotation = fromTo * footRotation;

				// Bend normal offset
				float bAngle = 0f;

				if (bendGoal != null && bendGoalWeight > 0f) {
					Vector3 b = Vector3.Cross(bendGoal.position - thigh.solverPosition, position - thigh.solverPosition);
					Quaternion l = Quaternion.LookRotation(bendNormal, thigh.solverPosition - foot.solverPosition);
					Vector3 bRelative = Quaternion.Inverse(l) * b;
					bAngle = Mathf.Atan2(bRelative.x, bRelative.z) * Mathf.Rad2Deg * bendGoalWeight;
				}

				float sO = swivelOffset + bAngle;

				if (sO != 0f) {
					bendNormal = Quaternion.AngleAxis(sO, thigh.solverPosition - lastBone.solverPosition) * bendNormal;
					thigh.solverRotation = Quaternion.AngleAxis(-sO, thigh.solverRotation * thigh.axis) * thigh.solverRotation;
				}
			}

			// Foot position offset
			private void ApplyPositionOffset(Vector3 offset, float weight) {
				if (weight <= 0f) return;
				offset *= weight;

				// Foot position offset
				footPosition += offset;
				position += offset;
			}

			// Foot rotation offset
			private void ApplyRotationOffset(Quaternion offset, float weight) {
				if (weight <= 0f) return;
				if (weight < 1f) {
					offset = Quaternion.Lerp(Quaternion.identity, offset, weight);
				}

				footRotation = offset * footRotation;
				rotation = offset * rotation;
				bendNormal = offset * bendNormal;
				footPosition = position + offset * (footPosition - position);
			}

			public void Solve() {
				// Foot pass
				VirtualBone.SolveTrigonometric(bones, 0, 1, 2, footPosition, bendNormal, 1f);

				// Rotate foot back to where it was before the last solving
				RotateTo(foot, footRotation);
				
				// Toes pass
				if (!hasToes) return;
				
				Vector3 b = Vector3.Cross(foot.solverPosition - thigh.solverPosition, toes.solverPosition - foot.solverPosition);

				VirtualBone.SolveTrigonometric(bones, 0, 2, 3, position, b, 1f);

				// Fix calf twist relative to thigh
				Quaternion calfRotation = thigh.solverRotation * calfRelToThigh;
				Quaternion fromTo = Quaternion.FromToRotation(calfRotation * calf.axis, foot.solverPosition - calf.solverPosition);
				calf.solverRotation = fromTo * calfRotation;

				// Keep toe rotation fixed
				toes.solverRotation = rotation;
			}

			public override void Write(ref Vector3[] solvedPositions, ref Quaternion[] solvedRotations) {
				solvedRotations[index] = thigh.solverRotation;
				solvedRotations[index + 1] = calf.solverRotation;
				solvedRotations[index + 2] = foot.solverRotation;
				if (hasToes) solvedRotations[index + 3] = toes.solverRotation;
			}

			public override void ResetOffsets() {
				footPositionOffset = Vector3.zero;
				footRotationOffset = Quaternion.identity;
				heelPositionOffset = Vector3.zero;
			}
		}
	}
}