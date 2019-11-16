using System;
using System.IO;
using System.Linq;
using CustomAvatar.Utilities;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
	public class AvatarManager
	{
		public static readonly string CustomAvatarsPath = Path.GetFullPath("CustomAvatars");

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

		private AvatarManager()
		{
            AvatarTailor = new AvatarTailor();

            Plugin.Instance.SceneTransitioned += OnSceneTransitioned;

            SceneManager.sceneLoaded += OnSceneLoaded;
		}

        ~AvatarManager()
		{
			Plugin.Instance.SceneTransitioned -= OnSceneTransitioned;

			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		public void GetAvatarsAsync(Action<CustomAvatar> success, Action<Exception> error)
		{
			Plugin.Logger.Info("Loading all avatars from " + CustomAvatarsPath);

			foreach (string filePath in Directory.GetFiles(CustomAvatarsPath, "*.avatar"))
			{
				SharedCoroutineStarter.instance.StartCoroutine(CustomAvatar.FromFileCoroutine(filePath, success, error));
			}
		}

		public void LoadAvatarFromSettingsAsync()
		{
			var previousAvatarPath = SettingsManager.Settings.PreviousAvatarPath;

			if (!File.Exists(previousAvatarPath))
			{
				previousAvatarPath = Directory.GetFiles(CustomAvatarsPath, "*.avatar").FirstOrDefault();
			}

			if (string.IsNullOrEmpty(previousAvatarPath))
			{
				Plugin.Logger.Info("No avatars found");
				return;
			}

			SwitchToAvatarAsync(previousAvatarPath);
		}

		public void SwitchToAvatarAsync(string filePath)
		{
			SharedCoroutineStarter.instance.StartCoroutine(CustomAvatar.FromFileCoroutine(filePath, avatar =>
			{
				SwitchToAvatar(avatar);
			}, ex =>
			{
				Plugin.Logger.Error("Failed to load avatar: " + ex.Message);
			}));
		}

		public void SwitchToAvatar(CustomAvatar avatar)
		{
			if (avatar == null) return;
			if (CurrentlySpawnedAvatar?.CustomAvatar == avatar) return;

			if (CurrentlySpawnedAvatar?.GameObject != null)
			{
				Object.Destroy(CurrentlySpawnedAvatar.GameObject);
			}

			CurrentlySpawnedAvatar = SpawnAvatar(avatar);

			AvatarChanged?.Invoke(CurrentlySpawnedAvatar);

			AvatarTailor.OnAvatarLoaded(CurrentlySpawnedAvatar);
			ResizeCurrentAvatar();
			OnFirstPersonEnabledChanged();

			SettingsManager.Settings.PreviousAvatarPath = avatar.FullPath;
		}

		public void SwitchToNextAvatar()
		{
			string[] files = Directory.GetFiles(CustomAvatarsPath, "*.avatar");
			int index = Array.IndexOf(files, CurrentlySpawnedAvatar.CustomAvatar.FullPath);

			index = (index + 1) % files.Length;

			SwitchToAvatarAsync(files[index]);
		}

		public void SwitchToPreviousAvatar()
		{
			string[] files = Directory.GetFiles(CustomAvatarsPath, "*.avatar");
			int index = Array.IndexOf(files, CurrentlySpawnedAvatar.CustomAvatar.FullPath);

			index = (index + files.Length - 1) % files.Length;

			SwitchToAvatarAsync(files[index]);
		}

		public void ResizeCurrentAvatar()
		{
			if (CurrentlySpawnedAvatar?.GameObject == null) return;
			if (CurrentlySpawnedAvatar?.CustomAvatar.Descriptor.AllowHeightCalibration != true) return;

			AvatarTailor.ResizeAvatar(CurrentlySpawnedAvatar);
		}

		public void OnFirstPersonEnabledChanged()
		{
			if (CurrentlySpawnedAvatar == null) return;
			AvatarLayers.SetChildrenToLayer(CurrentlySpawnedAvatar.GameObject,
				SettingsManager.Settings.IsAvatarVisibleInFirstPerson ? AvatarLayers.AlwaysVisible : AvatarLayers.OnlyInThirdPerson);
			foreach (var ex in CurrentlySpawnedAvatar.GameObject.GetComponentsInChildren<AvatarScriptPack.FirstPersonExclusion>())
				ex.OnFirstPersonEnabledChanged();
		}

		private void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
		{
			ResizeCurrentAvatar();
			OnFirstPersonEnabledChanged();
			CurrentlySpawnedAvatar?.EventsPlayer?.Restart();
		}

		private void OnSceneTransitioned(Scene newScene)
		{
			if (newScene.name.Equals("GameCore"))
				CurrentlySpawnedAvatar?.EventsPlayer?.LevelStartedEvent();
			else if (newScene.name.Equals("MenuCore"))
				CurrentlySpawnedAvatar?.EventsPlayer.MenuEnteredEvent();
		}

		private static SpawnedAvatar SpawnAvatar(CustomAvatar customAvatar)
		{
			return new SpawnedAvatar(customAvatar);
		}
	}
}
