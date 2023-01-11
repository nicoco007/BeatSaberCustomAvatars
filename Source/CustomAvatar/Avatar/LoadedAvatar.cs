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

extern alias BeatSaberFinalIK;

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar.Avatar
{
    /// <summary>
    /// Contains static information about an avatar. 
    /// </summary>
    [Obsolete("Use AvatarPrefab instead")]
    public class LoadedAvatar : IDisposable
    {
        /// <summary>
        /// The name of the file from which the avatar was loaded.
        /// </summary>
        public readonly string fileName;

        /// <summary>
        /// The full path of the file from which the avatar was loaded.
        /// </summary>
        public readonly string fullPath;

        /// <summary>
        /// The avatar prefab.
        /// </summary>
        public readonly GameObject prefab;

        /// <summary>
        /// The <see cref="AvatarDescriptor"/> retrieved from the root object on the prefab.
        /// </summary>
        public readonly AvatarDescriptor descriptor;

        /// <summary>
        /// Whether or not this avatar has IK.
        /// </summary>
        public readonly bool isIKAvatar;

        /// <summary>
        /// Whether or not this avatar has one or more full body (pelvis/feet) tracking points
        /// </summary>
        [Obsolete("This will always be true")]
        public readonly bool supportsFullBodyTracking = true;

        /// <summary>
        /// Whether or not this avatar supports finger tracking.
        /// </summary>
        public readonly bool supportsFingerTracking;

        /// <summary>
        /// The avatar's eye height.
        /// </summary>
        public readonly float eyeHeight;

        /// <summary>
        /// The avatar's estimated arm span.
        /// </summary>
        public readonly float armSpan;

        internal LoadedAvatar(AvatarPrefab prefab)
        {
            this.prefab = prefab.gameObject;
            fileName = prefab.fileName;
            fullPath = prefab.fullPath;
            descriptor = prefab.descriptor;
            isIKAvatar = prefab.isIKAvatar;
            supportsFingerTracking = prefab.supportsFingerTracking;
            eyeHeight = prefab.eyeHeight;
            armSpan = prefab.armSpan;
        }

        public void Dispose()
        {
            Object.Destroy(prefab);
        }
    }
}
