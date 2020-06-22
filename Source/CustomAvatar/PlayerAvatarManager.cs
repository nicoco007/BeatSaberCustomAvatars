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
        private readonly GameScenesManager _gameScenesManager;

        private readonly Dictionary<string, AvatarInfo> _avatarInfos = new Dictionary<string, AvatarInfo>();
        private string _switchingToPath;

        [Inject]
        private PlayerAvatarManager(AvatarTailor avatarTailor, ILoggerProvider loggerProvider, AvatarLoader avatarLoader, TrackedDeviceManager trackedDeviceManager, Settings settings, AvatarSpawner spawner, GameScenesManager gameScenesManager)
        {
            _logger = loggerProvider.CreateLogger<PlayerAvatarManager>();
            _avatarLoader = avatarLoader;
            _avatarTailor = avatarTailor;
            _trackedDeviceManager = trackedDeviceManager;
            _settings = settings;
            _spawner = spawner;
            _gameScenesManager = gameScenesManager;

            _settings.moveFloorWithRoomAdjustChanged += OnMoveFloorWithRoomAdjustChanged;
            _settings.firstPersonEnabledChanged += OnFirstPersonEnabledChanged;
            _gameScenesManager.transitionDidFinishEvent += OnSceneTransitionDidFinish;
            SceneManager.sceneLoaded += OnSceneLoaded;
            BeatSaberEvents.playerHeightChanged += OnPlayerHeightChanged;
        }

        public void Dispose()
        {
            Object.Destroy(currentlySpawnedAvatar);

            _settings.moveFloorWithRoomAdjustChanged -= OnMoveFloorWithRoomAdjustChanged;
            _gameScenesManager.transitionDidFinishEvent -= OnSceneTransitionDidFinish;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            BeatSaberEvents.playerHeightChanged -= OnPlayerHeightChanged;
        }

        internal void GetAvatarInfosAsync(Action<AvatarInfo> success = null, Action<Exception> error = null)
        {
            List<string> fileNames = GetAvatarFileNames();

            foreach (string existingFile in _avatarInfos.Keys.ToList())
            {
                if (!fileNames.Contains(existingFile))
                {
                    _avatarInfos.Remove(existingFile);
                }
            }

            foreach (string fileName in fileNames)
            {
                if (_avatarInfos.ContainsKey(fileName))
                {
                    _logger.Info($"Using cached information for '{fileName}'");
                    success(_avatarInfos[fileName]);
                }
                else
                {
                    SharedCoroutineStarter.instance.StartCoroutine(_avatarLoader.FromFileCoroutine(Path.Combine(kCustomAvatarsPath, fileName),
                        (avatar) =>
                        {
                            var info = new AvatarInfo(avatar);
                            _avatarInfos.Add(fileName, info);
                            success?.Invoke(info);
                        }, error));
                }
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
                _logger.Warning("Previously loaded avatar no longer exists");
                return;
            }

            SwitchToAvatarAsync(previousAvatarPath);
        }

        public void SwitchToAvatarAsync(string fileName)
        {
            Object.Destroy(currentlySpawnedAvatar);
            currentlySpawnedAvatar = null;

            if (string.IsNullOrEmpty(fileName))
            {
                _switchingToPath = null;
                SwitchToAvatar(null);
                return;
            }

            string fullPath = Path.Combine(kCustomAvatarsPath, fileName);

            _switchingToPath = fullPath;

            SharedCoroutineStarter.instance.StartCoroutine(_avatarLoader.FromFileCoroutine(fullPath, SwitchToAvatar));
        }

        private void SwitchToAvatar(LoadedAvatar avatar)
        {
            if (currentlySpawnedAvatar && currentlySpawnedAvatar.avatar == avatar) return;
            if (avatar?.fullPath != _switchingToPath) return;

            _settings.previousAvatarPath = avatar?.fullPath;

            if (avatar == null)
            {
                _logger.Info("No avatar selected");
                avatarChanged?.Invoke(null);
                return;
            }

            // cache avatar info since loading asset bundles is expensive
            if (_avatarInfos.ContainsKey(avatar.fullPath))
            {
                _avatarInfos[avatar.fullPath] = new AvatarInfo(avatar);
            }
            else
            {
                _avatarInfos.Add(avatar.fullPath, new AvatarInfo(avatar));
            }

            currentlySpawnedAvatar = _spawner.SpawnAvatar(avatar, new VRPlayerInput(_trackedDeviceManager));

            ResizeCurrentAvatar();
            
            currentlySpawnedAvatar.UpdateFirstPersonVisibility(_settings.isAvatarVisibleInFirstPerson ? FirstPersonVisibility.ApplyFirstPersonExclusions : FirstPersonVisibility.None);

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
            if (!currentlySpawnedAvatar) return;

            _avatarTailor.ResizeAvatar(currentlySpawnedAvatar);
        }

        private void OnMoveFloorWithRoomAdjustChanged(bool value)
        {
            ResizeCurrentAvatar();
        }

        private void OnFirstPersonEnabledChanged(bool enable)
        {
            if (!currentlySpawnedAvatar) return;

            currentlySpawnedAvatar.UpdateFirstPersonVisibility(enable ? FirstPersonVisibility.ApplyFirstPersonExclusions : FirstPersonVisibility.None);
        }

        private void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            if (!currentlySpawnedAvatar) return;

            if (newScene.name == "PCInit" && _settings.calibrateFullBodyTrackingOnStart && _settings.GetAvatarSettings(currentlySpawnedAvatar.avatar.fullPath).useAutomaticCalibration)
            {
                _avatarTailor.CalibrateFullBodyTrackingAuto(currentlySpawnedAvatar.input);
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
