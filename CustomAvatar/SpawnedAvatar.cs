using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
	public class SpawnedAvatar
	{
		public CustomAvatar customAvatar { get; }
		public GameObject gameObject { get; }
        public AvatarEventsPlayer eventsPlayer { get; }

		public SpawnedAvatar(CustomAvatar customAvatar)
		{
			this.customAvatar = customAvatar ?? throw new ArgumentNullException(nameof(customAvatar));
			this.gameObject = Object.Instantiate(customAvatar.gameObject);
			this.eventsPlayer = this.gameObject.AddComponent<AvatarEventsPlayer>();

			this.gameObject.AddComponent<AvatarBehaviour>();

			Object.DontDestroyOnLoad(this.gameObject);
		}
	}
}
