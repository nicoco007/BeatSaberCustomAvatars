using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
    public class SpawnedAvatar
    {
        public CustomAvatar CustomAvatar { get; }
        public GameObject GameObject { get; }
        public AvatarEventsPlayer EventsPlayer { get; }

        public SpawnedAvatar(CustomAvatar customAvatar)
        {
            CustomAvatar = customAvatar ?? throw new ArgumentNullException(nameof(customAvatar));
            GameObject = Object.Instantiate(customAvatar.GameObject);
            EventsPlayer = GameObject.AddComponent<AvatarEventsPlayer>();

            GameObject.AddComponent<AvatarBehaviour>();

            Object.DontDestroyOnLoad(GameObject);
        }
    }
}
