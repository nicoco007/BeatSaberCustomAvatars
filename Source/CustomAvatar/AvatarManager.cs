using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine.SceneManagement;
using Zenject;

namespace CustomAvatar
{
    public class AvatarManager
    {
        public static readonly string kCustomAvatarsPath = Path.GetFullPath("CustomAvatars");
        
        internal AvatarTailor avatarTailor { get; }
        internal SpawnedAvatar currentlySpawnedAvatar { get; private set; }

        internal event Action<SpawnedAvatar> avatarChanged;

        private AvatarManager(PlayerDataModel playerDataModel)
        {
            avatarTailor = new AvatarTailor();

            Plugin.instance.sceneTransitionDidFinish += OnSceneTransitionDidFinish;

            SceneManager.sceneLoaded += OnSceneLoaded;
            BeatSaberEvents.onPlayerHeightChanged += (height) => ResizeCurrentAvatar();

            Plugin.logger.Info("playerDataModel: " + playerDataModel);
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
            if (string.IsNullOrEmpty(filePath))
            {
                SwitchToAvatar(null);
                return;
            }

            SharedCoroutineStarter.instance.StartCoroutine(LoadedAvatar.FromFileCoroutine(filePath, SwitchToAvatar));
        }

        public void SwitchToAvatar(LoadedAvatar avatar)
        {
            if (currentlySpawnedAvatar?.avatar == avatar) return;

            currentlySpawnedAvatar?.Destroy();
            currentlySpawnedAvatar = null;

            SettingsManager.settings.previousAvatarPath = avatar?.fullPath;

            if (avatar == null)
            {
                Plugin.logger.Info("No avatar selected");
                avatarChanged?.Invoke(null);
                return;
            }

            currentlySpawnedAvatar = SpawnAvatar(avatar, new VRAvatarInput());

            ResizeCurrentAvatar();
            currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();

            avatarChanged?.Invoke(currentlySpawnedAvatar);
        }

        public void SwitchToNextAvatar()
        {
            List<string> files = GetAvatarFileNames();
            files.Insert(0, null);

            int index = files.IndexOf(currentlySpawnedAvatar?.avatar.fullPath);

            index = (index + 1) % files.Count;

            SwitchToAvatarAsync(files[index]);
        }

        public void SwitchToPreviousAvatar()
        {
            List<string> files = GetAvatarFileNames();
            files.Insert(0, null);
            
            int index = files.IndexOf(currentlySpawnedAvatar?.avatar.fullPath);

            index = (index + files.Count - 1) % files.Count;
            
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
            if (currentlySpawnedAvatar == null) return;

            currentlySpawnedAvatar.OnFirstPersonEnabledChanged();
            currentlySpawnedAvatar.eventsPlayer?.Restart();

            if (newScene.name == "PCInit" && SettingsManager.settings.calibrateFullBodyTrackingOnStart && SettingsManager.settings.GetAvatarSettings(currentlySpawnedAvatar.avatar.fullPath).useAutomaticCalibration)
            {
                avatarTailor.CalibrateFullBodyTrackingAuto(currentlySpawnedAvatar);
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

        private List<string> GetAvatarFileNames()
        {
            return Directory.GetFiles(kCustomAvatarsPath, "*.avatar").Select(f => GetRelativePath(kCustomAvatarsPath, f)).ToList();
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
