using System;
using UnityEngine;

namespace CustomAvatar.Tracking
{
    public interface IAvatarInput : IDisposable
    {
        bool allowMaintainPelvisPosition { get; }

        event Action inputChanged;

        bool TryGetHeadPose(out Pose pose);
        bool TryGetLeftHandPose(out Pose pose);
        bool TryGetRightHandPose(out Pose pose);
        bool TryGetWaistPose(out Pose pose);
        bool TryGetLeftFootPose(out Pose pose);
        bool TryGetRightFootPose(out Pose pose);
        bool TryGetLeftHandFingerCurl(out FingerCurl curl);
        bool TryGetRightHandFingerCurl(out FingerCurl curl);
    }
}
