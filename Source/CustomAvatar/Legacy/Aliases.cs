extern alias BeatSaberFinalIK;
extern alias BeatSaberDynamicBone;
using System;

// ReSharper disable UnusedMember.Global
namespace AvatarScriptPack
{
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.VRIK")] public class VRIK : BeatSaberFinalIK::RootMotion.FinalIK.VRIK { }
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer")] public class TwistRelaxer : BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer { }
    [Obsolete("Use CustomAvatar.FirstPersonExclusion")] public class FirstPersonExclusion : CustomAvatar.FirstPersonExclusion { }
}

namespace RootMotion.FinalIK
{
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.VRIK")] public class VRIK : BeatSaberFinalIK::RootMotion.FinalIK.VRIK {  }
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer")] public class TwistRelaxer : BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer { }
}

[Obsolete("Use BeatSaberDynamicBone::DynamicBone")] public class DynamicBone : BeatSaberDynamicBone::DynamicBone { }
[Obsolete("Use BeatSaberDynamicBone::DynamicBoneColliderBase")] public class DynamicBoneColliderBase : BeatSaberDynamicBone::DynamicBoneColliderBase { }
[Obsolete("Use BeatSaberDynamicBone::DynamicBoneCollider")] public class DynamicBoneCollider : BeatSaberDynamicBone::DynamicBoneCollider { }
[Obsolete("Use BeatSaberDynamicBone::DynamicBonePlaneCollider")] public class DynamicBonePlaneCollider : BeatSaberDynamicBone::DynamicBonePlaneCollider { }
