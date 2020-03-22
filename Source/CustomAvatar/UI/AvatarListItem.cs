using CustomAvatar.Avatar;
using UnityEngine;

namespace CustomAvatar.UI
{
    internal class AvatarListItem
    {
        public string name;
        public string author;
        public Texture2D icon;
        public LoadedAvatar avatar;

        internal AvatarListItem(LoadedAvatar avatar)
        {
            name = avatar.descriptor.name;
            author = avatar.descriptor.author;
            icon = avatar.descriptor.cover?.texture;
            this.avatar = avatar;
        }

        internal AvatarListItem(string name, Texture2D icon)
        {
            this.name = name;
            this.icon = icon;
        }
    }
}
