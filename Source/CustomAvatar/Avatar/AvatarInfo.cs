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
            if (left == null || right == null) return !(left == null ^ right == null);

            return left.name == right.name && left.author == right.author && left.fullPath == right.fullPath && left.size == right.size && left.created == right.created && left.modified == right.modified;
        }

        public static bool operator !=(AvatarInfo left, AvatarInfo right)
        {
            if (left == null || right == null) return left == null ^ right == null;

            return left.name != right.name || left.author != right.author || left.fullPath != right.fullPath || left.size != right.size || left.created != right.created || left.modified != right.modified;
        }
    }
}
