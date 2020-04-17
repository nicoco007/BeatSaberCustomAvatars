using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine.SceneManagement;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar
{
    public class AvatarManager : IDisposable
    {
        public static readonly string kCustomAvatarsPath = Path.GetFullPath("CustomAvatars");
        
        internal AvatarTailor avatarTailor { get; }
        internal SpawnedAvatar currentlySpawnedAvatar { get; private set; }

        internal event Action<SpawnedAvatar> avatarChanged;

        private readonly ILogger _logger;
        private readonly TrackedDeviceManager _trackedDeviceManager;
        private readonly Settings _settings;
        private readonly DiContainer _container;

        private AvatarManager(AvatarTailor avatarTailor, ILoggerFactory loggerFactory, TrackedDeviceManager trackedDeviceManager, Settings settings, DiContainer container)
        {
            this.avatarTailor = avatarTailor;
            _logger = loggerFactory.CreateLogger<AvatarManager>();
            _trackedDeviceManager = trackedDeviceManager;
            _settings = settings;
            _container = container;

            Plugin.instance.sceneTransitionDidFinish += OnSceneTransitionDidFinish;
            SceneManager.sceneLoaded += OnSceneLoaded;
            BeatSaberEvents.onPlayerHeightChanged += OnPlayerHeightChanged;
        }

        public void Dispose()
        {
            currentlySpawnedAvatar.Destroy();
            currentlySpawnedAvatar = null;

            Plugin.instance.sceneTransitionDidFinish -= OnSceneTransitionDidFinish;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            BeatSaberEvents.onPlayerHeightChanged -= OnPlayerHeightChanged;
        }

        public void GetAvatarsAsync(Action<LoadedAvatar> success = null, Action<Exception> error = null)
        {
            _logger.Info("Loading all avatars from " + kCustomAvatarsPath);

            foreach (string fileName in GetAvatarFileNames())
            {
                SharedCoroutineStarter.instance.StartCoroutine(LoadedAvatar.FromFileCoroutine(fileName, success, error));
            }
        }

        public void LoadAvatarFromSettingsAsync()
        {
            string previousAvatarPath = _settings.previousAvatarPath;

            if (string.IsNullOrEmpty(previousAvatarPath))
            {
                return;
            }

            if (!File.Exists(Path.Combine(kCustomAvatarsPath, previousAvatarPath)))
            {
                _logger.Warning("Previously loaded avatar no longer exists; reverting to default");
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

            _settings.previousAvatarPath = avatar?.fullPath;

            if (avatar == null)
            {
                _logger.Info("No avatar selected");
                avatarChanged?.Invoke(null);
                return;
            }

            currentlySpawnedAvatar = SpawnAvatar(avatar, new VRAvatarInput(_trackedDeviceManager));

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

            if (newScene.name == "PCInit" && _settings.calibrateFullBodyTrackingOnStart && _settings.GetAvatarSettings(currentlySpawnedAvatar.avatar.fullPath).useAutomaticCalibration)
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

        private void OnPlayerHeightChanged(float height)
        {
            ResizeCurrentAvatar();
        }

        private SpawnedAvatar SpawnAvatar(LoadedAvatar customAvatar, AvatarInput input)
        {
            if (customAvatar == null) throw new ArgumentNullException(nameof(customAvatar));
            if (input == null) throw new ArgumentNullException(nameof(input));
            
            return _container.Instantiate<SpawnedAvatar>(new object[] { customAvatar, input });
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
