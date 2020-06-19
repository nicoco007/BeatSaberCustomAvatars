using CustomAvatar.Avatar;
using UnityEngine;

namespace CustomAvatar.UI
{
    internal class AvatarListItem
    {
        public readonly string name;
        public readonly string author;
        public readonly Texture2D icon;
        public readonly string fullPath;

        internal AvatarListItem(AvatarInfo avatar)
        {
            name = avatar.name;
            author = avatar.author;
            icon = avatar.icon;
            fullPath = avatar.fullPath;
        }

        internal AvatarListItem(string name, Texture2D icon)
        {
            this.name = name;
            this.icon = icon;
        }
    }
}
