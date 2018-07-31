using UnityEngine;

namespace CustomAvatar
{
	public class CustomAvatarSpawner
	{
		private GameObject _spawnedAvatar;

		public GameObject SpawnAvatar(IAvatarInput avatarInput, CustomAvatar customAvatar)
		{
			if (customAvatar.GameObject == null)
			{
				Plugin.Log("Can't spawn an avatar that hasn't been loaded!");
				return null;
			}

			if (_spawnedAvatar != null)
			{
				Object.Destroy(_spawnedAvatar);
			}

			_spawnedAvatar = Object.Instantiate(customAvatar.GameObject);

			var behaviour = _spawnedAvatar.AddComponent<AvatarBehaviour>();
			behaviour.Init(avatarInput);
			
			Object.DontDestroyOnLoad(_spawnedAvatar);

			return _spawnedAvatar;
		}
	}
}