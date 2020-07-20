using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
    public class PlayerAvatarManager : IDisposable
    {
        public static readonly string kCustomAvatarsPath = Path.GetFullPath("CustomAvatars");
        public static readonly string kAvatarInfoCacheFilePath = Path.Combine(kCustomAvatarsPath, "cache.db");
        public static readonly byte[] kCacheFileSignature = { 0x43, 0x41, 0x64, 0x62  }; // Custom Avatars Database (CAdb)
        public static readonly byte kCacheFileVersion = 1;

        internal SpawnedAvatar currentlySpawnedAvatar { get; private set; }

        internal event Action<SpawnedAvatar> avatarChanged;

        private readonly DiContainer _container;
        private readonly ILogger<PlayerAvatarManager> _logger;
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

            currentlySpawnedAvatar = _spawner.SpawnAvatar(avatar, _container.Instantiate<VRPlayerInput>(new object[] { avatar }));

            ResizeCurrentAvatar();
            
            currentlySpawnedAvatar.UpdateFirstPersonVisibility(_settings.isAvatarVisibleInFirstPerson ? FirstPersonVisibility.VisibleWithExclusionsApplied : FirstPersonVisibility.None);

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

            currentlySpawnedAvatar.UpdateFirstPersonVisibility(enable ? FirstPersonVisibility.VisibleWithExclusionsApplied : FirstPersonVisibility.None);
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

                using (var stream = new FileStream(kAvatarInfoCacheFilePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream))
                {
                    if (!reader.ReadBytes(kCacheFileSignature.Length).SequenceEqual(kCacheFileSignature))
                    {
                        _logger.Warning($"Invalid cache file magic");
                        return;
                    }

                    if (reader.ReadByte() != kCacheFileVersion)
                    {
                        _logger.Warning($"Invalid cache file version");
                        return;
                    }

                    int count = reader.ReadInt32();

                    _logger.Trace($"Reading {count} cached infos");

                    for (int i = 0; i < count; i++)
                    {
                        var avatarInfo = new AvatarInfo(
                            reader.ReadString(),
                            reader.ReadString(),
                            BytesToTexture2D(reader.ReadBytes(reader.ReadInt32())),
                            reader.ReadString(),
                            reader.ReadInt64(),
                            DateTime.FromBinary(reader.ReadInt64()),
                            DateTime.FromBinary(reader.ReadInt64()),
                            DateTime.FromBinary(reader.ReadInt64())
                        );

                        string fullPath = Path.Combine(kCustomAvatarsPath, avatarInfo.fileName);

                        if (!File.Exists(fullPath))
                        {
                            _logger.Notice($"File '{avatarInfo.fileName}' no longer exists; skipped");
                            continue;
                        }

                        if (!avatarInfo.IsForFile(fullPath))
                        {
                            _logger.Notice($"Info for '{avatarInfo.fileName}' is outdated; skipped");
                            continue;
                        }

                        _logger.Trace($"Got cached info for '{avatarInfo.fileName}'");

                        if (_avatarInfos.ContainsKey(avatarInfo.fileName))
                        {
                            if (_avatarInfos[avatarInfo.fileName].timestamp > avatarInfo.timestamp)
                            {
                                _logger.Notice($"Current info for '{avatarInfo.fileName}' is more recent; skipped");
                            }
                            else
                            {
                                _avatarInfos[avatarInfo.fileName] = avatarInfo;
                            }
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
            // remove files that no longer exist
            foreach (string fileName in _avatarInfos.Keys.ToList())
            {
                if (!File.Exists(Path.Combine(kCustomAvatarsPath, fileName)))
                {
                    _avatarInfos.Remove(fileName);
                }
            }

            try
            {
                _logger.Info($"Saving avatar info cache to '{kAvatarInfoCacheFilePath}'");

                using (var stream = new FileStream(kAvatarInfoCacheFilePath, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(kCacheFileSignature);
                    writer.Write(kCacheFileVersion);
                    writer.Write(_avatarInfos.Count);

                    foreach (AvatarInfo avatarInfo in _avatarInfos.Values)
                    {
                        writer.Write(avatarInfo.name);
                        writer.Write(avatarInfo.author);

                        byte[] textureBytes = BytesFromTexture2D(avatarInfo.icon);
                        writer.Write(textureBytes.Length);
                        writer.Write(textureBytes);

                        writer.Write(avatarInfo.fileName);
                        writer.Write(avatarInfo.fileSize);
                        writer.Write(avatarInfo.created.ToBinary());
                        writer.Write(avatarInfo.lastModified.ToBinary());
                        writer.Write(avatarInfo.timestamp.ToBinary());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to save avatar info cache");
                _logger.Error(ex);
            }
        }

        private byte[] BytesFromTexture2D(Texture2D texture)
        {
            if (texture == null) return new byte[0];

            float ratio = Mathf.Min(1f, 256f / texture.width, 256f / texture.height);
            int width = Mathf.RoundToInt(texture.width * ratio);
            int height = Mathf.RoundToInt(texture.height * ratio);

            if (ratio < 1)
            {
                _logger.Trace($"Resizing texture with ratio: {ratio} (before: {texture.width} × {texture.height}, after: {width} × {height})");
            }

            if (ratio < 1 || !texture.isReadable)
            {
                RenderTexture renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
                RenderTexture.active = renderTexture;
                Graphics.Blit(texture, renderTexture);
                texture = renderTexture.GetTexture2D();
                RenderTexture.active = null;
                renderTexture.Release();
            }
            
            return texture.EncodeToPNG();
        }

        private Texture2D BytesToTexture2D(byte[] bytes)
        {
            if (bytes.Length == 0) return null;

            Texture2D texture = new Texture2D(0, 0, TextureFormat.ARGB32, false);

            try
            {
                texture.LoadImage(bytes);
            }
            catch
            {
                return null;
            }

            return texture;
        }
    }
}
