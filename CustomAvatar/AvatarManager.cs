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
		public static readonly string kCustomAvatarsPath = Path.GetFullPath("CustomAvatars");

		public static AvatarManager Instance
		{
			get
			{
                if (_instance == null)
				{
                    _instance = new AvatarManager();
				}

                return _instance;
			}
		}

		private static AvatarManager _instance;
        
		public AvatarTailor AvatarTailor { get; }
		public SpawnedAvatar CurrentlySpawnedAvatar { get; private set; }

		public event Action<SpawnedAvatar> avatarChanged;

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
			Plugin.Logger.Info("Loading all avatars from " + kCustomAvatarsPath);

			foreach (string filePath in Directory.GetFiles(kCustomAvatarsPath, "*.avatar"))
			{
				SharedCoroutineStarter.instance.StartCoroutine(CustomAvatar.FromFileCoroutine(filePath, success, error));
			}
		}

		public void LoadAvatarFromSettingsAsync()
		{
			var previousAvatarPath = Settings.previousAvatarPath;

			if (!File.Exists(previousAvatarPath))
			{
				previousAvatarPath = Directory.GetFiles(kCustomAvatarsPath, "*.avatar").FirstOrDefault();
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
			if (CurrentlySpawnedAvatar?.customAvatar == avatar) return;

			if (CurrentlySpawnedAvatar?.gameObject != null)
			{
				Object.Destroy(CurrentlySpawnedAvatar.gameObject);
			}

			CurrentlySpawnedAvatar = SpawnAvatar(avatar);

			avatarChanged?.Invoke(CurrentlySpawnedAvatar);

			AvatarTailor.OnAvatarLoaded(CurrentlySpawnedAvatar);
			ResizeCurrentAvatar();
			OnFirstPersonEnabledChanged();

			Settings.previousAvatarPath = avatar.fullPath;
		}

		public void SwitchToNextAvatar()
		{
			string[] files = Directory.GetFiles(kCustomAvatarsPath, "*.avatar");
			int index = Array.IndexOf(files, CurrentlySpawnedAvatar.customAvatar.fullPath);

			index = (index + 1) % files.Length;

			SwitchToAvatarAsync(files[index]);
		}

		public void SwitchToPreviousAvatar()
		{
			string[] files = Directory.GetFiles(kCustomAvatarsPath, "*.avatar");
			int index = Array.IndexOf(files, CurrentlySpawnedAvatar.customAvatar.fullPath);

			index = (index + files.Length - 1) % files.Length;

			SwitchToAvatarAsync(files[index]);
		}

		public void ResizeCurrentAvatar()
		{
			if (CurrentlySpawnedAvatar?.gameObject == null) return;
			if (CurrentlySpawnedAvatar?.customAvatar.descriptor.AllowHeightCalibration != true) return;

			AvatarTailor.ResizeAvatar(CurrentlySpawnedAvatar);
		}

		public void OnFirstPersonEnabledChanged()
		{
			if (CurrentlySpawnedAvatar == null) return;
			AvatarLayers.SetChildrenToLayer(CurrentlySpawnedAvatar.gameObject,
				Settings.isAvatarVisibleInFirstPerson ? AvatarLayers.AlwaysVisible : AvatarLayers.OnlyInThirdPerson);
			foreach (var ex in CurrentlySpawnedAvatar.gameObject.GetComponentsInChildren<AvatarScriptPack.FirstPersonExclusion>())
				ex.OnFirstPersonEnabledChanged();
		}

		private void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
		{
			ResizeCurrentAvatar();
			OnFirstPersonEnabledChanged();
			CurrentlySpawnedAvatar?.eventsPlayer?.Restart();
		}

		private void OnSceneTransitioned(Scene newScene)
		{
			if (newScene.name.Equals("GameCore"))
				CurrentlySpawnedAvatar?.eventsPlayer?.LevelStartedEvent();
			else if (newScene.name.Equals("MenuCore"))
				CurrentlySpawnedAvatar?.eventsPlayer.MenuEnteredEvent();
		}

		private static SpawnedAvatar SpawnAvatar(CustomAvatar customAvatar)
		{
			return new SpawnedAvatar(customAvatar);
		}
	}
}
