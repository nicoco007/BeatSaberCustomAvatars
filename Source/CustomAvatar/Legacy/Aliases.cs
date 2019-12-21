extern alias BeatSaberFinalIK;
extern alias BeatSaberDynamicBone;

namespace AvatarScriptPack
{
    public class VRIK : BeatSaberFinalIK::RootMotion.FinalIK.VRIK { }
    public class TwistRelaxer : BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer { }
}

namespace RootMotion.FinalIK
{
    public class VRIK : BeatSaberFinalIK::RootMotion.FinalIK.VRIK {  }
    public class TwistRelaxer : BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer { }
}

public class DynamicBone : BeatSaberDynamicBone::DynamicBone { }
public class DynamicBoneCollider : BeatSaberDynamicBone::DynamicBoneCollider { }
public class DynamicBonePlaneCollider : BeatSaberDynamicBone::DynamicBonePlaneCollider { }
