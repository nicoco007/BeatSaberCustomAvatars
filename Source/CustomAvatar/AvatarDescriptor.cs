//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using UnityEngine;

namespace CustomAvatar
{
    public class AvatarDescriptor : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Tooltip("Avatar's name")]
        public new string name;

        [Tooltip("Avatar creator's name")]
        public string author;

        [Tooltip("Whether or not to allow height calibration for this avatar.")]
        public bool allowHeightCalibration = true;

        [Tooltip("Whether or not this avatar supports automatic calibration. Note that this requires specific setup of the waist and feet trackers.")]
        public bool supportsAutomaticCalibration = false;

        [Tooltip("The image shown in the in-game avatars list")]
        public Sprite cover;

        // Legacy stuff
        #pragma warning disable 649
        [SerializeField] [HideInInspector] private string AvatarName;
        [SerializeField] [HideInInspector] private string AuthorName;
        [SerializeField] [HideInInspector] private Sprite CoverImage;
        [SerializeField] [HideInInspector] private string Name;
        [SerializeField] [HideInInspector] private string Author;
        [SerializeField] [HideInInspector] private Sprite Cover;
        #pragma warning restore 649

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            name = name ?? Name ?? AvatarName;
            author = author ?? Author ?? AuthorName;
            cover = cover ?? Cover ?? CoverImage;
        }
    }
}
