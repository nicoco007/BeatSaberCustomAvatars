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
using Object = UnityEngine.Object;

namespace CustomAvatar
{
    public class PlayerAvatarManager : IDisposable
    {
        public static readonly string kCustomAvatarsPath = Path.GetFullPath("CustomAvatars");
        
        internal SpawnedAvatar currentlySpawnedAvatar { get; private set; }

        internal event Action<SpawnedAvatar> avatarChanged;

        private readonly ILogger _logger;
        private readonly AvatarLoader _avatarLoader;
        private readonly AvatarTailor _avatarTailor;
        private readonly TrackedDeviceManager _trackedDeviceManager;
        private readonly Settings _settings;
        private readonly AvatarSpawner _spawner;

        [Inject]
        private PlayerAvatarManager(AvatarTailor avatarTailor, ILoggerProvider loggerProvider, AvatarLoader avatarLoader, TrackedDeviceManager trackedDeviceManager, Settings settings, AvatarSpawner spawner)
        {
            _logger = loggerProvider.CreateLogger<PlayerAvatarManager>();
            _avatarLoader = avatarLoader;
            _avatarTailor = avatarTailor;
            _trackedDeviceManager = trackedDeviceManager;
            _settings = settings;
            _spawner = spawner;

            Plugin.instance.sceneTransitionDidFinish += OnSceneTransitionDidFinish;
            SceneManager.sceneLoaded += OnSceneLoaded;
            BeatSaberEvents.playerHeightChanged += OnPlayerHeightChanged;
        }

        public void Dispose()
        {
            Object.Destroy(currentlySpawnedAvatar);

            Plugin.instance.sceneTransitionDidFinish -= OnSceneTransitionDidFinish;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            BeatSaberEvents.playerHeightChanged -= OnPlayerHeightChanged;
        }

        public void GetAvatarsAsync(Action<LoadedAvatar> success = null, Action<Exception> error = null)
        {
            _logger.Info("Loading all avatars from " + kCustomAvatarsPath);

            foreach (string fileName in GetAvatarFileNames())
            {
                SharedCoroutineStarter.instance.StartCoroutine(_avatarLoader.FromFileCoroutine(fileName, success, error));
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

        public void SwitchToAvatarAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                SwitchToAvatar(null);
                return;
            }

            string fullPath = Path.Combine(kCustomAvatarsPath, fileName);

            SharedCoroutineStarter.instance.StartCoroutine(_avatarLoader.FromFileCoroutine(fullPath, SwitchToAvatar));
        }

        public void SwitchToAvatar(LoadedAvatar avatar)
        {
            if (currentlySpawnedAvatar && currentlySpawnedAvatar.avatar == avatar) return;

            Object.Destroy(currentlySpawnedAvatar);

            _settings.previousAvatarPath = avatar?.fullPath;

            if (avatar == null)
            {
                _logger.Info("No avatar selected");
                avatarChanged?.Invoke(null);
                return;
            }

            currentlySpawnedAvatar = _spawner.SpawnAvatar(avatar, new VRAvatarInput(_trackedDeviceManager));

            ResizeCurrentAvatar();

            avatarChanged?.Invoke(currentlySpawnedAvatar);
        }

        public void SwitchToNextAvatar()
        {
            List<string> files = GetAvatarFileNames();
            files.Insert(0, null);
            
            int index = currentlySpawnedAvatar ? files.IndexOf(currentlySpawnedAvatar.avatar.fullPath) : 0;

            index = (index + 1) % files.Count;

            SwitchToAvatarAsync(files[index]);
        }

        public void SwitchToPreviousAvatar()
        {
            List<string> files = GetAvatarFileNames();
            files.Insert(0, null);
            
            int index = currentlySpawnedAvatar ? files.IndexOf(currentlySpawnedAvatar.avatar.fullPath) : 0;

            index = (index + files.Count - 1) % files.Count;
            
            SwitchToAvatarAsync(files[index]);
        }

        public void ResizeCurrentAvatar()
        {
            if (currentlySpawnedAvatar)
            {
                _avatarTailor.ResizeAvatar(currentlySpawnedAvatar);
            }
        }

        private void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            if (!currentlySpawnedAvatar) return;

            if (newScene.name == "PCInit" && _settings.calibrateFullBodyTrackingOnStart && _settings.GetAvatarSettings(currentlySpawnedAvatar.avatar.fullPath).useAutomaticCalibration)
            {
                _avatarTailor.CalibrateFullBodyTrackingAuto(currentlySpawnedAvatar);
            }

            ResizeCurrentAvatar();
        }

        private void OnSceneTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            if (!currentlySpawnedAvatar) return;

            ResizeCurrentAvatar();
        }

        private void OnPlayerHeightChanged(float height)
        {
            ResizeCurrentAvatar();
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
