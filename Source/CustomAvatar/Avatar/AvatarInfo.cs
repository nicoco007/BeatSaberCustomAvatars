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

using System;
using System.IO;
using ProtoBuf;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    [ProtoContract]
    internal readonly struct AvatarInfo
    {
        /// <summary>
        /// Name of the avatar.
        /// </summary>
        [ProtoMember(1)]
        public string name { get; }

        /// <summary>
        /// Avatar author's name.
        /// </summary>
        [ProtoMember(2)]
        public string author { get; }

        /// <summary>
        /// Avatar icon.
        /// </summary>
        [ProtoMember(3)]
        public Sprite icon { get; }

        /// <summary>
        /// File name of the avatar.
        /// </summary>
        [ProtoMember(4)]
        public string fileName { get; }

        /// <summary>
        /// File size of the avatar.
        /// </summary>
        [ProtoMember(5)]
        public long fileSize { get; }

        /// <summary>
        /// Date/time at which the avatar file was created.
        /// </summary>
        [ProtoMember(6)]
        public DateTime created { get; }

        /// <summary>
        /// Date/time at which the avatar file was last modified.
        /// </summary>
        [ProtoMember(7)]
        public DateTime lastModified { get; }

        /// <summary>
        /// Date/time at which this information was read from disk.
        /// </summary>
        [ProtoMember(8)]
        public DateTime timestamp { get; }

        public AvatarInfo(AvatarPrefab avatar, string fullPath)
        {
            name = avatar.descriptor.name ?? "Unknown";
            author = avatar.descriptor.author ?? "Unknown";
            icon = avatar.descriptor.cover ? avatar.descriptor.cover : null;

            // TODO: this should probably be created and stored in AvatarPrefab when the avatar is loaded
            FileInfo fileInfo = new(fullPath);

            fileName = fileInfo.Name;
            fileSize = fileInfo.Length;
            created = fileInfo.CreationTimeUtc;
            lastModified = fileInfo.LastWriteTimeUtc;

            timestamp = DateTime.UtcNow;
        }

        public bool IsForFile(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            FileInfo fileInfo = new(filePath);

            return fileName == fileInfo.Name && fileSize == fileInfo.Length && created == fileInfo.CreationTimeUtc && lastModified == fileInfo.LastWriteTimeUtc;
        }

        public override bool Equals(object obj)
        {
            if (obj is not AvatarInfo other) return false;

            return name == other.name && author == other.author && fileName == other.fileName && fileSize == other.fileSize && created == other.created && lastModified == other.lastModified && other.timestamp == timestamp;
        }

        public override string ToString()
        {
            return $"{nameof(AvatarInfo)}{{{nameof(name)}={name}, {nameof(author)}={author}, {nameof(fileName)}={fileName}}}";
        }

        public override int GetHashCode()
        {
            return (name, author, icon, fileName, fileSize, created, lastModified, timestamp).GetHashCode();
        }
    }
}
