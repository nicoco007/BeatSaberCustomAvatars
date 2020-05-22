using System;
using CustomAvatar.Exceptions;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    public class LoadedAvatar
    {
        public string fullPath { get; }
        public GameObject prefab { get; }
        public AvatarDescriptor descriptor { get; }

        internal LoadedAvatar(string fullPath, GameObject avatarGameObject)
        {
            this.fullPath = fullPath ?? throw new ArgumentNullException(nameof(avatarGameObject));
            prefab = avatarGameObject ? avatarGameObject : throw new ArgumentNullException(nameof(avatarGameObject));
            descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");
        }
    }
}
