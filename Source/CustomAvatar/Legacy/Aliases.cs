//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

extern alias BeatSaberFinalIK;
extern alias BeatSaberDynamicBone;
using System;

namespace AvatarScriptPack
{
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.VRIK")] internal class VRIK : BeatSaberFinalIK::RootMotion.FinalIK.VRIK { }
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer")] internal class TwistRelaxer : BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer { }
    [Obsolete("Use CustomAvatar.FirstPersonExclusion")] internal class FirstPersonExclusion : CustomAvatar.FirstPersonExclusion { }
}

namespace RootMotion.FinalIK
{
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.VRIK")] internal class VRIK : BeatSaberFinalIK::RootMotion.FinalIK.VRIK { }
    [Obsolete("Use BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer")] internal class TwistRelaxer : BeatSaberFinalIK::RootMotion.FinalIK.TwistRelaxer { }
}

[Obsolete("Use BeatSaberDynamicBone::DynamicBone")] internal class DynamicBone : BeatSaberDynamicBone::DynamicBone { }
[Obsolete("Use BeatSaberDynamicBone::DynamicBoneColliderBase")] internal class DynamicBoneColliderBase : BeatSaberDynamicBone::DynamicBoneColliderBase { }
[Obsolete("Use BeatSaberDynamicBone::DynamicBoneCollider")] internal class DynamicBoneCollider : BeatSaberDynamicBone::DynamicBoneCollider { }
[Obsolete("Use BeatSaberDynamicBone::DynamicBonePlaneCollider")] internal class DynamicBonePlaneCollider : BeatSaberDynamicBone::DynamicBonePlaneCollider { }
