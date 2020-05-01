using UnityEngine;

namespace CustomAvatar.Avatar
{
    public abstract class BodyAwareBehaviour : MonoBehaviour
    {
        public Transform head { get; private set; }
        public Transform body { get; private set; }
        public Transform leftHand { get; private set; }
        public Transform rightHand { get; private set; }
        public Transform leftLeg { get; private set; }
        public Transform rightLeg { get; private set; }
        public Transform pelvis { get; private set; }

        protected virtual void Start()
        {
            head = transform.Find("Head");
            body = transform.Find("Body");
            leftHand = transform.Find("LeftHand");
            rightHand = transform.Find("RightHand");
            leftLeg = transform.Find("LeftLeg");
            rightLeg = transform.Find("RightLeg");
            pelvis = transform.Find("Pelvis");
        }
    }
}
