using System;
using CustomAvatar.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
	public class SpawnedAvatar
	{
		public CustomAvatar CustomAvatar { get; }
		public AvatarBehaviour Behaviour { get; }
		public AvatarEventsPlayer EventsPlayer { get; }

        private readonly GameObject gameObject;

        public SpawnedAvatar(CustomAvatar customAvatar)
        {
            CustomAvatar = customAvatar ?? throw new ArgumentNullException(nameof(customAvatar));
            gameObject = Object.Instantiate(customAvatar.GameObject);

            EventsPlayer = gameObject.AddComponent<AvatarEventsPlayer>();
            Behaviour = gameObject.AddComponent<AvatarBehaviour>();

            Object.DontDestroyOnLoad(gameObject);
        }

        public void Destroy()
        {
	        Object.Destroy(gameObject);
        }

        public void OnFirstPersonEnabledChanged()
        {
	        SetChildrenToLayer(SettingsManager.Settings.IsAvatarVisibleInFirstPerson ? AvatarLayers.AlwaysVisible : AvatarLayers.OnlyInThirdPerson);
        }

        private void SetChildrenToLayer(int layer)
        {
	        foreach (var child in gameObject.GetComponentsInChildren<Transform>())
	        {
		        child.gameObject.layer = layer;
	        }
        }
    }
}
