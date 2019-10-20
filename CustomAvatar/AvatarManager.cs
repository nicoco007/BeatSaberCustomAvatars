using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomAvatar.Exceptions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
	public class AvatarManager
	{
		private const string CustomAvatarsPath = "CustomAvatars";
		private const string PreviousAvatarKey = "previousAvatar";

		public static AvatarManager Instance
		{
			get
			{
                if (instance == null)
				{
                    instance = new AvatarManager();
				}

                return instance;
			}
		}

		private static AvatarManager instance;
        
		public AvatarTailor AvatarTailor { get; }
		public SpawnedAvatar CurrentlySpawnedAvatar { get; private set; }

		public event Action<SpawnedAvatar> AvatarChanged;

		public List<CustomAvatar> Avatars { get; private set; }

		private AvatarManager()
		{
            AvatarTailor = new AvatarTailor();

            Plugin.Instance.FirstPersonEnabledChanged += OnFirstPersonEnabledChanged;
            Plugin.Instance.SceneTransitioned += OnSceneTransitioned;

            SceneManager.sceneLoaded += OnSceneLoaded;
		}

        ~AvatarManager()
		{
			Plugin.Instance.FirstPersonEnabledChanged -= OnFirstPersonEnabledChanged;
			Plugin.Instance.SceneTransitioned -= OnSceneTransitioned;

			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		public void LoadAvatars(bool forceReload = false)
		{
			if (Avatars == null || forceReload)
			{
				Avatars = new List<CustomAvatar>();

				Plugin.Logger.Info("Loading all Avatars");

				foreach (string filePath in Directory.GetFiles("CustomAvatars", "*.avatar"))
				{
					try
					{
						CustomAvatar avatar = CustomAvatar.FromFile(filePath);
						Plugin.Logger.Info($"Loaded avatar {avatar.Descriptor.Name} by {avatar.Descriptor.Author} from '{filePath}'");
						Avatars.Add(avatar);
					}
					catch (AvatarLoadException ex)
					{
						Plugin.Logger.Error($"Failed to load avatar at '{filePath}': {ex.Message}");
					}
				}
			}
		}

		public void LoadAvatarFromSettings()
		{
			if (Avatars.Count == 0)
			{
				Plugin.Logger.Warn("No custom Avatars found in path " + Path.GetFullPath(CustomAvatarsPath));
				return;
			}

			var previousAvatarPath = PlayerPrefs.GetString(PreviousAvatarKey, null);

			if (!File.Exists(previousAvatarPath))
			{
				previousAvatarPath = Avatars[0].FullPath;
			}

			var previousAvatar = Avatars.FirstOrDefault(x => x.FullPath == previousAvatarPath);

            SwitchToAvatar(previousAvatar);
		}

		public void SwitchToAvatar(CustomAvatar customAvatar)
		{
			if (customAvatar == null) return;
			if (CurrentlySpawnedAvatar?.CustomAvatar == customAvatar) return;

			if (CurrentlySpawnedAvatar?.GameObject != null)
			{
				Object.Destroy(CurrentlySpawnedAvatar.GameObject);
			}

			CurrentlySpawnedAvatar = SpawnAvatar(customAvatar);

			AvatarChanged?.Invoke(CurrentlySpawnedAvatar);

			AvatarTailor.OnAvatarLoaded(CurrentlySpawnedAvatar);
			ResizePlayerAvatar();
			OnFirstPersonEnabledChanged(Plugin.Instance.FirstPersonEnabled);

			PlayerPrefs.SetString(PreviousAvatarKey, customAvatar.FullPath);
		}

		public CustomAvatar SwitchToNextAvatar()
		{
			if (Avatars.Count == 0) return null;

			if (CurrentlySpawnedAvatar == null)
			{
				SwitchToAvatar(Avatars[0]);
				return Avatars[0];
			}

			var currentIndex = Avatars.IndexOf(CurrentlySpawnedAvatar.CustomAvatar);
			if (currentIndex < 0) currentIndex = 0;

			var nextIndex = currentIndex + 1;
			if (nextIndex >= Avatars.Count)
			{
				nextIndex = 0;
			}

			var nextAvatar = Avatars[nextIndex];
			SwitchToAvatar(nextAvatar);
			return nextAvatar;
		}

		public CustomAvatar SwitchToPreviousAvatar()
		{
			if (Avatars.Count == 0) return null;

			if (CurrentlySpawnedAvatar == null)
			{
				SwitchToAvatar(Avatars[0]);
				return Avatars[0];
			}

			var currentIndex = Avatars.IndexOf(CurrentlySpawnedAvatar.CustomAvatar);
			if (currentIndex < 0) currentIndex = 0;

			var nextIndex = currentIndex - 1;
			if (nextIndex < 0)
			{
				nextIndex = Avatars.Count - 1;
			}

			var nextAvatar = Avatars[nextIndex];
			SwitchToAvatar(nextAvatar);
			return nextAvatar;
		}

		public void ResizePlayerAvatar()
		{
			if (CurrentlySpawnedAvatar?.GameObject == null) return;
			if (CurrentlySpawnedAvatar?.CustomAvatar.Descriptor.AllowHeightCalibration != true) return;

			AvatarTailor.ResizeAvatar(CurrentlySpawnedAvatar);
		}

		private void OnFirstPersonEnabledChanged(bool firstPersonEnabled)
		{
			if (CurrentlySpawnedAvatar == null) return;
			AvatarLayers.SetChildrenToLayer(CurrentlySpawnedAvatar.GameObject,
				firstPersonEnabled ? AvatarLayers.Global : AvatarLayers.OnlyInThirdPerson);
			foreach (var ex in CurrentlySpawnedAvatar.GameObject.GetComponentsInChildren<AvatarScriptPack.FirstPersonExclusion>())
				ex.OnFirstPersonEnabledChanged(firstPersonEnabled);
		}

		private void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
		{
			ResizePlayerAvatar();
			OnFirstPersonEnabledChanged(Plugin.Instance.FirstPersonEnabled);
			CurrentlySpawnedAvatar?.GameObject.GetComponentInChildren<AvatarEventsPlayer>()?.Restart();
		}

		private void OnSceneTransitioned(Scene newScene)
		{
			Plugin.Logger.Debug("OnSceneTransitioned - " + newScene.name);
			if (newScene.name.Equals("GameCore"))
				CurrentlySpawnedAvatar?.GameObject.GetComponentInChildren<AvatarEventsPlayer>()?.LevelStartedEvent();
			else if (newScene.name.Equals("MenuCore"))
				CurrentlySpawnedAvatar?.GameObject.GetComponentInChildren<AvatarEventsPlayer>()?.MenuEnteredEvent();
		}

		private static SpawnedAvatar SpawnAvatar(CustomAvatar customAvatar)
		{
			if (customAvatar.GameObject == null)
			{
				Plugin.Logger.Error("Can't spawn " + customAvatar.FullPath + " because it hasn't been loaded!");
				return null;
			}

			var avatarGameObject = Object.Instantiate(customAvatar.GameObject);

			avatarGameObject.AddComponent<AvatarBehaviour>();
			avatarGameObject.AddComponent<AvatarEventsPlayer>();

			Object.DontDestroyOnLoad(avatarGameObject);

			var spawnedAvatar = new SpawnedAvatar(customAvatar, avatarGameObject);

			return spawnedAvatar;
		}
	}
}
