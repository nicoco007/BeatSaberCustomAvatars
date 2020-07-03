using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    internal class AvatarInfo
    {
        [JsonProperty] public readonly string name;
        [JsonProperty] public readonly string author;
        [JsonProperty] public readonly Texture2D icon;
        [JsonProperty] public readonly string fileName;
        [JsonProperty] public readonly long size;
        [JsonProperty] public readonly DateTime created;
        [JsonProperty] public readonly DateTime modified;

        [JsonIgnore] public bool isValid => !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(author) && icon && !string.IsNullOrEmpty(fileName) && size > 0 && !created.Equals(default) && !modified.Equals(default);

        [JsonConstructor]
        private AvatarInfo() { }

        public AvatarInfo(LoadedAvatar avatar)
        {
            name = avatar.descriptor.name;
            author = avatar.descriptor.author;
            icon = avatar.descriptor.cover ? avatar.descriptor.cover.texture : null;

            var fileInfo = new FileInfo(avatar.fullPath);

            fileName = fileInfo.Name;
            size = fileInfo.Length;
            created = fileInfo.CreationTimeUtc;
            modified = fileInfo.LastWriteTimeUtc;
        }

        public static bool operator ==(AvatarInfo left, AvatarInfo right)
        {
            if (ReferenceEquals(left, null)) return false;

            return left.Equals(right);
        }

        public static bool operator !=(AvatarInfo left, AvatarInfo right)
        {
            if (ReferenceEquals(left, null)) return false;

            return !left.Equals(right);
        }

        public bool IsForFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);

            return fileName == fileInfo.Name && size == fileInfo.Length && created == fileInfo.CreationTimeUtc && modified == fileInfo.LastWriteTimeUtc;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AvatarInfo other)) return false;

            return name == other.name && author == other.author && fileName == other.fileName && size == other.size && created == other.created && modified == other.modified;
        }

        public override int GetHashCode()
        {
            int hash = 23;

            var fields = new object[] { name, author, icon, fileName, size, created, modified };

            unchecked
            {
                foreach (object field in fields)
                {
                    if (field == null) continue;

                    hash = hash * 17 + field.GetHashCode();
                }
            }

            return hash;
        }
    }
}
