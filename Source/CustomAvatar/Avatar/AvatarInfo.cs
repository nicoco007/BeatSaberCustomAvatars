using System;
using System.IO;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    internal class AvatarInfo
    {
        internal readonly string name;
        internal readonly string author;
        internal readonly Texture2D icon;
        internal readonly string fullPath;
        internal readonly long size;
        internal readonly DateTime created;
        internal readonly DateTime modified;

        internal AvatarInfo(LoadedAvatar avatar)
        {
            name = avatar.descriptor.name;
            author = avatar.descriptor.author;
            icon = avatar.descriptor.cover ? avatar.descriptor.cover.texture : null;
            fullPath = avatar.fullPath;

            var fileInfo = new FileInfo(fullPath);

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

        public override bool Equals(object obj)
        {
            if (!(obj is AvatarInfo other)) return false;

            return name == other.name && author == other.author && fullPath == other.fullPath && size == other.size && created == other.created && modified == other.modified;
        }

        public override int GetHashCode()
        {
            int hash = 23;

            unchecked
            {
                if (name != null)     hash = hash * 17 + name.GetHashCode();
                if (author != null)   hash = hash * 17 + author.GetHashCode();
                if (icon != null)     hash = hash * 17 + icon.GetHashCode();
                if (fullPath != null) hash = hash * 17 + fullPath.GetHashCode();

                hash = hash * 17 + size.GetHashCode();
                hash = hash * 17 + created.GetHashCode();
                hash = hash * 17 + modified.GetHashCode();
            }

            return hash;
        }
    }
}
