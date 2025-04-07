//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.Animations;
using Zenject;

namespace CustomAvatar.Player
{
    /// <summary>
    /// Manages the player's local avatar.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerAvatarManager : MonoBehaviour
    {
        public static readonly string kCustomAvatarsPath = Path.Join(UnityGame.InstallPath, "CustomAvatars");

        private static readonly List<ConstraintSource> kEmptyConstraintSources = new(0);

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
        /// The current avatar's scale.
        /// </summary>

        public float scale
        {
            get => _scaleConstraint.scaleOffset;
            private set => _scaleConstraint.scaleOffset = value;
        }

        /// <summary>
        /// The current avatar's scaled eye height.
        /// </summary>
        public float scaledEyeHeight => currentlySpawnedAvatar != null ? scale * currentlySpawnedAvatar.prefab.eyeHeight : BeatSaberUtilities.kDefaultPlayerEyeHeight;

        internal Settings.AvatarSpecificSettings currentAvatarSettings { get; private set; }

        internal CalibrationData.FullBodyCalibration currentManualCalibration { get; private set; }

        internal string currentAvatarFileName
        {
            get => _settings.previousAvatarPath;
            private set
            {
                _settings.previousAvatarPath = value;
            }
        }

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

        private readonly AvatarCacheStore _avatarCacheStore = new(kCustomAvatarsPath);

        private DiContainer _container;
        private ILogger<PlayerAvatarManager> _logger;
        private AvatarLoader _avatarLoader;
        private Settings _settings;
        private CalibrationData _calibrationData;
        private AvatarSpawner _spawner;
        private BeatSaberUtilities _beatSaberUtilities;
        private ActiveOriginManager _activeOriginManager;

        private ParentConstraint _parentConstraint;
        private LossyScaleConstraint _scaleConstraint;

        private CancellationTokenSource _avatarLoadCancellationTokenSource;

        internal bool ignoreFirstPersonExclusions
        {
            get => currentAvatarSettings?.ignoreExclusions ?? false;
            set
            {
                if (currentAvatarSettings != null)
                {
                    currentAvatarSettings.ignoreExclusions = value;
                }

                UpdateFirstPersonVisibility();
            }
        }

        protected void Awake()
        {
            _parentConstraint = gameObject.AddComponent<ParentConstraint>();
            _parentConstraint.weight = 1;
            _parentConstraint.constraintActive = true;

            _scaleConstraint = gameObject.AddComponent<LossyScaleConstraint>();
        }

        protected void OnEnable()
        {
            _settings.moveFloorWithRoomAdjust.changed += OnMoveFloorWithRoomAdjustChanged;
            _settings.resizeMode.changed += OnResizeModeChanged;
            _settings.floorHeightAdjust.changed += OnFloorHeightAdjustChanged;
            _settings.isAvatarVisibleInFirstPerson.changed += OnAvatarVisibleInFirstPersonChanged;
            _settings.playerEyeHeight.changed += OnPlayerHeightChanged;
            _settings.playerArmSpan.changed += OnPlayerArmSpanChanged;
            _settings.enableLocomotion.changed += OnEnableLocomotionChanged;

            _beatSaberUtilities.roomAdjustChanged += OnRoomAdjustChanged;

            _activeOriginManager.changed += OnActiveOriginChanged;
        }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(
            DiContainer container,
            ILogger<PlayerAvatarManager> logger,
            AvatarLoader avatarLoader,
            Settings settings,
            CalibrationData calibrationData,
            AvatarSpawner spawner,
            BeatSaberUtilities beatSaberUtilities,
            ActiveOriginManager activeOriginManager)
        {
            _container = container;
            _logger = logger;
            _avatarLoader = avatarLoader;
            _settings = settings;
            _calibrationData = calibrationData;
            _spawner = spawner;
            _beatSaberUtilities = beatSaberUtilities;
            _activeOriginManager = activeOriginManager;
        }

        protected void Start()
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

            try
            {
                _logger.LogInformation("Reading cache from " + _avatarCacheStore.cacheFilePath);
                _avatarCacheStore.Load();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }

            LoadAvatarFromSettingsAsync();
            UpdateConstraints();
        }

        protected void OnDisable()
        {
            _settings.moveFloorWithRoomAdjust.changed -= OnMoveFloorWithRoomAdjustChanged;
            _settings.resizeMode.changed -= OnResizeModeChanged;
            _settings.floorHeightAdjust.changed -= OnFloorHeightAdjustChanged;
            _settings.isAvatarVisibleInFirstPerson.changed -= OnAvatarVisibleInFirstPersonChanged;
            _settings.playerEyeHeight.changed -= OnPlayerHeightChanged;
            _settings.playerArmSpan.changed -= OnPlayerArmSpanChanged;
            _settings.enableLocomotion.changed -= OnEnableLocomotionChanged;

            _beatSaberUtilities.roomAdjustChanged -= OnRoomAdjustChanged;

            _activeOriginManager.changed -= OnActiveOriginChanged;
        }

        protected void OnDestroy()
        {
            try
            {
                _logger.LogInformation("Writing cache to " + _avatarCacheStore.cacheFilePath);
                _avatarCacheStore.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        internal bool TryGetCachedAvatarInfo(string fileName, out AvatarInfo avatarInfo)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                avatarInfo = default;
                return false;
            }

            return _avatarCacheStore.TryGetValue(fileName, out avatarInfo);
        }

        internal async Task<AvatarInfo> GetAvatarInfo(string fileName, IProgress<float> progress, bool forceReload)
        {
            string fullPath = Path.Join(kCustomAvatarsPath, fileName);

            if (!forceReload && TryGetCachedAvatarInfo(fileName, out AvatarInfo avatarInfo) && avatarInfo.IsForFile(fullPath))
            {
                _logger.LogTrace($"Using cached information for '{fileName}'");
                progress.Report(1);
            }
            else
            {
                await LoadAndCacheAvatarAsync(fullPath, progress, CancellationToken.None);
            }

            return _avatarCacheStore[fileName];
        }

        public Task LoadAvatarFromSettingsAsync()
        {
            string previousAvatarFileName = _settings.previousAvatarPath;

            if (string.IsNullOrEmpty(previousAvatarFileName))
            {
                return Task.CompletedTask;
            }

            if (!File.Exists(Path.Join(kCustomAvatarsPath, previousAvatarFileName)))
            {
                _logger.LogWarning("Previously loaded avatar no longer exists");
                return Task.CompletedTask;
            }

            return SwitchToAvatarAsync(previousAvatarFileName, null);
        }

        public async Task SwitchToAvatarAsync(string fileName, IProgress<float> progress)
        {
            _avatarLoadCancellationTokenSource?.Cancel();

            if (currentlySpawnedAvatar != null)
            {
                if (currentlySpawnedAvatar.prefab != null)
                {
                    Destroy(currentlySpawnedAvatar.prefab);
                }

                Destroy(currentlySpawnedAvatar.gameObject);
            }

            currentlySpawnedAvatar = null;
            currentAvatarSettings = null;

            if (string.IsNullOrEmpty(fileName))
            {
                currentAvatarFileName = null;
                SwitchToAvatar(null, null);
                return;
            }

            string fullPath = Path.Join(kCustomAvatarsPath, fileName);

            avatarLoading?.Invoke(fullPath, TryGetCachedAvatarInfo(fileName, out AvatarInfo cachedInfo) ? cachedInfo.name : fileName);

            try
            {
                currentAvatarFileName = fileName;
                _avatarLoadCancellationTokenSource = new CancellationTokenSource();
                AvatarPrefab avatar = await LoadAndCacheAvatarAsync(fullPath, progress, _avatarLoadCancellationTokenSource.Token);
                SwitchToAvatar(avatar, fullPath);
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

            if (cancellationToken.IsCancellationRequested)
            {
                Destroy(avatar.gameObject);
                cancellationToken.ThrowIfCancellationRequested();
            }

            AvatarInfo info = new(avatar, fullPath);

            _avatarCacheStore[info.fileName] = info;

            return avatar;
        }

        private void SwitchToAvatar(AvatarPrefab avatar, string fullPath)
        {
            if (avatar == null)
            {
                _logger.LogInformation("No avatar selected");
                avatarChanged?.Invoke(null);
                UpdateAvatarVerticalPosition();
                return;
            }

            AvatarInfo avatarInfo = new(avatar, fullPath);

            // cache avatar info since loading asset bundles is expensive
            _avatarCacheStore[avatarInfo.fileName] = avatarInfo;

            currentlySpawnedAvatar = _spawner.SpawnAvatar(avatar, _container.Resolve<VRPlayerInput>(), transform);
            currentAvatarSettings = _settings.GetAvatarSettings(avatarInfo.fileName);
            currentManualCalibration = _calibrationData.GetAvatarManualCalibration(avatarInfo.fileName);

            ResizeCurrentAvatar();
            UpdateFirstPersonVisibility();
            UpdateLocomotionEnabled();

            avatarChanged?.Invoke(currentlySpawnedAvatar);
        }

        internal async Task SwitchToNextAvatarAsync()
        {
            List<string> files = GetAvatarFileNames();
            files.Insert(0, null);

            int index = !string.IsNullOrEmpty(currentAvatarFileName) ? files.IndexOf(currentAvatarFileName) : 0;

            index = (index + 1) % files.Count;

            await SwitchToAvatarAsync(files[index], null);
        }

        internal async Task SwitchToPreviousAvatarAsync()
        {
            List<string> files = GetAvatarFileNames();
            files.Insert(0, null);

            int index = !string.IsNullOrEmpty(currentAvatarFileName) ? files.IndexOf(currentAvatarFileName) : 0;

            index = (index + files.Count - 1) % files.Count;

            await SwitchToAvatarAsync(files[index], null);
        }

        internal float GetFloorOffset() => GetFloorOffset(_settings.playerEyeHeight);

        internal float GetFloorOffset(float eyeHeight)
        {
            if (currentlySpawnedAvatar == null || _settings.floorHeightAdjust == FloorHeightAdjustMode.Off)
            {
                return 0;
            }

            float offset = eyeHeight - scaledEyeHeight;

            if (_settings.moveFloorWithRoomAdjust)
            {
                offset += _beatSaberUtilities.roomCenter.y;
            }

            return offset;
        }

        internal List<string> GetAvatarFileNames()
        {
            if (!Directory.Exists(kCustomAvatarsPath)) return new List<string>();

            return Directory.GetFiles(kCustomAvatarsPath, "*.avatar", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f)).OrderBy(f => f).ToList();
        }

        private void OnActiveOriginChanged(Transform origin)
        {
            UpdateConstraints();
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

        private void OnRoomAdjustChanged(Vector3 roomCenter, Quaternion roomRotation)
        {
            UpdateAvatarVerticalPosition();
            UpdateLocomotionEnabled();
        }

        private void UpdateConstraints()
        {
            _logger.LogTrace("Updating constraints");

            if (_activeOriginManager.current != null)
            {
                _parentConstraint.SetSources([new ConstraintSource { sourceTransform = _activeOriginManager.current, weight = 1 }]);
                _scaleConstraint.sourceTransform = _activeOriginManager.current;
            }
            else
            {
                _parentConstraint.SetSources(kEmptyConstraintSources);
                _scaleConstraint.sourceTransform = null;
            }

            UpdateAvatarVerticalPosition();
        }

        internal void ResizeCurrentAvatar()
        {
            if (currentlySpawnedAvatar == null)
            {
                return;
            }

            ResizeCurrentAvatar(_settings.playerEyeHeight);
            _logger.LogInformation("Resized avatar with scale: " + scale);
            avatarScaleChanged?.Invoke(scale);
        }

        internal void ResizeCurrentAvatar(float playerEyeHeight)
        {
            scale = CalculateAvatarScale(playerEyeHeight);
            UpdateAvatarVerticalPosition(playerEyeHeight);
        }

        private float CalculateAvatarScale(float playerEyeHeight)
        {
            if (currentlySpawnedAvatar == null)
            {
                return 1;
            }

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

            return scale;
        }

        private void UpdateFirstPersonVisibility()
        {
            if (!currentlySpawnedAvatar)
            {
                return;
            }

            FirstPersonVisibility visibility = FirstPersonVisibility.Hidden;

            if (_settings.isAvatarVisibleInFirstPerson)
            {
                visibility = ignoreFirstPersonExclusions ? FirstPersonVisibility.Visible : FirstPersonVisibility.VisibleWithExclusionsApplied;
            }

            currentlySpawnedAvatar.SetFirstPersonVisibility(visibility);
        }

        private void OnEnableLocomotionChanged(bool enable)
        {
            UpdateLocomotionEnabled();
        }

        private void UpdateLocomotionEnabled()
        {
            if (currentlySpawnedAvatar != null && currentlySpawnedAvatar.ik != null)
            {
                currentlySpawnedAvatar.ik.isLocomotionEnabled = _settings.enableLocomotion;
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

        private void UpdateAvatarVerticalPosition() => UpdateAvatarVerticalPosition(_settings.playerEyeHeight);

        private void UpdateAvatarVerticalPosition(float eyeHeight)
        {
            if (_parentConstraint.sourceCount > 0)
            {
                _parentConstraint.SetTranslationOffset(0, new Vector3(0, GetFloorOffset(eyeHeight), 0));
            }
        }
    }
}
