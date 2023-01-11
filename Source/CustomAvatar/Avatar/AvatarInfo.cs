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

using System;
using System.IO;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    internal readonly struct AvatarInfo
    {
        /// <summary>
        /// Name of the avatar.
        /// </summary>
        public readonly string name;

        /// <summary>
        /// Avatar author's name.
        /// </summary>
        public readonly string author;

        /// <summary>
        /// Avatar icon.
        /// </summary>
        public readonly Sprite icon;

        /// <summary>
        /// File name of the avatar.
        /// </summary>
        public readonly string fileName;

        /// <summary>
        /// File size of the avatar.
        /// </summary>
        public readonly long fileSize;

        /// <summary>
        /// Date/time at which the avatar file was created.
        /// </summary>
        public readonly DateTime created;

        /// <summary>
        /// Date/time at which the avatar file was last modified.
        /// </summary>
        public readonly DateTime lastModified;

        /// <summary>
        /// Date/time at which this information was read from disk.
        /// </summary>
        public readonly DateTime timestamp;

        internal AvatarInfo(string name, string author, Texture2D icon, string fileName, long fileSize, DateTime created, DateTime lastModified, DateTime timestamp)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));

            this.name = name;
            this.author = author;
            this.icon = CreateSprite(icon);
            this.fileName = fileName;
            this.fileSize = fileSize;
            this.created = created;
            this.lastModified = lastModified;
            this.timestamp = timestamp;
        }

        public AvatarInfo(AvatarPrefab avatar)
        {
            name = avatar.descriptor.name ?? "Unknown";
            author = avatar.descriptor.author ?? "Unknown";
            icon = avatar.descriptor.cover ? avatar.descriptor.cover : null;

            var fileInfo = new FileInfo(avatar.fullPath);

            fileName = fileInfo.Name;
            fileSize = fileInfo.Length;
            created = fileInfo.CreationTimeUtc;
            lastModified = fileInfo.LastWriteTimeUtc;

            timestamp = DateTime.UtcNow;
        }

        public bool IsForFile(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            var fileInfo = new FileInfo(filePath);

            return fileName == fileInfo.Name && fileSize == fileInfo.Length && created == fileInfo.CreationTimeUtc && lastModified == fileInfo.LastWriteTimeUtc;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AvatarInfo other)) return false;

            return name == other.name && author == other.author && fileName == other.fileName && fileSize == other.fileSize && created == other.created && lastModified == other.lastModified && other.timestamp == timestamp;
        }

        public override int GetHashCode()
        {
            return (name, author, icon, fileName, fileSize, created, lastModified, timestamp).GetHashCode();
        }

        private static Sprite CreateSprite(Texture2D texture)
        {
            if (!texture) return null;

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
    }
}
