//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace CustomAvatar.Player
{
    /// <summary>
    /// Manages the player's local avatar.
    /// </summary>
    public class PlayerAvatarManager : IInitializable, IDisposable
    {
        public static readonly string kCustomAvatarsPath = Path.GetFullPath("CustomAvatars");
        public static readonly string kAvatarInfoCacheFilePath = Path.Combine(kCustomAvatarsPath, "cache.dat");
        public static readonly byte[] kCacheFileSignature = { 0x43, 0x41, 0x64, 0x62 }; // Custom Avatars Database (CAdb)
        public static readonly byte kCacheFileVersion = 1;

        /// <summary>
        /// The player's currently spawned avatar. This can be null.
        /// </summary>
        public SpawnedAvatar currentlySpawnedAvatar { get; private set; }

        /// <summary>
        /// Event triggered when the current avatar is deleted an a new one starts loading. Note that the argument may be null if no avatar was selected to replace the previous one.
        /// </summary>
        public event Action<string> avatarStartedLoading;

        /// <summary>
        /// Event triggered when a new avatar has finished loading and is spawned. Note that the argument may be null if no avatar was selected to replace the previous one.
        /// </summary>
        public event Action<SpawnedAvatar> avatarChanged;
        public event Action<Exception> avatarLoadFailed;
        public event Action<float> avatarScaleChanged;

        internal event Action<AvatarInfo> avatarAdded;
        internal event Action<AvatarInfo> avatarRemoved;

        private readonly DiContainer _container;
        private readonly ILogger<PlayerAvatarManager> _logger;
        private readonly AvatarLoader _avatarLoader;
        private readonly Settings _settings;
        private readonly AvatarSpawner _spawner;
        private readonly BeatSaberUtilities _beatSaberUtilities;
        private readonly FloorController _floorController;

        private readonly FileSystemWatcher _fileSystemWatcher;
        private readonly Dictionary<string, AvatarInfo> _avatarInfos = new Dictionary<string, AvatarInfo>();

        private string _switchingToPath;
        private Settings.AvatarSpecificSettings _currentAvatarSettings;
        private GameObject _avatarContainer;

        [Inject]
        private PlayerAvatarManager(DiContainer container, ILoggerProvider loggerProvider, AvatarLoader avatarLoader, Settings settings, AvatarSpawner spawner, BeatSaberUtilities beatSaberUtilities, FloorController floorController)
        {
            _container = container;
            _logger = loggerProvider.CreateLogger<PlayerAvatarManager>();
            _avatarLoader = avatarLoader;
            _settings = settings;
            _spawner = spawner;
            _beatSaberUtilities = beatSaberUtilities;
            _floorController = floorController;

            _fileSystemWatcher = new FileSystemWatcher(kCustomAvatarsPath, "*.avatar");
            _fileSystemWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
        }

        public void Initialize()
        {
            _settings.moveFloorWithRoomAdjust.changed += OnMoveFloorWithRoomAdjustChanged;
            _settings.isAvatarVisibleInFirstPerson.changed += OnFirstPersonEnabledChanged;
            _settings.resizeMode.changed += OnResizeModeChanged;
            _settings.floorHeightAdjust.changed += OnFloorHeightAdjustChanged;
            _settings.isAvatarVisibleInFirstPerson.changed += OnAvatarVisibleInFirstPersonChanged;

            _floorController.floorPositionChanged += OnFloorPositionChanged;
            BeatSaberEvents.playerHeightChanged += OnPlayerHeightChanged;

            _avatarContainer = new GameObject("Avatar Container");
            Object.DontDestroyOnLoad(_avatarContainer);

            _fileSystemWatcher.Changed += OnAvatarFileChanged;
            _fileSystemWatcher.Created += OnAvatarFileCreated;
            _fileSystemWatcher.Deleted += OnAvatarFileDeleted;

            _fileSystemWatcher.EnableRaisingEvents = true;

            _logger.Trace($"Watching files in '{_fileSystemWatcher.Path}' ('{_fileSystemWatcher.Filter}')");

            LoadAvatarInfosFromFile();
            LoadAvatarFromSettingsAsync();
        }

        public void Dispose()
        {
            currentlySpawnedAvatar?.avatar.Dispose();
            Object.Destroy(_avatarContainer);

            _settings.moveFloorWithRoomAdjust.changed -= OnMoveFloorWithRoomAdjustChanged;
            _settings.isAvatarVisibleInFirstPerson.changed -= OnFirstPersonEnabledChanged;
            _settings.resizeMode.changed -= OnResizeModeChanged;
            _settings.floorHeightAdjust.changed -= OnFloorHeightAdjustChanged;
            _settings.isAvatarVisibleInFirstPerson.changed -= OnAvatarVisibleInFirstPersonChanged;

            _floorController.floorPositionChanged -= OnFloorPositionChanged;
            BeatSaberEvents.playerHeightChanged -= OnPlayerHeightChanged;

            _fileSystemWatcher.Changed -= OnAvatarFileChanged;
            _fileSystemWatcher.Created -= OnAvatarFileCreated;
            _fileSystemWatcher.Deleted -= OnAvatarFileDeleted;

            _fileSystemWatcher.Dispose();

            SaveAvatarInfosToFile();
        }

        internal void GetAvatarInfosAsync(Action<AvatarInfo> success = null, Action<Exception> error = null, Action complete = null, bool forceReload = false)
        {
            List<string> fileNames = GetAvatarFileNames();
            int loadedCount = 0;

            foreach (string existingFile in _avatarInfos.Keys.ToList())
            {
                if (!fileNames.Contains(existingFile))
                {
                    _avatarInfos.Remove(existingFile);
                }
            }

            if (forceReload)
            {
                string fullPath = currentlySpawnedAvatar ? currentlySpawnedAvatar.avatar.fullPath : null;
                SwitchToAvatarAsync(null);
                _switchingToPath = fullPath;
            }

            foreach (string fileName in fileNames)
            {
                string fullPath = Path.Combine(kCustomAvatarsPath, fileName);

                if (!forceReload && _avatarInfos.ContainsKey(fileName) && _avatarInfos[fileName].IsForFile(fullPath))
                {
                    _logger.Trace($"Using cached information for '{fileName}'");
                    success?.Invoke(_avatarInfos[fileName]);

                    if (++loadedCount == fileNames.Count) complete?.Invoke();
                }
                else
                {
                    SharedCoroutineStarter.instance.StartCoroutine(_avatarLoader.FromFileCoroutine(fullPath,
                        (avatar) =>
                        {
                            var info = new AvatarInfo(avatar);

                            if (_avatarInfos.ContainsKey(fileName))
                            {
                                _avatarInfos[fileName] = info;
                            }
                            else
                            {
                                _avatarInfos.Add(fileName, info);
                            }

                            success?.Invoke(info);

                            if (avatar.fullPath == _switchingToPath)
                            {
                                SwitchToAvatar(avatar);
                            }
                        },
                        (exception) =>
                        {
                            error?.Invoke(exception);
                        },
                        () =>
                        {
                            if (++loadedCount == fileNames.Count) complete?.Invoke();
                        }));
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
            currentlySpawnedAvatar?.avatar.Dispose();
            Object.Destroy(currentlySpawnedAvatar);
            currentlySpawnedAvatar = null;
            _currentAvatarSettings = null;

            if (string.IsNullOrEmpty(fileName))
            {
                _switchingToPath = null;
                avatarStartedLoading?.Invoke(null);
                SwitchToAvatar(null);
                return;
            }

            string fullPath = Path.Combine(kCustomAvatarsPath, fileName);

            _switchingToPath = fullPath;

            avatarStartedLoading?.Invoke(fullPath);

            SharedCoroutineStarter.instance.StartCoroutine(_avatarLoader.FromFileCoroutine(fullPath, SwitchToAvatar, OnAvatarLoadFailed));
        }

        private void LoadAvatar(string fullPath, Action<LoadedAvatar> success = null, Action<Exception> error = null, Action complete = null)
        {
            SharedCoroutineStarter.instance.StartCoroutine(_avatarLoader.FromFileCoroutine(fullPath,
                (avatar) =>
                {
                    var info = new AvatarInfo(avatar);
                    string fileName = info.fileName;

                    if (_avatarInfos.ContainsKey(fileName))
                    {
                        _avatarInfos[fileName] = info;
                    }
                    else
                    {
                        _avatarInfos.Add(fileName, info);
                    }

                    success?.Invoke(avatar);
                    avatarAdded?.Invoke(info);
                }, error, complete));
        }

        private void OnAvatarFileChanged(object sender, FileSystemEventArgs e)
        {
            _logger.Trace($"File change detected: '{e.FullPath}'");

            if (e.FullPath == currentlySpawnedAvatar?.avatar.fullPath)
            {
                _logger.Info("Reloading spawned avatar");
                SwitchToAvatarAsync(e.Name);
            }
            else
            {
                _logger.Info($"Reloading avatar info for '{e.FullPath}'");
                LoadAvatar(e.FullPath);
            }
        }

        private void OnAvatarFileCreated(object sender, FileSystemEventArgs e)
        {
            _logger.Info($"Loading avatar info for '{e.FullPath}'");
            LoadAvatar(e.FullPath, (avatar) => avatar.Dispose());
        }

        private void OnAvatarFileDeleted(object sender, FileSystemEventArgs e)
        {
            _logger.Trace($"File deleted: '{e.FullPath}'");

            string fileName = Path.GetFileName(e.FullPath);

            if (_avatarInfos.TryGetValue(fileName, out AvatarInfo info))
            {
                _logger.Info($"Removing '{fileName}'");
                _avatarInfos.Remove(fileName);
                avatarRemoved?.Invoke(info);
            }
        }

        private void SwitchToAvatar(LoadedAvatar avatar)
        {
            if ((currentlySpawnedAvatar && currentlySpawnedAvatar.avatar == avatar) || avatar?.fullPath != _switchingToPath)
            {
                avatar?.Dispose();
                return;
            }

            if (avatar == null)
            {
                _logger.Info("No avatar selected");
                avatarChanged?.Invoke(null);
                _settings.previousAvatarPath = null;
                UpdateFloorOffsetForCurrentAvatar();
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

            currentlySpawnedAvatar = _spawner.SpawnAvatar(avatar, _container.Resolve<VRPlayerInput>(), _avatarContainer.transform);
            _currentAvatarSettings = _settings.GetAvatarSettings(avatar.fileName);

            ResizeCurrentAvatar();
            UpdateFirstPersonVisibility();
            UpdateLocomotionEnabled();

            avatarChanged?.Invoke(currentlySpawnedAvatar);
        }

        private void OnAvatarLoadFailed(Exception error)
        {
            avatarLoadFailed?.Invoke(error);
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

        internal void Move(Vector3 position, Quaternion rotation)
        {
            _avatarContainer.transform.SetPositionAndRotation(position, rotation);

            if (currentlySpawnedAvatar && currentlySpawnedAvatar.ik) currentlySpawnedAvatar.ik.ResetSolver();
        }

        private void OnResizeModeChanged(AvatarResizeMode resizeMode)
        {
            ResizeCurrentAvatar();
        }

        private void OnFloorHeightAdjustChanged(FloorHeightAdjust floorHeightAdjust)
        {
            ResizeCurrentAvatar();
        }

        private void OnAvatarVisibleInFirstPersonChanged(bool visible)
        {
            UpdateFirstPersonVisibility();
        }

        private void ResizeCurrentAvatar()
        {
            if (!currentlySpawnedAvatar || !currentlySpawnedAvatar.avatar.descriptor.allowHeightCalibration) return;

            float scale;
            AvatarResizeMode resizeMode = _settings.resizeMode;

            switch (resizeMode)
            {
                case AvatarResizeMode.ArmSpan:
                    float avatarArmLength = currentlySpawnedAvatar.avatar.armSpan;

                    if (avatarArmLength > 0)
                    {
                        scale = _settings.playerArmSpan / avatarArmLength;
                    }
                    else
                    {
                        scale = 1.0f;
                    }

                    break;

                case AvatarResizeMode.Height:
                    float avatarEyeHeight = currentlySpawnedAvatar.avatar.eyeHeight;
                    float playerEyeHeight = _beatSaberUtilities.GetRoomAdjustedPlayerEyeHeight();

                    if (avatarEyeHeight > 0)
                    {
                        scale = playerEyeHeight / avatarEyeHeight;
                    }
                    else
                    {
                        scale = 1.0f;
                    }

                    break;

                default:
                    scale = 1.0f;
                    break;
            }

            if (scale <= 0)
            {
                _logger.Warning("Calculated scale is <= 0; reverting to 1");
                scale = 1.0f;
            }

            currentlySpawnedAvatar.scale = scale;

            UpdateFloorOffsetForCurrentAvatar();

            avatarScaleChanged?.Invoke(scale);
        }

        private void UpdateFloorOffsetForCurrentAvatar()
        {
            if (_settings.floorHeightAdjust == FloorHeightAdjust.Off || !currentlySpawnedAvatar)
            {
                _floorController.SetFloorOffset(0);

                return;
            }

            float floorOffset = _beatSaberUtilities.GetRoomAdjustedPlayerEyeHeight() - currentlySpawnedAvatar.scaledEyeHeight;

            _floorController.SetFloorOffset(floorOffset);
        }

        private void UpdateFirstPersonVisibility()
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

        private void UpdateLocomotionEnabled()
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

        private void OnFloorPositionChanged(float verticalPosition)
        {
            SetAvatarVerticalPosition(verticalPosition);
        }

        private void SetAvatarVerticalPosition(float verticalPosition)
        {
            _avatarContainer.transform.position = new Vector3(0, verticalPosition, 0);
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
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
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

                using (var stream = new FileStream(kAvatarInfoCacheFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
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

                    stream.SetLength(stream.Position);
                }

                File.SetAttributes(kAvatarInfoCacheFilePath, FileAttributes.Hidden);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to save avatar info cache");
                _logger.Error(ex);
            }
        }
    }
}
