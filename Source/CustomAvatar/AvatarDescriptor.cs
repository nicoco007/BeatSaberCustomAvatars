//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Logging;
using UnityEngine;

#if UNITY_EDITOR
using CustomAvatar.Utilities;
#else
using Zenject;
#endif

namespace CustomAvatar
{
    /// <summary>
    /// Container for an avatar's name and other information configured before exportation.
    /// </summary>
    public class AvatarDescriptor : MonoBehaviour, ISerializationCallbackReceiver
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
        /// Whether or not to allow height calibration for this avatar.
        /// </summary>
        [Tooltip("Whether or not to allow height calibration for this avatar.")]
        public bool allowHeightCalibration = true;

        /// <summary>
        /// Whether or not this avatar supports automatic calibration. Note that this requires specific setup of the waist and feet trackers.
        /// </summary>
        [Tooltip("Whether or not this avatar supports automatic calibration. Note that this requires specific setup of the waist and feet trackers.")]
        public bool supportsAutomaticCalibration = false;

        /// <summary>
        /// The image shown in the in-game avatars list.
        /// </summary>
        [Tooltip("The image shown in the in-game avatars list.")]
        public Sprite cover;

        // Legacy stuff
        #pragma warning disable 649
        [SerializeField] [HideInInspector] private readonly string AvatarName;
        [SerializeField] [HideInInspector] private readonly string AuthorName;
        [SerializeField] [HideInInspector] private readonly Sprite CoverImage;
        [SerializeField] [HideInInspector] private readonly string Name;
        [SerializeField] [HideInInspector] private readonly string Author;
        [SerializeField] [HideInInspector] private readonly Sprite Cover;
        #pragma warning restore 649

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            name = name ?? Name ?? AvatarName;
            author = author ?? Author ?? AuthorName;
            cover = cover ?? Cover ?? CoverImage;
        }

        #if UNITY_EDITOR
        public void Start()
        {
            IKHelper ikHelper = new IKHelper(new EditorLoggerProvider());
            ikHelper.InitializeVRIK(transform.GetComponentInChildren<VRIKManager>(), transform);
        }
        #else
        [Inject]
        private void Inject(ILoggerProvider loggerProvider)
        {
            ILogger<AvatarDescriptor> logger = loggerProvider.CreateLogger<AvatarDescriptor>(name);

            if (!string.IsNullOrEmpty(AvatarName) ||
                !string.IsNullOrEmpty(Name) ||
                !string.IsNullOrEmpty(AuthorName) ||
                !string.IsNullOrEmpty(Author) ||
                CoverImage ||
                Cover)
            {
                logger.Warning("Avatar is using a deprecated field; please re-export this avatar using the latest version of Custom Avatars");
            }
        }
        #endif
    }
}
