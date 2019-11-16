using System;
using CustomAvatar.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
	public class SpawnedAvatar
	{
		public CustomAvatar CustomAvatar { get; }
		public AvatarEventsPlayer EventsPlayer { get; }

		public Vector3 Position
		{
			get => gameObject.transform.position - initialPosition;
			set => gameObject.transform.position = initialPosition + value;
		}

		public float Scale
        {
	        get => gameObject.transform.localScale.y / initialScale.y;
	        set
	        {
		        gameObject.transform.localScale = initialScale * value;
		        Plugin.Logger.Info("Avatar resized with scale: " + value);
	        }
        }

        private readonly GameObject gameObject;
        private readonly Vector3 initialPosition;
        private readonly Vector3 initialScale;

        public SpawnedAvatar(CustomAvatar customAvatar)
        {
            CustomAvatar = customAvatar ?? throw new ArgumentNullException(nameof(customAvatar));
            gameObject = Object.Instantiate(customAvatar.GameObject);

            initialPosition = gameObject.transform.position;
            initialScale = gameObject.transform.localScale;

            EventsPlayer = gameObject.AddComponent<AvatarEventsPlayer>();
            gameObject.AddComponent<AvatarBehaviour>();

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
