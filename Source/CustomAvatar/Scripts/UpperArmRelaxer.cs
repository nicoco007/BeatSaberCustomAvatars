extern alias BeatSaberFinalIK;

using BeatSaberFinalIK::RootMotion.FinalIK;
using UnityEngine;

namespace CustomAvatar.Scripts
{
    public class UpperArmRelaxer : MonoBehaviour
    {
        public IK ik;

        public Transform[] children;

        [Tooltip("The weight of relaxing the twist.")]
        [Range(0f, 1f)] public float weight = 1f;

        [Tooltip("If 0.5, this Transform will be twisted half way from parent to child. If 1, the twist angle will be locked to the child and will rotate with along with it.")]
        [Range(0f, 1f)] public float parentChildCrossfade = 0.15f;

        [Tooltip("Rotation offset around the twist axis.")]
        [Range(-180f, 180f)] public float twistAngleOffset;

        /// <summary>
        /// Rotate this Transform to relax it's twist angle relative to the "parent" and "child" Transforms.
        /// </summary>
        public void Relax()
        {
            if (weight <= 0f) return; // Nothing to do here

            Quaternion rotation = transform.rotation;
            var twistOffset = Quaternion.AngleAxis(twistAngleOffset, rotation * _twistAxis);
            rotation = twistOffset * rotation;

            // Find the world space relaxed axes of the parent and child
            Vector3 relaxedAxisDefault = transform.parent.rotation * _parentDefaultLocalRotation * _axisRelativeToSelfDefault;
            Vector3 relaxedAxisCurrent = rotation * _axisRelativeToSelfDefault;

            // Cross-fade between the parent and child
            var relaxedAxis = Vector3.Slerp(relaxedAxisDefault, relaxedAxisCurrent, parentChildCrossfade);

            // Convert relaxedAxis to (axis, twistAxis) space so we could calculate the twist angle
            var r = Quaternion.LookRotation(rotation * _axis, rotation * _twistAxis);
            relaxedAxis = Quaternion.Inverse(r) * relaxedAxis;

            // Calculate the angle by which we need to rotate this Transform around the twist axis.
            float angle = Mathf.Atan2(relaxedAxis.x, relaxedAxis.z) * Mathf.Rad2Deg;

            // Store the rotation of the child so it would not change with twisting this Transform
            var childrenRotations = new Quaternion[children.Length];

            for (int i = 0; i < children.Length; i++)
            {
                childrenRotations[i] = children[i].rotation;
            }

            // Twist the bone
            transform.rotation = Quaternion.AngleAxis(angle * weight, rotation * _twistAxis) * rotation;

            // Revert the rotation of the child
            for (int i = 0; i < children.Length; i++)
            {
                children[i].rotation = childrenRotations[i];
            }
        }

        private Vector3 _twistAxis = Vector3.right;
        private Vector3 _axis = Vector3.forward;
        private Vector3 _axisRelativeToSelfDefault;

        private Quaternion _parentDefaultLocalRotation;

        private Pose[] _childrenDefaultLocalPoses;

        private void OnEnable()
        {
            if (ik != null)
            {
                IKSolver solver = ik.GetIKSolver();
                solver.OnPreUpdate += OnPreUpdate;
                solver.OnPostUpdate += OnPostUpdate;
            }
        }

        internal void Start()
        {
            _twistAxis = transform.InverseTransformDirection(children[0].position - transform.position);
            _axis = new Vector3(_twistAxis.y, _twistAxis.z, _twistAxis.x);

            // Axis in world space
            Vector3 axisWorld = transform.rotation * _axis;

            // Store the axis in worldspace relative to the rotations of the parent and child
            _axisRelativeToSelfDefault = Quaternion.Inverse(transform.rotation) * axisWorld;
            _parentDefaultLocalRotation = transform.localRotation;

            _childrenDefaultLocalPoses = new Pose[children.Length];

            for (int i = 0; i < children.Length; i++)
            {
                _childrenDefaultLocalPoses[i] = new Pose(children[i].localPosition, children[i].localRotation);
            }
        }

        private void Update()
        {
            if (ik == null)
                FixTransforms();
        }

        private void LateUpdate()
        {
            if (ik == null)
                Relax();
        }

        private void OnDisable()
        {
            if (ik != null)
            {
                IKSolver solver = ik.GetIKSolver();
                solver.OnPreUpdate -= OnPreUpdate;
                solver.OnPostUpdate -= OnPostUpdate;
            }
        }

        internal void OnPreUpdate()
        {
            if (ik != null)
                FixTransforms();
        }

        internal void OnPostUpdate()
        {
            if (ik != null)
                Relax();
        }

        internal void FixTransforms()
        {
            // this is what FixTransforms does but these bones aren't listed in the references so we do it manually
            for (int i = 0; i < children.Length; i++)
            {
                children[i].localPosition = _childrenDefaultLocalPoses[i].position;
                children[i].localRotation = _childrenDefaultLocalPoses[i].rotation;
            }
        }
    }
}
