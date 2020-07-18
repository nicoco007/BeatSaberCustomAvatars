using System;
using System.IO;
using AvatarScriptPack;
using CustomAvatar.Exceptions;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    public class LoadedAvatar
    {
        public readonly string fileName;
        public readonly string fullPath;
        public readonly GameObject prefab;
        public readonly AvatarDescriptor descriptor;

        public bool isIKAvatar { get; }

        internal LoadedAvatar(string fullPath, GameObject avatarGameObject)
        {
            this.fullPath = fullPath ?? throw new ArgumentNullException(nameof(avatarGameObject));
            prefab = avatarGameObject ? avatarGameObject : throw new ArgumentNullException(nameof(avatarGameObject));
            descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");

            fileName = Path.GetFileName(fullPath);

            #pragma warning disable CS0618
            isIKAvatar = prefab.GetComponentInChildren<IKManager>() || prefab.GetComponentInChildren<VRIKManager>();
            #pragma warning restore CS0618
        }
    }
}
