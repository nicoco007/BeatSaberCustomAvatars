extern alias BeatSaberFinalIK;
extern alias BeatSaberDynamicBone;
using System;

// ReSharper disable UnusedMember.Global
namespace AvatarScriptPack
{
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.VRIK")] internal class VRIK : BeatSaberFinalIK::RootMotion.FinalIK.VRIK { }
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer")] internal class TwistRelaxer : BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer { }
    [Obsolete("Use CustomAvatar.FirstPersonExclusion")] internal class FirstPersonExclusion : CustomAvatar.FirstPersonExclusion { }
}

namespace RootMotion.FinalIK
{
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.VRIK")] internal class VRIK : BeatSaberFinalIK::RootMotion.FinalIK.VRIK {  }
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer")] internal class TwistRelaxer : BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer { }
}

[Obsolete("Use BeatSaberDynamicBone::DynamicBone")] internal class DynamicBone : BeatSaberDynamicBone::DynamicBone { }
[Obsolete("Use BeatSaberDynamicBone::DynamicBoneColliderBase")] internal class DynamicBoneColliderBase : BeatSaberDynamicBone::DynamicBoneColliderBase { }
[Obsolete("Use BeatSaberDynamicBone::DynamicBoneCollider")] internal class DynamicBoneCollider : BeatSaberDynamicBone::DynamicBoneCollider { }
[Obsolete("Use BeatSaberDynamicBone::DynamicBonePlaneCollider")] internal class DynamicBonePlaneCollider : BeatSaberDynamicBone::DynamicBonePlaneCollider { }
