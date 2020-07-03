using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using CustomAvatar.Utilities.Converters;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
    public class PlayerAvatarManager : IDisposable
    {
        public static readonly string kCustomAvatarsPath = Path.GetFullPath("CustomAvatars");
        public static readonly string kAvatarInfoCacheFilePath = Path.GetFullPath(Path.Combine("UserData", "CustomAvatars.Cache.json"));

        internal SpawnedAvatar currentlySpawnedAvatar { get; private set; }

        internal event Action<SpawnedAvatar> avatarChanged;

        private readonly DiContainer _container;
        private readonly ILogger _logger;
        private readonly AvatarLoader _avatarLoader;
        private readonly AvatarTailor _avatarTailor;
        private readonly Settings _settings;
        private readonly AvatarSpawner _spawner;
        private readonly GameScenesManager _gameScenesManager;

        private readonly Dictionary<string, AvatarInfo> _avatarInfos = new Dictionary<string, AvatarInfo>();
        private string _switchingToPath;

        [Inject]
        private PlayerAvatarManager(DiContainer container, AvatarTailor avatarTailor, ILoggerProvider loggerProvider, AvatarLoader avatarLoader, Settings settings, AvatarSpawner spawner, GameScenesManager gameScenesManager)
        {
            _container = container;
            _logger = loggerProvider.CreateLogger<PlayerAvatarManager>();
            _avatarLoader = avatarLoader;
            _avatarTailor = avatarTailor;
            _settings = settings;
            _spawner = spawner;
            _gameScenesManager = gameScenesManager;

            _settings.moveFloorWithRoomAdjustChanged += OnMoveFloorWithRoomAdjustChanged;
            _settings.firstPersonEnabledChanged += OnFirstPersonEnabledChanged;
            _gameScenesManager.transitionDidFinishEvent += OnSceneTransitionDidFinish;
            SceneManager.sceneLoaded += OnSceneLoaded;
            BeatSaberEvents.playerHeightChanged += OnPlayerHeightChanged;

            LoadAvatarInfosFromFile();
        }

        public void Dispose()
        {
            Object.Destroy(currentlySpawnedAvatar);

            _settings.moveFloorWithRoomAdjustChanged -= OnMoveFloorWithRoomAdjustChanged;
            _gameScenesManager.transitionDidFinishEvent -= OnSceneTransitionDidFinish;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            BeatSaberEvents.playerHeightChanged -= OnPlayerHeightChanged;

            SaveAvatarInfosToFile();
        }

        private JsonSerializer GetSerializer()
        {
            return new JsonSerializer
            {
                Formatting = Formatting.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new Vector2JsonConverter(), new Vector3JsonConverter(), new QuaternionJsonConverter(), new PoseJsonConverter(), new FloatJsonConverter(), new ColorJsonConverter(), new Texture2DConverter() }
            };
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
                string fullPath = Path.Combine(kCustomAvatarsPath, fileName);

                if (_avatarInfos.ContainsKey(fileName) && _avatarInfos[fileName].IsForFile(fullPath))
                {
                    _logger.Info($"Using cached information for '{fileName}'");
                    success(_avatarInfos[fileName]);
                }
                else
                {
                    SharedCoroutineStarter.instance.StartCoroutine(_avatarLoader.FromFileCoroutine(fullPath,
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
            string previousAvatarFileName = _settings.previousAvatarPath;

            if (string.IsNullOrEmpty(previousAvatarFileName))
            {
                return;
            }

            if (!File.Exists(Path.Combine(kCustomAvatarsPath, previousAvatarFileName)))
            {
                _logger.Warning("Previously loaded avatar no longer exists");
                return;
            }

            SwitchToAvatarAsync(previousAvatarFileName);
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

            if (avatar == null)
            {
                _logger.Info("No avatar selected");
                avatarChanged?.Invoke(null);
                _settings.previousAvatarPath = null;
                return;
            }
            
            var avatarInfo = new AvatarInfo(avatar);

            _settings.previousAvatarPath = avatarInfo.fileName;

            // cache avatar info since loading asset bundles is expensive
            if (_avatarInfos.ContainsKey(avatarInfo.fileName))
            {
                _avatarInfos[avatarInfo.fileName] = avatarInfo;
            }
            else
            {
                _avatarInfos.Add(avatarInfo.fileName, avatarInfo);
            }

            DiContainer subContainer = new DiContainer(_container);

            subContainer.Bind<LoadedAvatar>().FromInstance(avatar);
            subContainer.Bind<Settings.AvatarSpecificSettings>().FromInstance(_settings.GetAvatarSettings(avatarInfo.fileName));
            subContainer.BindInterfacesTo<VRPlayerInput>().AsSingle();

            currentlySpawnedAvatar = _spawner.SpawnAvatar(avatar, subContainer.Resolve<IAvatarInput>());

            ResizeCurrentAvatar();
            
            currentlySpawnedAvatar.UpdateFirstPersonVisibility(_settings.isAvatarVisibleInFirstPerson ? FirstPersonVisibility.VisibleWithExclusions : FirstPersonVisibility.None);

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

            currentlySpawnedAvatar.UpdateFirstPersonVisibility(enable ? FirstPersonVisibility.VisibleWithExclusions : FirstPersonVisibility.None);
        }

        private void OnSceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            if (!currentlySpawnedAvatar) return;

            if (newScene.name == "PCInit" && _settings.calibrateFullBodyTrackingOnStart && _settings.GetAvatarSettings(currentlySpawnedAvatar.avatar.fileName).useAutomaticCalibration)
            {
                _avatarTailor.CalibrateFullBodyTrackingAuto();
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
            return Directory.GetFiles(kCustomAvatarsPath, "*.avatar", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f)).ToList();
        }

        private void LoadAvatarInfosFromFile()
        {
            if (!File.Exists(kAvatarInfoCacheFilePath)) return;

            try
            {
                _logger.Info($"Loading cached avatar info from '{kAvatarInfoCacheFilePath}'");

                // storing in JSON isn't incredibly efficient but I'm lazy
                using (var reader = new StreamReader(kAvatarInfoCacheFilePath, Encoding.UTF8))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var serializer = GetSerializer();
                    var avatarInfos = serializer.Deserialize<AvatarInfo[]>(jsonReader);

                    if (avatarInfos == null) return;

                    foreach (AvatarInfo avatarInfo in avatarInfos)
                    {
                        if (!avatarInfo.isValid) continue;

                        string fullPath = Path.Combine(kCustomAvatarsPath, avatarInfo.fileName);

                        if (!File.Exists(fullPath)) continue;
                        if (!avatarInfo.IsForFile(fullPath)) continue;

                        _logger.Info($"Got cached info for '{fullPath}'");

                        if (_avatarInfos.ContainsKey(avatarInfo.fileName))
                        {
                            _avatarInfos[avatarInfo.fileName] = avatarInfo;
                        }
                        else
                        {
                            _avatarInfos.Add(avatarInfo.fileName, avatarInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load cached avatar info");
                _logger.Error(ex);
            }
        }

        private void SaveAvatarInfosToFile()
        {
            foreach (string fileName in _avatarInfos.Keys.ToList())
            {
                if (!File.Exists(Path.Combine(kCustomAvatarsPath, fileName)))
                {
                    _avatarInfos.Remove(fileName);
                }
            }

            try
            {
                _logger.Info($"Saving cached avatar info to '{kAvatarInfoCacheFilePath}'");

                using (var writer = new StreamWriter(kAvatarInfoCacheFilePath, false, Encoding.UTF8))
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    var serializer = GetSerializer();
                    serializer.Serialize(jsonWriter, _avatarInfos.Values);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to save cached avatar info");
                _logger.Error(ex);
            }
        }
    }
}
