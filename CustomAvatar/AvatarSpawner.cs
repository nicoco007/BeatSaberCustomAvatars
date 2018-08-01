using UnityEngine;

namespace CustomAvatar
{
	public static class AvatarSpawner
	{
		public static SpawnedAvatar SpawnAvatar(CustomAvatar customAvatar, IAvatarInput avatarInput)
		{
			if (customAvatar.GameObject == null)
			{
				Plugin.Log("Can't spawn " + customAvatar.FullPath + " because it hasn't been loaded!");
				return null;
			}

			var avatarGameObject = Object.Instantiate(customAvatar.GameObject);

			var behaviour = avatarGameObject.AddComponent<AvatarBehaviour>();
			behaviour.Init(avatarInput);
			
			Object.DontDestroyOnLoad(avatarGameObject);

			var spawnedAvatar = new SpawnedAvatar(customAvatar, avatarGameObject);
			
			return spawnedAvatar;
		}
	}
}