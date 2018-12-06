using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
	public class PlayerAvatarManager
	{
		private readonly AvatarLoader _avatarLoader;
		private readonly PlayerAvatarInput _playerAvatarInput;
		private SpawnedAvatar _currentSpawnedPlayerAvatar;
		private float _prevPlayerHeight = MainSettingsModel.kDefaultPlayerHeight;
		private Vector3 _startAvatarLocalScale = Vector3.one;

		public event Action<CustomAvatar> AvatarChanged;

		private CustomAvatar CurrentPlayerAvatar
		{
			get { return _currentSpawnedPlayerAvatar?.CustomAvatar; }
			set
			{
				if (value == null) return;
				if (CurrentPlayerAvatar == value) return;
				value.Load(CustomAvatarLoaded);
			}
		}

		public PlayerAvatarManager(AvatarLoader avatarLoader, CustomAvatar startAvatar = null)
		{
			_playerAvatarInput = new PlayerAvatarInput();
			_avatarLoader = avatarLoader;

			if (startAvatar != null)
			{
				CurrentPlayerAvatar = startAvatar;
			}

			Plugin.Instance.FirstPersonEnabledChanged += OnFirstPersonEnabledChanged;
			SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
		}

		~PlayerAvatarManager()
		{
			Plugin.Instance.FirstPersonEnabledChanged -= OnFirstPersonEnabledChanged;
			SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;
		}

		public CustomAvatar GetCurrentAvatar()
		{
			return CurrentPlayerAvatar;
		}

		public void SwitchToAvatar(CustomAvatar customAvatar)
		{
			CurrentPlayerAvatar = customAvatar;
		}

		public CustomAvatar SwitchToNextAvatar()
		{
			var avatars = _avatarLoader.Avatars;
			if (avatars.Count == 0) return null;

			if (CurrentPlayerAvatar == null)
			{
				CurrentPlayerAvatar = avatars[0];
				return avatars[0];
			}

			var currentIndex = _avatarLoader.IndexOf(CurrentPlayerAvatar);
			if (currentIndex < 0) currentIndex = 0;

			var nextIndex = currentIndex + 1;
			if (nextIndex >= avatars.Count)
			{
				nextIndex = 0;
			}

			var nextAvatar = avatars[nextIndex];
			CurrentPlayerAvatar = nextAvatar;
			return nextAvatar;
		}

		public CustomAvatar SwitchToPreviousAvatar()
		{
			var avatars = _avatarLoader.Avatars;
			if (avatars.Count == 0) return null;

			if (CurrentPlayerAvatar == null)
			{
				CurrentPlayerAvatar = avatars[0];
				return avatars[0];
			}

			var currentIndex = _avatarLoader.IndexOf(CurrentPlayerAvatar);
			if (currentIndex < 0) currentIndex = 0;

			var nextIndex = currentIndex - 1;
			if (nextIndex < 0)
			{
				nextIndex = avatars.Count - 1;
			}

			var nextAvatar = avatars[nextIndex];
			CurrentPlayerAvatar = nextAvatar;
			return nextAvatar;
		}

		private void CustomAvatarLoaded(CustomAvatar loadedAvatar, AvatarLoadResult result)
		{
			if (result != AvatarLoadResult.Completed)
			{
				Plugin.Log("Avatar " + loadedAvatar.FullPath + " failed to load");
				return;
			}

			Plugin.Log("Loaded avatar " + loadedAvatar.Name + " by " + loadedAvatar.AuthorName);

			if (_currentSpawnedPlayerAvatar?.GameObject != null)
			{
				Object.Destroy(_currentSpawnedPlayerAvatar.GameObject);
			}

			_currentSpawnedPlayerAvatar = AvatarSpawner.SpawnAvatar(loadedAvatar, _playerAvatarInput);

			if (AvatarChanged != null)
			{
				AvatarChanged(loadedAvatar);
			}

			_startAvatarLocalScale = _currentSpawnedPlayerAvatar.GameObject.transform.localScale;
			_prevPlayerHeight = -1;
			ResizePlayerAvatar();
			OnFirstPersonEnabledChanged(Plugin.Instance.FirstPersonEnabled);
		}

		private void OnFirstPersonEnabledChanged(bool firstPersonEnabled)
		{
			if (_currentSpawnedPlayerAvatar == null) return;
			AvatarLayers.SetChildrenToLayer(_currentSpawnedPlayerAvatar.GameObject,
				firstPersonEnabled ? 0 : AvatarLayers.OnlyInThirdPerson);
			foreach (var ex in _currentSpawnedPlayerAvatar.GameObject.GetComponentsInChildren<AvatarScriptPack.FirstPersonExclusion>())
				ex.OnFirstPersonEnabledChanged(firstPersonEnabled);
		}

		private void SceneManagerOnSceneLoaded(Scene newScene, LoadSceneMode mode)
		{
			ResizePlayerAvatar();
			OnFirstPersonEnabledChanged(Plugin.Instance.FirstPersonEnabled);
            _currentSpawnedPlayerAvatar?.GameObject.GetComponentInChildren<AvatarEventsPlayer>()?.Restart();
        }

		public void OnSceneTransitioned(Scene newScene)
		{
			Plugin.Log("OnSceneTransitioned - " + newScene.name);
			if (newScene.name.Equals("GameCore"))
				_currentSpawnedPlayerAvatar?.GameObject.GetComponentInChildren<AvatarEventsPlayer>()?.LevelStartedEvent();
			else if (newScene.name.Equals("Menu"))
				_currentSpawnedPlayerAvatar?.GameObject.GetComponentInChildren<AvatarEventsPlayer>()?.MenuEnteredEvent();
		}

		private void ResizePlayerAvatar()
		{
			if (_currentSpawnedPlayerAvatar?.GameObject == null) return;
			if (!_currentSpawnedPlayerAvatar.CustomAvatar.AllowHeightCalibration) return;

			var playerHeight = BeatSaberUtil.GetPlayerHeight();
			if (playerHeight == _prevPlayerHeight) return;
			_prevPlayerHeight = playerHeight;
			_currentSpawnedPlayerAvatar.GameObject.transform.localScale =
				_startAvatarLocalScale * (playerHeight / _currentSpawnedPlayerAvatar.CustomAvatar.Height);
			Plugin.Log("Resizing avatar to " + (playerHeight / _currentSpawnedPlayerAvatar.CustomAvatar.Height) +
			                  "x scale");
		}
	}
}
