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
using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using HMUI;
using Polyglot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.UI
{
    internal class MirrorViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.UI.Views.Mirror.bsml";

        private StereoMirrorRenderer _mirror;

        private DiContainer _container;
        private MirrorHelper _mirrorHelper;
        private Settings _settings;
        private MainSettingsModelSO _mainSettingsModel;
        private PlayerAvatarManager _avatarManager;
        private HierarchyManager _hierarchyManager;
        private PlatformLeaderboardViewController _platformLeaderboardViewController;

        private bool _isLoaderActive;
        private string _errorMessage;

        private GameObject _progressObject;
        private Image _progressBar;
        private TextMeshProUGUI _progressTitle;
        private TextMeshProUGUI _progressText;

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

        #region Behaviour Lifecycle

        [Inject]
        internal void Construct(DiContainer container, MirrorHelper mirrorHelper, Settings settings, MainSettingsModelSO mainSettingsModel, PlayerAvatarManager avatarManager, HierarchyManager hierarchyManager, PlatformLeaderboardViewController platformLeaderboardViewController)
        {
            _container = container;
            _mirrorHelper = mirrorHelper;
            _settings = settings;
            _mainSettingsModel = mainSettingsModel;
            _avatarManager = avatarManager;
            _hierarchyManager = hierarchyManager;
            _platformLeaderboardViewController = platformLeaderboardViewController;
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

            _settings.mirror.renderScale.changed += OnMirrorRenderScaleChanged;
            _settings.mirror.antiAliasingLevel.changed += OnMirrorAntiAliasingLevelChanged;

            if (addedToHierarchy)
            {
                var mirrorSize = new Vector2(4, 2);
                _mirror = _mirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, _hierarchyManager.transform.Find("TopScreen").position.z), Quaternion.Euler(-90f, 0, 0), mirrorSize, null);

                if (!_mirror) return;

                _container.InstantiateComponent<AutoResizeMirror>(_mirror.gameObject);
            }

            if (firstActivation)
            {
                var containerTransform = (RectTransform)Instantiate(_platformLeaderboardViewController.transform.Find("Container/LeaderboardTableView/LoadingControl/DownloadingContainer"));
                containerTransform.SetParent(gameObject.transform, false);
                containerTransform.name = "ProgressContainer";
                containerTransform.anchorMin = new Vector2(0.3f, 0.5f);
                containerTransform.anchorMax = new Vector2(0.7f, 0.5f);

                GameObject containerGameObject = containerTransform.gameObject;
                _progressObject = containerGameObject;

                var progressBarTransform = (RectTransform)containerTransform.Find("DownloadingProgress");
                progressBarTransform.name = "ProgressBar";
                _progressBar = progressBarTransform.GetComponent<Image>();

                var progressBackgroundTransform = (RectTransform)containerTransform.Find("DownloadingBG");
                progressBackgroundTransform.name = "ProgressBG";
                Image progressBackgroundImage = progressBackgroundTransform.GetComponent<Image>();
                progressBackgroundImage.color = new Color(1, 1, 1, 0.2f);

                var progressTitleTransform = (RectTransform)containerTransform.Find("DownloadingText");
                progressTitleTransform.name = "ProgressTitle";
                Destroy(progressTitleTransform.GetComponent<LocalizedTextMeshProUGUI>());
                _progressTitle = progressTitleTransform.GetComponent<TextMeshProUGUI>();

                // CurvedTextMeshPro doesn't save fontSize properly when inactive
                containerGameObject.SetActive(true);

                var progressTextObject = new GameObject("ProgressText", typeof(RectTransform));
                var progressTextTransform = (RectTransform)progressTextObject.transform;
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

            SetLoading(false);
            OnMirrorRenderScaleChanged(_settings.mirror.renderScale);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            if (removedFromHierarchy && _mirror)
            {
                Destroy(_mirror.gameObject);
            }

            _avatarManager.avatarLoading -= OnAvatarLoading;
            _avatarManager.avatarChanged -= OnAvatarChanged;
            _avatarManager.avatarLoadFailed -= OnAvatarLoadFailed;

            _settings.mirror.renderScale.changed -= OnMirrorRenderScaleChanged;
            _settings.mirror.antiAliasingLevel.changed -= OnMirrorAntiAliasingLevelChanged;
        }

        #endregion

        private void OnAvatarLoading(string filePath, string name)
        {
            _progressTitle.text = $"Loading {name}";
            SetLoading(true);
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            SetLoading(false);
        }

        private void OnAvatarLoadFailed(Exception exception)
        {
            SetLoading(false);

            errorMessage = $"Failed to load selected avatar\n<size=75%>{exception.Message}</size>";
        }

        private void OnMirrorRenderScaleChanged(float renderScale)
        {
            UpdateMirrorRenderSettings(renderScale, _settings.mirror.antiAliasingLevel);
        }

        private void OnMirrorAntiAliasingLevelChanged(int antiAliasingLevel)
        {
            UpdateMirrorRenderSettings(_settings.mirror.renderScale, antiAliasingLevel);
        }

        private void UpdateMirrorRenderSettings(float scale, int antiAliasingLevel)
        {
            if (!_mirror) return;

            _mirror.renderScale = scale * _mainSettingsModel.vrResolutionScale;
            _mirror.antiAliasing = antiAliasingLevel;
        }

        private void SetLoading(bool loading)
        {
            _progressObject.SetActive(loading);
            errorMessage = null;
        }

        private class AutoResizeMirror : EnvironmentObject
        {
            protected override void UpdateOffset()
            {
                float floorOffset = playerAvatarManager.GetFloorOffset();

                if (settings.moveFloorWithRoomAdjust)
                {
                    floorOffset += beatSaberUtilities.roomCenter.y;
                }

                float scale = transform.localPosition.z / 2.6f; // screen system scale
                float width = 2.5f + scale;
                float height = 2f + 0.5f * scale - floorOffset;

                transform.localPosition = new Vector3(transform.localPosition.x, floorOffset + height / 2, transform.localPosition.z);
                transform.localScale = new Vector3(width / 10, 1, height / 10);
            }
        }
    }
}
