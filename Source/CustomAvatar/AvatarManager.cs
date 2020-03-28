using System;
using System.IO;
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Tracking;
using UnityEngine.SceneManagement;
using Zenject;

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

            Plugin.instance.sceneTransitionDidFinish += OnSceneTransitionDidFinish;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        ~AvatarManager()
        {
            Plugin.instance.sceneTransitionDidFinish -= OnSceneTransitionDidFinish;

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void GetAvatarsAsync(Action<LoadedAvatar> success = null, Action<Exception> error = null)
        {
            Plugin.logger.Info("Loading all avatars from " + kCustomAvatarsPath);

            foreach (string fileName in GetAvatarFileNames())
            {
                SharedCoroutineStarter.instance.StartCoroutine(LoadedAvatar.FromFileCoroutine(fileName, success, error));
            }
        }

        public void LoadAvatarFromSettingsAsync()
        {
            string previousAvatarPath = Plugin.settings.previousAvatarPath;

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
            SharedCoroutineStarter.instance.StartCoroutine(LoadedAvatar.FromFileCoroutine(filePath, avatar =>
            {
                SwitchToAvatar(avatar);
            }));
        }

        public void SwitchToAvatar(LoadedAvatar avatar)
        {
            if (currentlySpawnedAvatar?.customAvatar == avatar) return;

            currentlySpawnedAvatar?.Destroy();
            currentlySpawnedAvatar = null;

            Plugin.settings.previousAvatarPath = avatar?.fullPath;

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
            currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();
            currentlySpawnedAvatar?.eventsPlayer?.Restart();

            if (newScene.name == "HealthWarning" && Plugin.settings.calibrateFullBodyTrackingOnStart && Plugin.settings.useAutomaticFullBodyCalibration)
            {
                avatarTailor.CalibrateFullBodyTrackingAuto();
            }

            ResizeCurrentAvatar();
        }

        private void OnSceneTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene == "GameCore" && currentlySpawnedAvatar?.eventsPlayer)
            {
                currentlySpawnedAvatar.eventsPlayer.LevelStartedEvent();
            }

            if (currentScene == "MenuCore" && currentlySpawnedAvatar?.eventsPlayer)
            {
                currentlySpawnedAvatar.eventsPlayer.MenuEnteredEvent();
            }

            ResizeCurrentAvatar();
        }

        private static SpawnedAvatar SpawnAvatar(LoadedAvatar customAvatar, AvatarInput input)
        {
            if (customAvatar == null) throw new ArgumentNullException(nameof(customAvatar));
            if (input == null) throw new ArgumentNullException(nameof(input));

            var spawnedAvatar = new SpawnedAvatar(customAvatar, input);

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
