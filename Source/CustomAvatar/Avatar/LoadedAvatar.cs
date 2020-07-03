using System;
using AvatarScriptPack;
using CustomAvatar.Exceptions;
using UnityEngine;

namespace CustomAvatar.Avatar
{
    public class LoadedAvatar
    {
        public string fullPath { get; }
        public GameObject prefab { get; }
        public AvatarDescriptor descriptor { get; }

        public bool isIKAvatar { get; }

        internal LoadedAvatar(string fullPath, GameObject avatarGameObject)
        {
            this.fullPath = fullPath ?? throw new ArgumentNullException(nameof(avatarGameObject));
            prefab = avatarGameObject ? avatarGameObject : throw new ArgumentNullException(nameof(avatarGameObject));
            descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");

            #pragma warning disable CS0618
            isIKAvatar = prefab.GetComponentInChildren<IKManager>() || prefab.GetComponentInChildren<VRIKManager>();
            #pragma warning restore CS0618
        }
    }
}
