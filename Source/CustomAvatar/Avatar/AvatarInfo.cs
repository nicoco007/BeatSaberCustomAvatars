using System;
using System.IO;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    internal struct AvatarInfo
    {
        internal string name { get; }
        internal string author { get; }
        internal Texture2D icon { get; }
        internal string fullPath { get; }
        internal DateTime created { get; }
        internal DateTime modified { get; }

        internal AvatarInfo(LoadedAvatar avatar)
        {
            name = avatar.descriptor.name;
            author = avatar.descriptor.author;
            icon = avatar.descriptor.cover ? avatar.descriptor.cover.texture : Texture2D.blackTexture;
            fullPath = avatar.fullPath;

            var fileInfo = new FileInfo(fullPath);

            created = fileInfo.CreationTimeUtc;
            modified = fileInfo.LastWriteTimeUtc;
        }

        public static bool operator ==(AvatarInfo left, AvatarInfo right)
        {
            return left.name == right.name && left.author == right.author && left.fullPath == right.fullPath && left.created == right.created && left.modified == right.modified;
        }

        public static bool operator !=(AvatarInfo left, AvatarInfo right)
        {
            return left.name != right.name || left.author != right.author || left.fullPath != right.fullPath || left.created != right.created || left.modified != right.modified;
        }
    }
}
