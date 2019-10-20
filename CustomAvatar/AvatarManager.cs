using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
	public class AvatarManager
	{
		private const string PreviousAvatarKey = "previousAvatar";
		private readonly string CustomAvatarsPath = Path.GetFullPath("CustomAvatars");

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
			var previousAvatarPath = PlayerPrefs.GetString(PreviousAvatarKey, null);

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
			ResizePlayerAvatar();
			OnFirstPersonEnabledChanged(Plugin.Instance.FirstPersonEnabled);

			PlayerPrefs.SetString(PreviousAvatarKey, avatar.FullPath);
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
			if (newScene.name.Equals("GameCore"))
				CurrentlySpawnedAvatar?.GameObject.GetComponentInChildren<AvatarEventsPlayer>()?.LevelStartedEvent();
			else if (newScene.name.Equals("MenuCore"))
				CurrentlySpawnedAvatar?.GameObject.GetComponentInChildren<AvatarEventsPlayer>()?.MenuEnteredEvent();
		}

		private static SpawnedAvatar SpawnAvatar(CustomAvatar customAvatar)
		{
			var avatarGameObject = Object.Instantiate(customAvatar.GameObject);

			avatarGameObject.AddComponent<AvatarBehaviour>();
			avatarGameObject.AddComponent<AvatarEventsPlayer>();

			Object.DontDestroyOnLoad(avatarGameObject);

			var spawnedAvatar = new SpawnedAvatar(customAvatar, avatarGameObject);

			return spawnedAvatar;
		}
	}
}
