//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using Zenject;
using Object = UnityEngine.Object;

namespace CustomAvatar.Avatar
{
    public class PlayerAvatarManager : IInitializable, IDisposable
    {
        public static readonly string kCustomAvatarsPath = Path.GetFullPath("CustomAvatars");
        public static readonly string kAvatarInfoCacheFilePath = Path.Combine(kCustomAvatarsPath, "cache.dat");
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
        private Settings.AvatarSpecificSettings _currentAvatarSettings;

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
        }

        public void Initialize()
        {
            _settings.moveFloorWithRoomAdjustChanged += OnMoveFloorWithRoomAdjustChanged;
            _settings.firstPersonEnabledChanged += OnFirstPersonEnabledChanged;
            BeatSaberUtilities.playerHeightChanged += OnPlayerHeightChanged;
            _gameScenesManager.transitionDidFinishEvent += OnTransitionDidFinish;

            if (_settings.calibrateFullBodyTrackingOnStart && !string.IsNullOrEmpty(_settings.previousAvatarPath) && _settings.GetAvatarSettings(_settings.previousAvatarPath).useAutomaticCalibration)
            {
                _avatarTailor.CalibrateFullBodyTrackingAuto();
            }

            LoadAvatarInfosFromFile();
            LoadAvatarFromSettingsAsync();
        }

        public void Dispose()
        {
            Object.Destroy(currentlySpawnedAvatar);

            _settings.moveFloorWithRoomAdjustChanged -= OnMoveFloorWithRoomAdjustChanged;
            _settings.firstPersonEnabledChanged -= OnFirstPersonEnabledChanged;
            BeatSaberUtilities.playerHeightChanged -= OnPlayerHeightChanged;
            _gameScenesManager.transitionDidFinishEvent -= OnTransitionDidFinish;

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
                    _logger.Trace($"Using cached information for '{fileName}'");
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
            _currentAvatarSettings = null;

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
            _currentAvatarSettings = _settings.GetAvatarSettings(avatar.fileName);

            ResizeCurrentAvatar();
            UpdateFirstPersonVisibility();
            UpdateLocomotionEnabled();

            avatarChanged?.Invoke(currentlySpawnedAvatar);
        }

        public void SwitchToNextAvatar()
        {
            List<string> files = GetAvatarFileNames();
            files.Insert(0, null);

            int index = !string.IsNullOrEmpty(_switchingToPath) ? files.IndexOf(Path.GetFileName(_switchingToPath)) : 0;

            index = (index + 1) % files.Count;

            SwitchToAvatarAsync(files[index]);
        }

        public void SwitchToPreviousAvatar()
        {
            List<string> files = GetAvatarFileNames();
            files.Insert(0, null);
            
            int index = !string.IsNullOrEmpty(_switchingToPath) ? files.IndexOf(Path.GetFileName(_switchingToPath)) : 0;

            index = (index + files.Count - 1) % files.Count;
            
            SwitchToAvatarAsync(files[index]);
        }

        internal void ResizeCurrentAvatar()
        {
            if (!currentlySpawnedAvatar) return;

            _avatarTailor.ResizeAvatar(currentlySpawnedAvatar);
        }

        internal void UpdateFirstPersonVisibility()
        {
            if (!currentlySpawnedAvatar) return;

            var visibility = FirstPersonVisibility.Hidden;

            if (_settings.isAvatarVisibleInFirstPerson)
            {
                if (_currentAvatarSettings.ignoreExclusions)
                {
                    visibility = FirstPersonVisibility.Visible;
                }
                else
                {
                    visibility = FirstPersonVisibility.VisibleWithExclusionsApplied;
                }
            }

            currentlySpawnedAvatar.SetFirstPersonVisibility(visibility);
        }

        internal void UpdateLocomotionEnabled()
        {
            if (!currentlySpawnedAvatar) return;

            currentlySpawnedAvatar.SetLocomotionEnabled(_settings.enableLocomotion);
        }

        private void OnMoveFloorWithRoomAdjustChanged(bool value)
        {
            ResizeCurrentAvatar();
        }

        private void OnFirstPersonEnabledChanged(bool enable)
        {
            UpdateFirstPersonVisibility();
        }

        private void OnPlayerHeightChanged(float height)
        {
            ResizeCurrentAvatar();
        }

        private void OnTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            ResizeCurrentAvatar();
        }

        private List<string> GetAvatarFileNames()
        {
            return Directory.GetFiles(kCustomAvatarsPath, "*.avatar", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f)).OrderBy(f => f).ToList();
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
                            reader.ReadTexture2D(),
                            reader.ReadString(),
                            reader.ReadInt64(),
                            reader.ReadDateTime(),
                            reader.ReadDateTime(),
                            reader.ReadDateTime()
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
                        writer.Write(avatarInfo.icon, true);
                        writer.Write(avatarInfo.fileName);
                        writer.Write(avatarInfo.fileSize);
                        writer.Write(avatarInfo.created);
                        writer.Write(avatarInfo.lastModified);
                        writer.Write(avatarInfo.timestamp);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to save avatar info cache");
                _logger.Error(ex);
            }
        }
    }
}
