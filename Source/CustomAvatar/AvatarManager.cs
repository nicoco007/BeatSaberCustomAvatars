using System;
using System.IO;
using System.Linq;
using CustomAvatar.Tracking;
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
        
        internal AvatarTailor avatarTailor { get; }
        internal SpawnedAvatar currentlySpawnedAvatar { get; private set; }

        internal event Action<SpawnedAvatar> avatarChanged;

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
            string previousAvatarPath = SettingsManager.settings.previousAvatarPath;

            if (string.IsNullOrEmpty(previousAvatarPath))
            {
                return;
            }

            if (!File.Exists(Path.Combine(kCustomAvatarsPath, previousAvatarPath)))
            {
                Plugin.logger.Warn("Previously loaded avatar no longer exists; reverting to default");
                return;
            }

            SwitchToAvatarAsync(previousAvatarPath);
        }

        public void SwitchToAvatarAsync(string filePath)
        {
            SharedCoroutineStarter.instance.StartCoroutine(CustomAvatar.FromFileCoroutine(filePath, avatar =>
            {
                Plugin.logger.Info("Successfully loaded avatar " + avatar.descriptor.name);
                SwitchToAvatar(avatar);
            }, ex =>
            {
                Plugin.logger.Error("Failed to load avatar: " + ex.Message);
            }));
        }

        public void SwitchToAvatar(CustomAvatar avatar)
        {
            if (currentlySpawnedAvatar?.customAvatar == avatar) return;

            currentlySpawnedAvatar?.Destroy();
            currentlySpawnedAvatar = null;

            SettingsManager.settings.previousAvatarPath = avatar?.fullPath;

            if (avatar == null) return;

            currentlySpawnedAvatar = SpawnAvatar(avatar, new VRAvatarInput());

            avatarChanged?.Invoke(currentlySpawnedAvatar);

            ResizeCurrentAvatar();
            currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();
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
            if (currentlySpawnedAvatar != null)
            {
                avatarTailor.ResizeAvatar(currentlySpawnedAvatar);
            }
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

        private static SpawnedAvatar SpawnAvatar(CustomAvatar customAvatar, AvatarInput input)
        {
            if (customAvatar == null) throw new ArgumentNullException(nameof(customAvatar));
            if (input == null) throw new ArgumentNullException(nameof(input));

            var spawnedAvatar = new SpawnedAvatar(customAvatar);

            spawnedAvatar.behaviour.input = input;

            return spawnedAvatar;
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
