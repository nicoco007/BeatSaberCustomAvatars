//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

namespace CustomAvatar.Avatar
{
    public static class AvatarLayers
    {
        public static readonly int kAlwaysVisible = 10; // Beat Saber's "Avatar" layer
        public static readonly int kOnlyInThirdPerson = 3; // technically reserved for Unity but changing it breaks compatibility with other mods and changing it just because Unity *might* use it someday isn't worth it right now

        public static readonly int kAlwaysVisibleMask = 1 << kAlwaysVisible;
        public static readonly int kOnlyInThirdPersonMask = 1 << kOnlyInThirdPerson;
        public static readonly int kAllLayersMask = kAlwaysVisibleMask | kOnlyInThirdPersonMask;
    }
}
