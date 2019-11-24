using System;
using System.IO;
using System.Linq;
using CustomAvatar.Utilities;
using UnityEngine.SceneManagement;

namespace CustomAvatar
{
    public class AvatarManager
    {
        public static readonly string kCustomAvatarsPath = Path.GetFullPath("CustomAvatars");

        public static AvatarManager instance
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
        
        public AvatarTailor avatarTailor { get; }
        public SpawnedAvatar currentlySpawnedAvatar { get; private set; }

        public event Action<SpawnedAvatar> avatarChanged;

        private AvatarManager()
        {
            avatarTailor = new AvatarTailor();

            Plugin.instance.sceneTransitioned += OnSceneTransitioned;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        ~AvatarManager()
        {
            Plugin.instance.sceneTransitioned -= OnSceneTransitioned;

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void GetAvatarsAsync(Action<CustomAvatar> success, Action<Exception> error)
        {
            Plugin.logger.Info("Loading all avatars from " + kCustomAvatarsPath);

            foreach (string fileName in GetAvatarFileNames())
            {
                SharedCoroutineStarter.instance.StartCoroutine(CustomAvatar.FromFileCoroutine(fileName, success, error));
            }
        }

        public void LoadAvatarFromSettingsAsync()
        {
            var previousAvatarPath = SettingsManager.settings.previousAvatarPath;

            if (!File.Exists(previousAvatarPath))
            {
                previousAvatarPath = GetAvatarFileNames().FirstOrDefault();
            }

            if (string.IsNullOrEmpty(previousAvatarPath))
            {
                Plugin.logger.Info("No avatars found");
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
                Plugin.logger.Error("Failed to load avatar: " + ex.Message);
            }));
        }

        public void SwitchToAvatar(CustomAvatar avatar)
        {
            if (avatar == null) return;
            if (currentlySpawnedAvatar?.customAvatar == avatar) return;

            currentlySpawnedAvatar?.Destroy();

            currentlySpawnedAvatar = SpawnAvatar(avatar);

            avatarChanged?.Invoke(currentlySpawnedAvatar);

            ResizeCurrentAvatar();
            currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();

            SettingsManager.settings.previousAvatarPath = avatar.fullPath;
        }

        public void SwitchToNextAvatar()
        {
            string[] files = GetAvatarFileNames();
            int index = Array.IndexOf(files, currentlySpawnedAvatar.customAvatar.fullPath);

            index = (index + 1) % files.Length;

            SwitchToAvatarAsync(files[index]);
        }

        public void SwitchToPreviousAvatar()
        {
            string[] files = GetAvatarFileNames();
            int index = Array.IndexOf(files, currentlySpawnedAvatar.customAvatar.fullPath);

            index = (index + files.Length - 1) % files.Length;

            SwitchToAvatarAsync(files[index]);
        }

        public void ResizeCurrentAvatar()
        {
            if (currentlySpawnedAvatar?.customAvatar.descriptor.allowHeightCalibration != true) return;

            avatarTailor.ResizeAvatar(currentlySpawnedAvatar);
        }

        private void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            ResizeCurrentAvatar();
            currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();
            currentlySpawnedAvatar?.eventsPlayer?.Restart();
        }

        private void OnSceneTransitioned(Scene newScene)
        {
            if (newScene.name.Equals("GameCore"))
                currentlySpawnedAvatar?.eventsPlayer?.LevelStartedEvent();
            else if (newScene.name.Equals("MenuCore"))
                currentlySpawnedAvatar?.eventsPlayer.MenuEnteredEvent();
        }

        private static SpawnedAvatar SpawnAvatar(CustomAvatar customAvatar)
        {
            return new SpawnedAvatar(customAvatar);
        }

        private string[] GetAvatarFileNames()
        {
            return Directory.GetFiles(kCustomAvatarsPath, "*.avatar").Select(f => GetRelativePath(kCustomAvatarsPath, f)).ToArray();
        }

        private string GetRelativePath(string rootDirectoryPath, string path)
        {
            string fullRootDirectoryPath = Path.GetFullPath(rootDirectoryPath);
            string fullPath = Path.GetFullPath(path);

            if (!fullPath.StartsWith(fullRootDirectoryPath))
            {
                return fullPath;
            }

            return fullPath.Substring(fullRootDirectoryPath.Length + 1);
        }
    }
}
