using System;
using UnityEngine;

namespace CustomAvatar
{
	public class SpawnedAvatar
	{
		public SpawnedAvatar(CustomAvatar customAvatar, GameObject gameObject)
		{
			CustomAvatar = customAvatar ?? throw new ArgumentNullException(nameof(customAvatar));
			GameObject = gameObject ?? throw new ArgumentNullException(nameof(gameObject));
		}
		
		public CustomAvatar CustomAvatar { get; }
		public GameObject GameObject { get; }
	}
}
