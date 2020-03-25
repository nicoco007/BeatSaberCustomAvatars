using UnityEngine;

namespace CustomAvatar.Avatar
{
    internal abstract class BodyAwareBehaviour : MonoBehaviour
    {
        protected Transform _head;
        protected Transform _body;
        protected Transform _leftHand;
        protected Transform _rightHand;
        protected Transform _leftLeg;
        protected Transform _rightLeg;
        protected Transform _pelvis;

        protected virtual void Start()
        {
            _head = transform.Find("Head");
            _body = transform.Find("Body");
            _leftHand = transform.Find("LeftHand");
            _rightHand = transform.Find("RightHand");
            _leftLeg = transform.Find("LeftLeg");
            _rightLeg = transform.Find("RightLeg");
            _pelvis = transform.Find("Pelvis");
        }
    }
}
