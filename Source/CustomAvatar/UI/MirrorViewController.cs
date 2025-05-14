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
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BGLib.Polyglot;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using CustomAvatar.Tracking;
using HMUI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CustomAvatar.UI
{
    [ViewDefinition("CustomAvatar.UI.Views.Mirror.bsml")]
    [HotReload(RelativePathToLayout = "Views/Mirror.bsml")]
    internal class MirrorViewController : BSMLAutomaticViewController
    {
        private Settings _settings;
        private PlayerAvatarManager _avatarManager;
        private PlatformLeaderboardViewController _platformLeaderboardViewController;
        private TrackingRig _trackingRig;

        private bool _isLoaderActive;
        private string _errorMessage;

        private GameObject _progressObject;
        private Image _progressBar;
        private TextMeshProUGUI _progressTitle;
        private TextMeshProUGUI _progressText;

        private IMirrorProvider _currentMirrorProvider;
        private FakeMirrorProvider _fakeMirrorProvider;
        private RealMirrorProvider _realMirrorProvider;

        protected bool isLoaderActive
        {
            get => _isLoaderActive;
            set
            {
                _isLoaderActive = value;
                NotifyPropertyChanged();
            }
        }

        protected string errorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value ?? string.Empty;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(isErrorMessageVisible));
            }
        }

        protected bool isErrorMessageVisible => !string.IsNullOrEmpty(errorMessage);

        protected string calibrationMessage => _trackingRig.activeCalibrationMode switch
        {
            CalibrationMode.Automatic => "Stand up straight with your whole body and head facing the same direction.\nPress both triggers simultaneously to save.",
            CalibrationMode.Manual => "Align your body with the avatar and\npress both triggers simultaneously to save.",
            _ => null,
        };

        protected bool isCalibrationMessageVisible => _trackingRig.activeCalibrationMode != CalibrationMode.None;

        [Inject]
        [UsedImplicitly]
        private void Construct(
            DiContainer container,
            MirrorHelper mirrorHelper,
            Settings settings,
            SettingsManager settingsManager,
            PlayerAvatarManager avatarManager,
            HierarchyManager hierarchyManager,
            PlatformLeaderboardViewController platformLeaderboardViewController,
            TrackingRig trackingRig)
        {
            _settings = settings;
            _avatarManager = avatarManager;
            _platformLeaderboardViewController = platformLeaderboardViewController;
            _trackingRig = trackingRig;
            _fakeMirrorProvider = new FakeMirrorProvider();
            _realMirrorProvider = new RealMirrorProvider(container, mirrorHelper, settings, settingsManager, hierarchyManager);
        }

        internal void UpdateProgress(float progress)
        {
            _progressBar.fillAmount = progress;
            _progressText.text = $"{progress * 100:0}%";
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            _avatarManager.avatarLoading += OnAvatarLoading;
            _avatarManager.avatarChanged += OnAvatarChanged;
            _avatarManager.avatarLoadFailed += OnAvatarLoadFailed;

            _settings.mirror.useFakeMirrorBeta.changed += OnUseFakeMirrorChanged;

            _trackingRig.activeCalibrationModeChanged += OnActiveCalibrationModeChanged;

            if (addedToHierarchy)
            {
                _realMirrorProvider.Initialize();
            }

            if (firstActivation)
            {
                CreateProgressBar();
            }

            SetLoading(false);
            OnUseFakeMirrorChanged(_settings.mirror.useFakeMirrorBeta);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            _currentMirrorProvider?.HideAvatar();
            _currentMirrorProvider?.Disable();

            if (removedFromHierarchy)
            {
                _realMirrorProvider.Destroy();
            }

            _avatarManager.avatarLoading -= OnAvatarLoading;
            _avatarManager.avatarChanged -= OnAvatarChanged;
            _avatarManager.avatarLoadFailed -= OnAvatarLoadFailed;

            _settings.mirror.useFakeMirrorBeta.changed -= OnUseFakeMirrorChanged;

            _trackingRig.activeCalibrationModeChanged += OnActiveCalibrationModeChanged;
        }

        private void CreateProgressBar()
        {
            RectTransform containerTransform = (RectTransform)Instantiate(_platformLeaderboardViewController.transform.Find("Container/LeaderboardTableView/LoadingControl/DownloadingContainer"));
            containerTransform.SetParent(gameObject.transform, false);
            containerTransform.name = "ProgressContainer";
            containerTransform.anchorMin = new Vector2(0.3f, 0.5f);
            containerTransform.anchorMax = new Vector2(0.7f, 0.5f);

            GameObject containerGameObject = containerTransform.gameObject;
            _progressObject = containerGameObject;

            RectTransform progressBarTransform = (RectTransform)containerTransform.Find("DownloadingProgress");
            progressBarTransform.name = "ProgressBar";
            _progressBar = progressBarTransform.GetComponent<Image>();

            RectTransform progressBackgroundTransform = (RectTransform)containerTransform.Find("DownloadingBG");
            progressBackgroundTransform.name = "ProgressBG";
            Image progressBackgroundImage = progressBackgroundTransform.GetComponent<Image>();
            progressBackgroundImage.color = new Color(1, 1, 1, 0.2f);

            RectTransform progressTitleTransform = (RectTransform)containerTransform.Find("DownloadingText");
            progressTitleTransform.name = "ProgressTitle";
            Destroy(progressTitleTransform.GetComponent<LocalizedTextMeshProUGUI>());
            _progressTitle = progressTitleTransform.GetComponent<TextMeshProUGUI>();

            // CurvedTextMeshPro doesn't save fontSize properly when inactive
            containerGameObject.SetActive(true);

            GameObject progressTextObject = new("ProgressText", typeof(RectTransform));
            RectTransform progressTextTransform = (RectTransform)progressTextObject.transform;
            progressTextTransform.SetParent(containerTransform, false);
            progressTextTransform.anchorMin = new Vector2(1, 0.5f);
            progressTextTransform.anchorMax = new Vector2(0, 0.5f);
            progressTextTransform.anchoredPosition = new Vector2(0, -4);
            _progressText = progressTextObject.AddComponent<CurvedTextMeshPro>();
            _progressText.fontMaterial = _progressTitle.fontMaterial;
            _progressText.fontSize = 3;
            _progressText.alignment = TextAlignmentOptions.Center;
            _progressText.enableWordWrapping = false;

            containerGameObject.SetActive(false);
        }

        private void OnAvatarLoading(string filePath, string name)
        {
            _progressTitle.text = $"Loading {name}";
            SetLoading(true);
            _currentMirrorProvider.HideAvatar();
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            SetLoading(false);
            _currentMirrorProvider.ShowAvatar(avatar);
        }

        private void OnAvatarLoadFailed(Exception exception)
        {
            SetLoading(false);

            errorMessage = $"Failed to load selected avatar\n<size=75%>{exception.Message}</size>";
        }

        private void OnUseFakeMirrorChanged(bool value)
        {
            _currentMirrorProvider?.HideAvatar();
            _currentMirrorProvider?.Disable();

            _currentMirrorProvider = value ? _fakeMirrorProvider : _realMirrorProvider;

            _currentMirrorProvider.Enable();
            _currentMirrorProvider.ShowAvatar(_avatarManager.currentlySpawnedAvatar);
        }

        private void OnActiveCalibrationModeChanged(CalibrationMode calibrationMode)
        {
            NotifyPropertyChanged(nameof(isCalibrationMessageVisible));
            NotifyPropertyChanged(nameof(calibrationMessage));
        }

        private void SetLoading(bool loading)
        {
            _progressObject.SetActive(loading);
            errorMessage = null;
        }

        [UsedImplicitly]
        private void OnCancelButtonClicked()
        {
            _trackingRig.EndCalibration();
        }
    }
}
