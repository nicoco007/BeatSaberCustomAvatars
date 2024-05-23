extern alias BeatSaberFinalIK;

using BeatSaberFinalIK::RootMotion.FinalIK;
using UnityEngine;

namespace CustomAvatar.Scripts
{
    /// <summary>
    /// Relaxes the twist rotation if the Transform relative to its parent and a child Transforms, using the Transform's initial rotation as the most relaxed pose.
    /// </summary>
    [DisallowMultipleComponent]
    public class TwistRelaxerV2 : MonoBehaviour
    {
        [Tooltip("If the transform on which this component is placed is the forearm roll bone, the parent should be the forearm bone. If null, will be found automatically.")]
        public Transform parent;

        [Tooltip("If the transform on which this component is placed is the forearm roll bone, the child should be the hand bone. If null, will attempt to find automatically. Assign the hand manually if the hand bone is not a child of the roll bone.")]
        public Transform[] children = new Transform[0];

        [Tooltip("The weight of relaxing the twist of this transform.")]
        [Range(0f, 1f)] public float weight = 1f;

        [Tooltip("If 0.5, this transform will be twisted half way from parent to child. If 1, the twist angle will be locked to the child and will rotate with along with it.")]
        [Range(0f, 1f)] public float parentChildCrossfade = 0.5f;

        [Tooltip("Rotation offset around the twist axis.")]
        [Range(-180f, 180f)] public float twistAngleOffset;

#pragma warning disable CS0649
        internal IK ik;
#pragma warning restore CS0649

        private Vector3 _twistAxis = Vector3.right;
        private Vector3 _axis = Vector3.forward;
        private Vector3 _axisRelativeToParentDefault;
        private Vector3 _axisRelativeToChildDefault;
        private Quaternion[] _childRotations;
        private bool _inititated;
        private Quaternion _defaultLocalRotation = Quaternion.identity;
        private Quaternion[] _defaultChildLocalRotations;

        internal void OnPreUpdate()
        {
            if (ik.fixTransforms)
            {
                FixTransforms();
            }
        }

        internal void OnPostUpdate()
        {
            Relax();
        }

        internal void FixTransforms()
        {
            transform.localRotation = _defaultLocalRotation;

            for (int i = 0; i < children.Length; i++)
            {
                children[i].localRotation = _defaultChildLocalRotations[i];
            }
        }

        private void Start()
        {
            if (_inititated)
            {
                return;
            }

            if (parent == null)
            {
                parent = transform.parent;
            }

            if (children.Length == 0)
            {
                if (transform.childCount == 0)
                {
                    Transform[] children = parent.GetComponentsInChildren<Transform>();
                    for (int i = 1; i < children.Length; i++)
                    {
                        if (children[i] != transform)
                        {
                            this.children = new Transform[1] { children[i] };
                            break;
                        }
                    }
                }
                else
                {
                    children = new Transform[1] { transform.GetChild(0) };
                }
            }

            if (children.Length == 0 || children[0] == null)
            {
                Debug.LogError("TwistRelaxer has no children assigned.", transform);
                return;
            }

            _twistAxis = transform.InverseTransformDirection(children[0].position - transform.position);
            _axis = new Vector3(_twistAxis.y, _twistAxis.z, _twistAxis.x);

            // Axis in world space
            Vector3 axisWorld = transform.rotation * _axis;

            // Store the axis in worldspace relative to the rotations of the parent and child
            _axisRelativeToParentDefault = Quaternion.Inverse(parent.rotation) * axisWorld;
            _axisRelativeToChildDefault = Quaternion.Inverse(children[0].rotation) * axisWorld;

            _childRotations = new Quaternion[children.Length];

            _defaultLocalRotation = transform.localRotation;
            _defaultChildLocalRotations = new Quaternion[children.Length];
            for (int i = 0; i < children.Length; i++)
            {
                _defaultChildLocalRotations[i] = children[i].localRotation;
            }

            if (ik != null)
            {
                IKSolver solver = ik.GetIKSolver();
                solver.OnPreUpdate += OnPreUpdate;
                solver.OnPostUpdate += OnPostUpdate;
            }

            _inititated = true;
        }

        private void Update()
        {
            if (ik == null)
            {
                FixTransforms();
            }
        }

        private void LateUpdate()
        {
            if (ik == null)
            {
                Relax();
            }
        }

        private void OnDestroy()
        {
            if (ik != null)
            {
                IKSolver solver = ik.GetIKSolver();
                solver.OnPreUpdate -= OnPreUpdate;
                solver.OnPostUpdate -= OnPostUpdate;
            }
        }

        private void Relax()
        {
            if (!_inititated || weight <= 0f)
            {
                return;
            }

            Quaternion rotation = transform.rotation;
            var twistOffset = Quaternion.AngleAxis(twistAngleOffset, rotation * _twistAxis);
            rotation = twistOffset * rotation;

            // Find the world space relaxed axes of the parent and child
            Vector3 relaxedAxisParent = twistOffset * parent.rotation * _axisRelativeToParentDefault;
            Vector3 relaxedAxisChild = twistOffset * children[0].rotation * _axisRelativeToChildDefault;

            // Cross-fade between the parent and child
            var relaxedAxis = Vector3.Slerp(relaxedAxisParent, relaxedAxisChild, parentChildCrossfade);

            // Convert relaxedAxis to (axis, twistAxis) space so we could calculate the twist angle
            var rotationInAxisSpace = Quaternion.LookRotation(rotation * _axis, rotation * _twistAxis);
            relaxedAxis = Quaternion.Inverse(rotationInAxisSpace) * relaxedAxis;

            // Calculate the angle by which we need to rotate this Transform around the twist axis.
            float angle = Mathf.Atan2(relaxedAxis.x, relaxedAxis.z) * Mathf.Rad2Deg;

            // Store the rotation of the child so it would not change with twisting this Transform
            for (int i = 0; i < children.Length; i++)
            {
                _childRotations[i] = children[i].rotation;
            }

            // Twist the bone
            transform.rotation = Quaternion.AngleAxis(angle * weight, rotation * _twistAxis) * rotation;

            // Revert the rotation of the child
            for (int i = 0; i < children.Length; i++)
            {
                children[i].rotation = _childRotations[i];
            }
        }
    }
}
