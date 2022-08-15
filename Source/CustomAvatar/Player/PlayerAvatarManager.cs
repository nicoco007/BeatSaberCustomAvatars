//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
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

        /// <summary>
        /// Delegate for <see cref="avatarLoading"/>.
        /// </summary>
        /// <param name="fullPath">Full path of the avatar file.</param>
        /// <param name="name">If the avatar was previously loaded successfully and cached, the name of the avatar. If not, the avatar file's name without extension.</param>
        public delegate void AvatarLoadingDelegate(string fullPath, string name);

        /// <summary>
        /// The player's currently spawned avatar. This can be null.
        /// </summary>
        public SpawnedAvatar currentlySpawnedAvatar { get; private set; }

        /// <summary>
        /// Event triggered when the current avatar is deleted an a new one starts loading. Note that the argument may be null if no avatar was selected to replace the previous one.
        /// </summary>
        [Obsolete("Use the avatarLoading event instead")]
        public event Action<string> avatarStartedLoading;

        /// <summary>
        /// Event triggered when the current avatar is deleted an a new one starts loading. Note that both arguments may be null if no avatar was selected to replace the previous one.
        /// </summary>
        public event AvatarLoadingDelegate avatarLoading;

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

        private readonly DiContainer _container;
        private readonly ILogger<PlayerAvatarManager> _logger;
        private readonly AvatarLoader _avatarLoader;
        private readonly Settings _settings;
        private readonly AvatarSpawner _spawner;
        private readonly BeatSaberUtilities _beatSaberUtilities;
        private readonly ActivePlayerSpaceManager _activePlayerSpaceManager;

        private readonly Dictionary<string, AvatarInfo> _avatarInfos = new Dictionary<string, AvatarInfo>();

        private string _switchingToPath;
        private Settings.AvatarSpecificSettings _currentAvatarSettings;
        private GameObject _avatarContainer;
        private CancellationTokenSource _avatarLoadCancellationTokenSource;

        internal PlayerAvatarManager(DiContainer container, ILogger<PlayerAvatarManager> logger, AvatarLoader avatarLoader, Settings settings, AvatarSpawner spawner, BeatSaberUtilities beatSaberUtilities, ActivePlayerSpaceManager activePlayerSpaceManager)
        {
            _container = container;
            _logger = logger;
            _avatarLoader = avatarLoader;
            _settings = settings;
            _spawner = spawner;
            _beatSaberUtilities = beatSaberUtilities;
            _activePlayerSpaceManager = activePlayerSpaceManager;
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
                _logger.LogError($"Failed to create folder '{kCustomAvatarsPath}'");
                _logger.LogError(ex);
            }

            _settings.moveFloorWithRoomAdjust.changed += OnMoveFloorWithRoomAdjustChanged;
            _settings.resizeMode.changed += OnResizeModeChanged;
            _settings.floorHeightAdjust.changed += OnFloorHeightAdjustChanged;
            _settings.isAvatarVisibleInFirstPerson.changed += OnAvatarVisibleInFirstPersonChanged;
            _settings.playerArmSpan.changed += OnPlayerArmSpanChanged;
            _settings.enableLocomotion.changed += OnEnableLocomotionChanged;

            _beatSaberUtilities.roomAdjustChanged += OnRoomAdjustChanged;
            _beatSaberUtilities.playerHeightChanged += OnPlayerHeightChanged;

            _activePlayerSpaceManager.changed += OnActivePlayerSpaceChanged;

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
            _beatSaberUtilities.playerHeightChanged -= OnPlayerHeightChanged;

            _activePlayerSpaceManager.changed -= OnActivePlayerSpaceChanged;

            SaveAvatarInfosToFile();
        }

        internal bool TryGetCachedAvatarInfo(string fileName, out AvatarInfo avatarInfo)
        {
            return _avatarInfos.TryGetValue(fileName, out avatarInfo);
        }

        internal async Task<AvatarInfo> GetAvatarInfo(string fileName, IProgress<float> progress, bool forceReload)
        {
            string fullPath = Path.Combine(kCustomAvatarsPath, fileName);

            if (!forceReload && _avatarInfos.ContainsKey(fileName) && _avatarInfos[fileName].IsForFile(fullPath))
            {
                _logger.LogTrace($"Using cached information for '{fileName}'");
                progress.Report(1);
            }
            else
            {
                await LoadAndCacheAvatarAsync(fullPath, progress, CancellationToken.None);
            }

            return _avatarInfos[fileName];
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
                _logger.LogWarning("Previously loaded avatar no longer exists");
                return Task.CompletedTask;
            }

            return SwitchToAvatarAsync(previousAvatarFileName, null);
        }

        public async Task SwitchToAvatarAsync(string fileName, IProgress<float> progress)
        {
            if (currentlySpawnedAvatar && currentlySpawnedAvatar.prefab) Object.Destroy(currentlySpawnedAvatar.prefab.gameObject);
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

            _avatarInfos.TryGetValue(fileName, out AvatarInfo cachedInfo);

            avatarStartedLoading?.Invoke(fullPath);
            avatarLoading?.Invoke(fullPath, !string.IsNullOrWhiteSpace(cachedInfo.name) ? cachedInfo.name : fileName);

            try
            {
                if (_avatarLoadCancellationTokenSource != null)
                {
                    _avatarLoadCancellationTokenSource.Cancel();
                }

                _avatarLoadCancellationTokenSource = new CancellationTokenSource();
                AvatarPrefab avatarPrefab = await _avatarLoader.LoadFromFileAsync(fullPath, progress, _avatarLoadCancellationTokenSource.Token);
                SwitchToAvatar(avatarPrefab);
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace($"Canceled loading of '{fullPath}'");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load '{fullPath}'");
                _logger.LogError(ex);

                avatarLoadFailed?.Invoke(ex);
            }
        }

        private async Task<AvatarPrefab> LoadAndCacheAvatarAsync(string fullPath, IProgress<float> progress, CancellationToken cancellationToken)
        {
            AvatarPrefab avatar = await _avatarLoader.LoadFromFileAsync(fullPath, progress, cancellationToken);

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

        private void SwitchToAvatar(AvatarPrefab avatar)
        {
            if (!avatar)
            {
                _logger.LogInformation("No avatar selected");
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

            if (currentlySpawnedAvatar)
            {
                Object.Destroy(currentlySpawnedAvatar.gameObject);
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

            await SwitchToAvatarAsync(files[index], null);
        }

        public async Task SwitchToPreviousAvatarAsync()
        {
            List<string> files = GetAvatarFileNames();
            files.Insert(0, null);

            int index = !string.IsNullOrEmpty(_switchingToPath) ? files.IndexOf(Path.GetFileName(_switchingToPath)) : 0;

            index = (index + files.Count - 1) % files.Count;

            await SwitchToAvatarAsync(files[index], null);
        }

        internal float GetFloorOffset()
        {
            if (_settings.floorHeightAdjust == FloorHeightAdjustMode.Off || !currentlySpawnedAvatar) return 0;

            return _beatSaberUtilities.GetRoomAdjustedPlayerEyeHeight() - currentlySpawnedAvatar.scaledEyeHeight;
        }

        internal List<string> GetAvatarFileNames()
        {
            if (!Directory.Exists(kCustomAvatarsPath)) return new List<string>();

            return Directory.GetFiles(kCustomAvatarsPath, "*.avatar", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f)).OrderBy(f => f).ToList();
        }

        private void OnActivePlayerSpaceChanged(Transform playerSpace)
        {
            _avatarContainer.transform.SetParent(playerSpace, false);

            if (playerSpace == null)
            {
                Object.DontDestroyOnLoad(_avatarContainer);
            }
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
                _logger.LogWarning("Calculated scale is <= 0; reverting to 1");
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

        private void LoadAvatarInfosFromFile()
        {
            if (!File.Exists(kAvatarInfoCacheFilePath)) return;

            try
            {
                _logger.LogInformation($"Loading cached avatar info from '{kAvatarInfoCacheFilePath}'");

                using (var stream = new FileStream(kAvatarInfoCacheFilePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (!reader.ReadBytes(kCacheFileSignature.Length).SequenceEqual(kCacheFileSignature))
                    {
                        _logger.LogWarning($"Invalid cache file magic");
                        return;
                    }

                    if (reader.ReadByte() != kCacheFileVersion)
                    {
                        _logger.LogWarning($"Invalid cache file version");
                        return;
                    }

                    int count = reader.ReadInt32();

                    _logger.LogTrace($"Reading {count} cached infos");

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
                            _logger.LogError($"Invalid avatar file name '{avatarInfo.fileName}'");
                            continue;
                        }

                        string fullPath = Path.Combine(kCustomAvatarsPath, avatarInfo.fileName);

                        if (!File.Exists(fullPath))
                        {
                            _logger.LogNotice($"File '{avatarInfo.fileName}' no longer exists; skipped");
                            continue;
                        }

                        if (!avatarInfo.IsForFile(fullPath))
                        {
                            _logger.LogNotice($"Info for '{avatarInfo.fileName}' is outdated; skipped");
                            continue;
                        }

                        _logger.LogTrace($"Got cached info for '{avatarInfo.fileName}'");

                        if (_avatarInfos.ContainsKey(avatarInfo.fileName))
                        {
                            if (_avatarInfos[avatarInfo.fileName].timestamp > avatarInfo.timestamp)
                            {
                                _logger.LogNotice($"Current info for '{avatarInfo.fileName}' is more recent; skipped");
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
                _logger.LogError("Failed to load cached avatar info");
                _logger.LogError(ex);
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
                _logger.LogInformation($"Saving avatar info cache to '{kAvatarInfoCacheFilePath}'");

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
                _logger.LogError("Failed to save avatar info cache");
                _logger.LogError(ex);
            }
        }
    }
}
