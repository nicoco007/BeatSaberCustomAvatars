using System;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
	public class PlayerAvatarManager
	{
		private readonly AvatarLoader _avatarLoader;
		private readonly AvatarTailor _avatarTailor;
		public SpawnedAvatar _currentSpawnedPlayerAvatar;

		public event Action<CustomAvatar> AvatarChanged;

		public PlayerAvatarManager(AvatarLoader avatarLoader, AvatarTailor avatarTailor, CustomAvatar startAvatar = null)
		{
			Console.WriteLine("For PlayerAvatarManager");
			_avatarLoader = avatarLoader;
			_avatarTailor = avatarTailor;

			if (startAvatar != null)
			{
				SwitchToAvatar(startAvatar);
			}

			Plugin.Instance.FirstPersonEnabledChanged += OnFirstPersonEnabledChanged;
			Plugin.Instance.SceneTransitioned += OnSceneTransitioned;
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		public CustomAvatar CurrentPlayerAvatar => _currentSpawnedPlayerAvatar?.CustomAvatar;

		~PlayerAvatarManager()
		{
			Plugin.Instance.FirstPersonEnabledChanged -= OnFirstPersonEnabledChanged;
			Plugin.Instance.SceneTransitioned -= OnSceneTransitioned;
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		public SpawnedAvatar GetSpawnedAvatar()
		{
			return _currentSpawnedPlayerAvatar;
		}

		public void SwitchToAvatar(CustomAvatar customAvatar)
		{
			if (customAvatar == null) return;
			if (CurrentPlayerAvatar == customAvatar) return;

			customAvatar.Load(OnCustomAvatarLoaded);
		}

		public CustomAvatar SwitchToNextAvatar()
		{
			var avatars = _avatarLoader.Avatars;
			if (avatars.Count == 0) return null;

			if (CurrentPlayerAvatar == null)
			{
				SwitchToAvatar(avatars[0]);
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
			SwitchToAvatar(nextAvatar);
			return nextAvatar;
		}

		public CustomAvatar SwitchToPreviousAvatar()
		{
			var avatars = _avatarLoader.Avatars;
			if (avatars.Count == 0) return null;

			if (CurrentPlayerAvatar == null)
			{
				SwitchToAvatar(avatars[0]);
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
			SwitchToAvatar(nextAvatar);
			return nextAvatar;
		}

		public void ResizePlayerAvatar()
		{
			if (_currentSpawnedPlayerAvatar?.GameObject == null) return;
			if (!_currentSpawnedPlayerAvatar.CustomAvatar.AllowHeightCalibration) return;

			_avatarTailor.ResizeAvatar(_currentSpawnedPlayerAvatar);
		}

		private void OnCustomAvatarLoaded(CustomAvatar loadedAvatar, AvatarLoadResult result)
		{
			if (result != AvatarLoadResult.Completed)
			{
				Plugin.Logger.Error("Avatar " + loadedAvatar.FullPath + " failed to load");
				return;
			}

			Plugin.Logger.Info("Loaded avatar " + loadedAvatar.Name + " by " + loadedAvatar.AuthorName);

			if (_currentSpawnedPlayerAvatar?.GameObject != null)
			{
				Object.Destroy(_currentSpawnedPlayerAvatar.GameObject);
			}

			_currentSpawnedPlayerAvatar = SpawnAvatar(loadedAvatar);

			AvatarChanged?.Invoke(loadedAvatar);

			_avatarTailor.OnAvatarLoaded(_currentSpawnedPlayerAvatar);
			ResizePlayerAvatar();
			OnFirstPersonEnabledChanged(Plugin.Instance.FirstPersonEnabled);
		}

		private void OnFirstPersonEnabledChanged(bool firstPersonEnabled)
		{
			if (_currentSpawnedPlayerAvatar == null) return;
			AvatarLayers.SetChildrenToLayer(_currentSpawnedPlayerAvatar.GameObject,
				firstPersonEnabled ? AvatarLayers.Global : AvatarLayers.OnlyInThirdPerson);
			foreach (var ex in _currentSpawnedPlayerAvatar.GameObject.GetComponentsInChildren<AvatarScriptPack.FirstPersonExclusion>())
				ex.OnFirstPersonEnabledChanged(firstPersonEnabled);
		}

		private void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
		{
			ResizePlayerAvatar();
			OnFirstPersonEnabledChanged(Plugin.Instance.FirstPersonEnabled);
			_currentSpawnedPlayerAvatar?.GameObject.GetComponentInChildren<AvatarEventsPlayer>()?.Restart();
		}

		private void OnSceneTransitioned(Scene newScene)
		{
			Plugin.Logger.Debug("OnSceneTransitioned - " + newScene.name);
			if (newScene.name.Equals("GameCore"))
				_currentSpawnedPlayerAvatar?.GameObject.GetComponentInChildren<AvatarEventsPlayer>()?.LevelStartedEvent();
			else if (newScene.name.Equals("MenuCore"))
				_currentSpawnedPlayerAvatar?.GameObject.GetComponentInChildren<AvatarEventsPlayer>()?.MenuEnteredEvent();
		}

		private static SpawnedAvatar SpawnAvatar(CustomAvatar customAvatar)
		{
			if (customAvatar.GameObject == null)
			{
				Plugin.Logger.Error("Can't spawn " + customAvatar.FullPath + " because it hasn't been loaded!");
				return null;
			}

			var avatarGameObject = Object.Instantiate(customAvatar.GameObject);

			var behaviour = avatarGameObject.AddComponent<AvatarBehaviour>();
			avatarGameObject.AddComponent<AvatarEventsPlayer>();

			/* Don't have the patience to make this work rn
			 
			var mainCamera = Camera.main;

			foreach (Camera cam in avatarGameObject.GetComponentsInChildren<Camera>())
			{
				if(mainCamera)
				{
					var newCamObj = Object.Instantiate(mainCamera, cam.transform);
					newCamObj.tag = "Untagged";
					while (newCamObj.transform.childCount > 0) Object.DestroyImmediate(newCamObj.transform.GetChild(0).gameObject);
					Object.DestroyImmediate(newCamObj.GetComponent("CameraRenderCallbacksManager"));
					Object.DestroyImmediate(newCamObj.GetComponent("AudioListener"));
					Object.DestroyImmediate(newCamObj.GetComponent("MeshCollider"));

					var newCam = newCamObj.GetComponent<Camera>();
					newCam.stereoTargetEye = StereoTargetEyeMask.None;
					newCam.cullingMask = cam.cullingMask;

					var _liv = newCam.GetComponent<LIV.SDK.Unity.LIV>();
					if (_liv)
						Object.Destroy(_liv);

					var _screenCamera = new GameObject("Screen Camera").AddComponent<ScreenCameraBehaviour>();

					if (_previewMaterial == null)
						_previewMaterial = new Material(Shader.Find("Hidden/BlitCopyWithDepth"));


					cam.enabled = false;
				}
			}
			*/

			Object.DontDestroyOnLoad(avatarGameObject);

			var spawnedAvatar = new SpawnedAvatar(customAvatar, avatarGameObject);

			return spawnedAvatar;
		}
	}
}
