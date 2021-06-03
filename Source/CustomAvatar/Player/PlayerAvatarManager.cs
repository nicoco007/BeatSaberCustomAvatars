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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.HarmonyPatches;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using IPA.Utilities;
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
        public static readonly string kCustomAvatarsPath = Path.Combine(UnityGame.InstallPath, "CustomAvatars");
        public static readonly string kAvatarInfoCacheFilePath = Path.Combine(kCustomAvatarsPath, "cache.dat");
        public static readonly byte[] kCacheFileSignature = { 0x43, 0x41, 0x64, 0x62 }; // Custom Avatars Database (CAdb)
        public static readonly byte kCacheFileVersion = 2;

        private const int kMaxNumberOfConcurrentLoadingTasks = 4;

        /// <summary>
        /// The player's currently spawned avatar. This can be null.
        /// </summary>
        public SpawnedAvatar currentlySpawnedAvatar { get; private set; }

        public Transform parent => _avatarContainer.transform.parent;

        /// <summary>
        /// Event triggered when the current avatar is deleted an a new one starts loading. Note that the argument may be null if no avatar was selected to replace the previous one.
        /// </summary>
        public event Action<string> avatarStartedLoading;

        /// <summary>
        /// Event triggered when a new avatar has finished loading and is spawned. Note that the argument may be null if no avatar was selected to replace the previous one.
        /// </summary>
        public event Action<SpawnedAvatar> avatarChanged;

        /// <summary>
        /// Event triggered when the selected avatar has failed to load.
        /// </summary>
        public event Action<Exception> avatarLoadFailed;

        /// <summary>
        /// Event triggered when the selected avatar's scale changes.
        /// </summary>
        public event Action<float> avatarScaleChanged;

        internal event Action<AvatarInfo> avatarAdded;
        internal event Action<AvatarInfo> avatarRemoved;

        private readonly DiContainer _container;
        private readonly ILogger<PlayerAvatarManager> _logger;
        private readonly AvatarLoader _avatarLoader;
        private readonly Settings _settings;
        private readonly AvatarSpawner _spawner;
        private readonly BeatSaberUtilities _beatSaberUtilities;

        private readonly Dictionary<string, AvatarInfo> _avatarInfos = new Dictionary<string, AvatarInfo>();

        private FileSystemWatcher _fileSystemWatcher;
        private string _switchingToPath;
        private Settings.AvatarSpecificSettings _currentAvatarSettings;
        private GameObject _avatarContainer;

        internal PlayerAvatarManager(DiContainer container, ILogger<PlayerAvatarManager> logger, AvatarLoader avatarLoader, Settings settings, AvatarSpawner spawner, BeatSaberUtilities beatSaberUtilities)
        {
            _container = container;
            _logger = logger;
            _avatarLoader = avatarLoader;
            _settings = settings;
            _spawner = spawner;
            _beatSaberUtilities = beatSaberUtilities;
        }

        public void Initialize()
        {
            try
            {
                if (!Directory.Exists(kCustomAvatarsPath))
                {
                    Directory.CreateDirectory(kCustomAvatarsPath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create folder '{kCustomAvatarsPath}'");
                _logger.Error(ex);
            }

            try
            {
                _fileSystemWatcher = new FileSystemWatcher(kCustomAvatarsPath, "*.avatar")
                {
                    NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
                };

                _fileSystemWatcher.Changed += OnAvatarFileChanged;
                _fileSystemWatcher.Created += OnAvatarFileCreated;
                _fileSystemWatcher.Deleted += OnAvatarFileDeleted;

                _fileSystemWatcher.EnableRaisingEvents = true;

                _logger.Trace($"Watching files in '{_fileSystemWatcher.Path}' ('{_fileSystemWatcher.Filter}')");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to create FileSystemWatcher");
                _logger.Error(ex);
            }

            _settings.moveFloorWithRoomAdjust.changed += OnMoveFloorWithRoomAdjustChanged;
            _settings.resizeMode.changed += OnResizeModeChanged;
            _settings.floorHeightAdjust.changed += OnFloorHeightAdjustChanged;
            _settings.isAvatarVisibleInFirstPerson.changed += OnAvatarVisibleInFirstPersonChanged;
            _settings.playerArmSpan.changed += OnPlayerArmSpanChanged;
            _settings.enableLocomotion.changed += OnEnableLocomotionChanged;

            _beatSaberUtilities.roomAdjustChanged += OnRoomAdjustChanged;

            PlayerData_playerSpecificSettings.playerHeightChanged += OnPlayerHeightChanged;

            _avatarContainer = new GameObject("Avatar Container");
            Object.DontDestroyOnLoad(_avatarContainer);

            LoadAvatarInfosFromFile();
            LoadAvatarFromSettingsAsync();
        }

        public void Dispose()
        {
            if (currentlySpawnedAvatar) Object.Destroy(currentlySpawnedAvatar.prefab.gameObject);
            Object.Destroy(_avatarContainer);

            _settings.moveFloorWithRoomAdjust.changed -= OnMoveFloorWithRoomAdjustChanged;
            _settings.resizeMode.changed -= OnResizeModeChanged;
            _settings.floorHeightAdjust.changed -= OnFloorHeightAdjustChanged;
            _settings.isAvatarVisibleInFirstPerson.changed -= OnAvatarVisibleInFirstPersonChanged;
            _settings.playerArmSpan.changed -= OnPlayerArmSpanChanged;
            _settings.enableLocomotion.changed -= OnEnableLocomotionChanged;

            _beatSaberUtilities.roomAdjustChanged -= OnRoomAdjustChanged;

            PlayerData_playerSpecificSettings.playerHeightChanged -= OnPlayerHeightChanged;

            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.Changed -= OnAvatarFileChanged;
                _fileSystemWatcher.Created -= OnAvatarFileCreated;
                _fileSystemWatcher.Deleted -= OnAvatarFileDeleted;

                _fileSystemWatcher.Dispose();
            }

            SaveAvatarInfosToFile();
        }

        internal async Task<List<AvatarInfo>> GetAvatarInfosAsync(bool forceReload = false)
        {
            List<string> fileNames = GetAvatarFileNames();

            foreach (string existingFile in _avatarInfos.Keys.ToList())
            {
                if (!fileNames.Contains(existingFile))
                {
                    _avatarInfos.Remove(existingFile);
                }
            }

            if (forceReload)
            {
                string fullPath = currentlySpawnedAvatar ? currentlySpawnedAvatar.prefab.fullPath : null;
                _ = SwitchToAvatarAsync(null);
                _switchingToPath = fullPath;
            }

            var tasks = new List<Task>();

            using (var semaphore = new SemaphoreSlim(kMaxNumberOfConcurrentLoadingTasks))
            {
                foreach (string fileName in fileNames)
                {
                    await semaphore.WaitAsync();

                    string fullPath = Path.Combine(kCustomAvatarsPath, fileName);

                    if (!forceReload && _avatarInfos.ContainsKey(fileName) && _avatarInfos[fileName].IsForFile(fullPath))
                    {
                        _logger.Trace($"Using cached information for '{fileName}'");
                        semaphore.Release();
                    }
                    else
                    {
                        tasks.Add(new Func<Task>(async () =>
                        {
                            try
                            {
                                await LoadAvatarAsync(fullPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error($"Failed to load avatar '{fullPath}'");
                                _logger.Error(ex);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        })());
                    }
                }

                await Task.WhenAll(tasks);
            }

            return _avatarInfos.Values.ToList();
        }

        public Task LoadAvatarFromSettingsAsync()
        {
            string previousAvatarFileName = _settings.previousAvatarPath;

            if (string.IsNullOrEmpty(previousAvatarFileName))
            {
                return Task.CompletedTask;
            }

            if (!File.Exists(Path.Combine(kCustomAvatarsPath, previousAvatarFileName)))
            {
                _logger.Warning("Previously loaded avatar no longer exists");
                return Task.CompletedTask;
            }

            return SwitchToAvatarAsync(previousAvatarFileName);
        }

        public async Task SwitchToAvatarAsync(string fileName)
        {
            if (currentlySpawnedAvatar) Object.Destroy(currentlySpawnedAvatar.prefab.gameObject);
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

            try
            {
                AvatarPrefab avatarPrefab = await _avatarLoader.LoadFromFileAsync(fullPath);
                SwitchToAvatar(avatarPrefab);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load selected avatar");
                _logger.Error(ex);

                avatarLoadFailed?.Invoke(ex);
            }
        }

        private async Task<AvatarPrefab> LoadAvatarAsync(string fullPath)
        {
            AvatarPrefab avatar = await _avatarLoader.LoadFromFileAsync(fullPath);

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

            avatarAdded?.Invoke(info);

            if (avatar.fullPath == _switchingToPath)
            {
                SwitchToAvatar(avatar);
            }
            else
            {
                Object.Destroy(avatar.gameObject);
            }

            return avatar;
        }

        private async void OnAvatarFileChanged(object sender, FileSystemEventArgs e)
        {
            _logger.Trace($"File change detected: '{e.FullPath}'");

            if (currentlySpawnedAvatar && e.FullPath == currentlySpawnedAvatar.prefab.fullPath)
            {
                _logger.Info("Reloading spawned avatar");
                await SwitchToAvatarAsync(e.Name);
            }
            else
            {
                _logger.Info($"Reloading avatar info for '{e.FullPath}'");
                AvatarPrefab avatarPrefab = await LoadAvatarAsync(e.FullPath);
                Object.Destroy(avatarPrefab.gameObject);
            }
        }

        private async void OnAvatarFileCreated(object sender, FileSystemEventArgs e)
        {
            _logger.Info($"Loading avatar info for '{e.FullPath}'");
            AvatarPrefab avatarPrefab = await LoadAvatarAsync(e.FullPath);
            Object.Destroy(avatarPrefab.gameObject);
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

        private void SwitchToAvatar(AvatarPrefab avatar)
        {
            if (!avatar)
            {
                _logger.Info("No avatar selected");
                if (_currentAvatarSettings != null) _currentAvatarSettings.ignoreExclusions.changed -= OnIgnoreFirstPersonExclusionsChanged;
                _currentAvatarSettings = null;
                avatarChanged?.Invoke(null);
                _settings.previousAvatarPath = null;
                UpdateAvatarVerticalPosition();
                return;
            }
            else if ((currentlySpawnedAvatar && currentlySpawnedAvatar.prefab == avatar) || avatar.fullPath != _switchingToPath)
            {
                Object.Destroy(avatar.gameObject);
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

            if (_currentAvatarSettings != null) _currentAvatarSettings.ignoreExclusions.changed -= OnIgnoreFirstPersonExclusionsChanged;
            currentlySpawnedAvatar = _spawner.SpawnAvatar(avatar, _container.Resolve<VRPlayerInputInternal>(), _avatarContainer.transform);
            _currentAvatarSettings = _settings.GetAvatarSettings(avatar.fileName);
            _currentAvatarSettings.ignoreExclusions.changed += OnIgnoreFirstPersonExclusionsChanged;

            ResizeCurrentAvatar();
            UpdateFirstPersonVisibility();
            UpdateLocomotionEnabled();

            avatarChanged?.Invoke(currentlySpawnedAvatar);
        }

        public async Task SwitchToNextAvatarAsync()
        {
            List<string> files = GetAvatarFileNames();
            files.Insert(0, null);

            int index = !string.IsNullOrEmpty(_switchingToPath) ? files.IndexOf(Path.GetFileName(_switchingToPath)) : 0;

            index = (index + 1) % files.Count;

            await SwitchToAvatarAsync(files[index]);
        }

        public async Task SwitchToPreviousAvatarAsync()
        {
            List<string> files = GetAvatarFileNames();
            files.Insert(0, null);

            int index = !string.IsNullOrEmpty(_switchingToPath) ? files.IndexOf(Path.GetFileName(_switchingToPath)) : 0;

            index = (index + files.Count - 1) % files.Count;

            await SwitchToAvatarAsync(files[index]);
        }

        internal void SetParent(Transform parent)
        {
            _avatarContainer.transform.SetParent(parent, false);

            // transform is moved to parent's scene so we need to mark it as non-destructible again
            if (parent)
            {
                _logger.Trace($"Parented avatar container to '{parent.name}' (scene '{parent.gameObject.scene.name}')");
            }
            else
            {
                _logger.Warning($"Parented avatar container to nothing!");
                Object.DontDestroyOnLoad(_avatarContainer);
            }
        }

        internal float GetFloorOffset()
        {
            if (_settings.floorHeightAdjust == FloorHeightAdjustMode.Off || !currentlySpawnedAvatar) return 0;

            return _beatSaberUtilities.GetRoomAdjustedPlayerEyeHeight() - currentlySpawnedAvatar.scaledEyeHeight;
        }

        private void OnResizeModeChanged(AvatarResizeMode resizeMode)
        {
            ResizeCurrentAvatar();
        }

        private void OnFloorHeightAdjustChanged(FloorHeightAdjustMode floorHeightAdjust)
        {
            ResizeCurrentAvatar();
        }

        private void OnAvatarVisibleInFirstPersonChanged(bool visible)
        {
            UpdateFirstPersonVisibility();
        }

        private void OnIgnoreFirstPersonExclusionsChanged(bool ignore)
        {
            UpdateFirstPersonVisibility();
        }

        private void OnRoomAdjustChanged(Vector3 roomCenter, Quaternion quaternion)
        {
            UpdateAvatarVerticalPosition();
            UpdateLocomotionEnabled();
        }

        private void ResizeCurrentAvatar()
        {
            if (!currentlySpawnedAvatar || !currentlySpawnedAvatar.prefab.descriptor.allowHeightCalibration) return;

            float scale;
            AvatarResizeMode resizeMode = _settings.resizeMode;

            switch (resizeMode)
            {
                case AvatarResizeMode.ArmSpan:
                    float avatarArmLength = currentlySpawnedAvatar.prefab.armSpan;

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
                    float avatarEyeHeight = currentlySpawnedAvatar.prefab.eyeHeight;
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

            UpdateAvatarVerticalPosition();

            avatarScaleChanged?.Invoke(scale);
        }

        private void UpdateFirstPersonVisibility()
        {
            if (!currentlySpawnedAvatar) return;

            FirstPersonVisibility visibility = FirstPersonVisibility.Hidden;

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

        private void OnEnableLocomotionChanged(bool enable)
        {
            UpdateLocomotionEnabled();
        }

        private void UpdateLocomotionEnabled()
        {
            if (currentlySpawnedAvatar && currentlySpawnedAvatar.TryGetComponent(out AvatarIK ik))
            {
                ik.isLocomotionEnabled = _settings.enableLocomotion;

                currentlySpawnedAvatar.transform.localPosition = Quaternion.Inverse(_beatSaberUtilities.roomRotation) * -_beatSaberUtilities.roomCenter;
            }
        }

        private void OnMoveFloorWithRoomAdjustChanged(bool value)
        {
            ResizeCurrentAvatar();
        }

        private void OnPlayerHeightChanged(float height)
        {
            _logger.Info($"Player height set to {height} m");

            ResizeCurrentAvatar();
        }

        private void OnPlayerArmSpanChanged(float armSpan)
        {
            if (_settings.resizeMode == AvatarResizeMode.ArmSpan)
            {
                ResizeCurrentAvatar();
            }
        }

        private void UpdateAvatarVerticalPosition()
        {
            Vector3 localPosition = _avatarContainer.transform.localPosition;
            localPosition.y = GetFloorOffset();
            _avatarContainer.transform.localPosition = localPosition;

            if (!currentlySpawnedAvatar) return;

            Vector3 avatarPosition = currentlySpawnedAvatar.transform.localPosition;
            avatarPosition.y = _settings.moveFloorWithRoomAdjust ? 0 : -_beatSaberUtilities.roomCenter.y;
            currentlySpawnedAvatar.transform.localPosition = avatarPosition;
        }

        private List<string> GetAvatarFileNames()
        {
            if (!Directory.Exists(kCustomAvatarsPath)) return new List<string>();

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

                        if (string.IsNullOrWhiteSpace(avatarInfo.fileName) || Path.GetInvalidFileNameChars().Any(c => avatarInfo.fileName.Contains(c)))
                        {
                            _logger.Error($"Invalid avatar file name '{avatarInfo.fileName}'");
                            continue;
                        }

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
                            writer.Write(avatarInfo.icon ? avatarInfo.icon.texture : null, true);
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
