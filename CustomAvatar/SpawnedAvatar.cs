using UnityEngine;

namespace CustomAvatar
{
	public class SpawnedAvatar
	{
		public SpawnedAvatar(CustomAvatar customAvatar, GameObject gameObject)
		{
			CustomAvatar = customAvatar;
			GameObject = gameObject;
		}
		
		public CustomAvatar CustomAvatar { get; }
		public GameObject GameObject { get; }
	}
}