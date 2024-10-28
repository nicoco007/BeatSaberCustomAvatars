//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using UnityEngine;
using System.Linq;

// keeping root namespace for compatibility
namespace CustomAvatar
{
    /// <summary>
    /// Container for an avatar's name and other information configured before exportation.
    /// </summary>
    [DisallowMultipleComponent]
    public partial class AvatarDescriptor : MonoBehaviour, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Avatar's name.
        /// </summary>
        [Tooltip("Avatar's name.")]
        public new string name;

        /// <summary>
        /// Avatar creator's name.
        /// </summary>
        [Tooltip("Avatar creator's name.")]
        public string author;

        /// <summary>
        /// The image shown in the in-game avatars list.
        /// </summary>
        [Tooltip("The image shown in the in-game avatars list.")]
        public Sprite cover;

        // Legacy stuff
#pragma warning disable CS0649, IDE0044, IDE1006, IDE0055
        [SerializeField] [HideInInspector] private string AvatarName;
        [SerializeField] [HideInInspector] private string AuthorName;
        [SerializeField] [HideInInspector] private Sprite CoverImage;
        [SerializeField] [HideInInspector] private string Name;
        [SerializeField] [HideInInspector] private string Author;
        [SerializeField] [HideInInspector] private Sprite Cover;
#pragma warning restore CS0649, IDE0044, IDE1006, IDE0055

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            name ??= Name ?? AvatarName;
            author ??= Author ?? AuthorName;
            cover = FirstNonNullUnityObject(cover, Cover, CoverImage);

            Name = AvatarName = null;
            Author = AuthorName = null;
            Cover = CoverImage = null;
        }

        // Editor calls DoesObjectWithInstanceIDExist which doesn't exist at run time and blows up if not on the main thread, so just check the cached pointer.
        private T FirstNonNullUnityObject<T>(params T[] objects) where T : Object => objects.FirstOrDefault(o => o is not null && o.GetCachedPtr() != System.IntPtr.Zero);
    }
}
