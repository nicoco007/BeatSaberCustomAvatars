using System;
using UnityEngine;

namespace CustomAvatar.Tracking
{
    public abstract class AvatarInput
    {
        public event Action inputChanged;

        protected void InvokeInputChanged()
        {
            inputChanged?.Invoke();
        }

        public virtual bool TryGetHeadPose(out Pose pose)
        {
            pose = Pose.identity;
            return false;
        }

        public virtual bool TryGetLeftHandPose(out Pose pose)
        {
            pose = Pose.identity;
            return false;
        }

        public virtual bool TryGetRightHandPose(out Pose pose)
        {
            pose = Pose.identity;
            return false;
        }

        public virtual bool TryGetWaistPose(out Pose pose)
        {
            pose = Pose.identity;
            return false;
        }

        public virtual bool TryGetLeftFootPose(out Pose pose)
        {
            pose = Pose.identity;
            return false;
        }

        public virtual bool TryGetRightFootPose(out Pose pose)
        {
            pose = Pose.identity;
            return false;
        }

        public virtual bool TryGetLeftHandFingerCurl(out FingerCurl curl)
        {
            curl = null;
            return false;
        }

        public virtual bool TryGetRightHandFingerCurl(out FingerCurl curl)
        {
            curl = null;
            return false;
        }
    }
}
