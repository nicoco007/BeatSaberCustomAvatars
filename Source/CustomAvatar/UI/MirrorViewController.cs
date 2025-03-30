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
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BGLib.Polyglot;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
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
        private StereoMirrorRenderer _mirror;

        private DiContainer _container;
        private MirrorHelper _mirrorHelper;
        private Settings _settings;
        private SettingsManager _settingsManager;
        private PlayerAvatarManager _avatarManager;
        private HierarchyManager _hierarchyManager;
        private PlatformLeaderboardViewController _platformLeaderboardViewController;
        private AvatarSpawner _avatarSpawner;
        private TrackingRig _trackingRig;

        private bool _isLoaderActive;
        private string _errorMessage;

        private GameObject _progressObject;
        private Image _progressBar;
        private TextMeshProUGUI _progressTitle;
        private TextMeshProUGUI _progressText;

        private GameObject _mirroredAvatarContainer;

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

        #region Behaviour Lifecycle

        [Inject]
        internal void Construct(
            DiContainer container,
            MirrorHelper mirrorHelper,
            Settings settings,
            SettingsManager settingsManager,
            PlayerAvatarManager avatarManager,
            HierarchyManager hierarchyManager,
            PlatformLeaderboardViewController platformLeaderboardViewController,
            AvatarSpawner avatarSpawner,
            TrackingRig trackingRig)
        {
            _container = container;
            _mirrorHelper = mirrorHelper;
            _settings = settings;
            _settingsManager = settingsManager;
            _avatarManager = avatarManager;
            _hierarchyManager = hierarchyManager;
            _platformLeaderboardViewController = platformLeaderboardViewController;
            _avatarSpawner = avatarSpawner;
            _trackingRig = trackingRig;
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
            _settings.mirror.useFakeMirrorBeta.changed += OnUseFakeMirrorChanged;

            _trackingRig.activeCalibrationModeChanged += OnActiveCalibrationModeChanged;

            if (addedToHierarchy)
            {
                Vector2 mirrorSize = new(4, 2);
                _mirror = _mirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, _hierarchyManager.transform.Find("TopScreen").position.z), Quaternion.Euler(-90f, 0, 0), mirrorSize, null);

                if (!_mirror) return;

                GameObject mirrorGameObject = _mirror.gameObject;
                _container.InstantiateComponent<AutoResizeMirror>(mirrorGameObject);
                mirrorGameObject.SetActive(!_settings.mirror.useFakeMirrorBeta);
            }

            if (firstActivation)
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

            SetLoading(false);
            ShowMirroredAvatar();
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
            _settings.mirror.useFakeMirrorBeta.changed -= OnUseFakeMirrorChanged;

            _trackingRig.activeCalibrationModeChanged += OnActiveCalibrationModeChanged;

            Destroy(_mirroredAvatarContainer);
        }

        #endregion

        private void OnAvatarLoading(string filePath, string name)
        {
            _progressTitle.text = $"Loading {name}";
            SetLoading(true);
            DestroyMirroredAvatar();
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            SetLoading(false);
            ShowMirroredAvatar();
        }

        private void ShowMirroredAvatar()
        {
            DestroyMirroredAvatar();

            if (!_settings.mirror.useFakeMirrorBeta)
            {
                return;
            }

            SpawnedAvatar avatar = _avatarManager.currentlySpawnedAvatar;

            if (avatar == null)
            {
                return;
            }

            _mirroredAvatarContainer = new GameObject("MirroredAvatarContainer");
            _mirroredAvatarContainer.SetActive(false);

            // mirrored at the line where the player platform usually ends (i.e. mirror plane is 0.75 m in front of the player)
            Transform mirroredAvatarTransform = _mirroredAvatarContainer.transform;
            mirroredAvatarTransform.SetPositionAndRotation(new Vector3(0, 0, 1.5f), Quaternion.identity);

            GameObject gameObject = _avatarSpawner.SpawnBareAvatar(avatar.prefab, _mirroredAvatarContainer.transform);

            TransformCopier transformCopier = _mirroredAvatarContainer.AddComponent<TransformCopier>();
            transformCopier.root = avatar.transform.parent;
            transformCopier.from = avatar.transform.GetComponentsInChildren<Transform>();
            transformCopier.to = gameObject.transform.GetComponentsInChildren<Transform>();

            foreach (Transform transform in transformCopier.to)
            {
                transform.gameObject.layer = AvatarLayers.kAlwaysVisible;
            }

            _mirroredAvatarContainer.SetActive(true);
            gameObject.SetActive(true);
        }

        private void DestroyMirroredAvatar()
        {
            Destroy(_mirroredAvatarContainer);
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

        private void OnUseFakeMirrorChanged(bool value)
        {
            ShowMirroredAvatar();
            _mirror.gameObject.SetActive(!value);
        }

        private void OnActiveCalibrationModeChanged(CalibrationMode calibrationMode)
        {
            NotifyPropertyChanged(nameof(isCalibrationMessageVisible));
            NotifyPropertyChanged(nameof(calibrationMessage));
        }

        private void UpdateMirrorRenderSettings(float scale, int antiAliasingLevel)
        {
            if (!_mirror) return;

            _mirror.renderScale = scale * _settingsManager.settings.quality.vrResolutionScale;
            _mirror.antiAliasing = antiAliasingLevel;
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

        private class AutoResizeMirror : EnvironmentObject
        {
            protected override void UpdateOffset()
            {
                float floorOffset = playerAvatarManager.GetFloorOffset();
                float scale = transform.localPosition.z / 2.6f; // screen system scale
                float width = 2.5f + scale;
                float height = 2f + 0.5f * scale - floorOffset;

                transform.localPosition = new Vector3(transform.localPosition.x, floorOffset + height / 2, transform.localPosition.z);
                transform.localScale = new Vector3(width / 10, 1, height / 10);
            }
        }

        // TODO: blend shapes and possibly other things
        private class TransformCopier : MonoBehaviour
        {
            public Transform root;
            public Transform[] from;
            public Transform[] to;

            protected void OnEnable()
            {
                Application.onBeforeRender += OnBeforeRender;
            }

            protected void OnDisable()
            {
                Application.onBeforeRender -= OnBeforeRender;
            }

            private void OnBeforeRender()
            {
                Vector3 scale = root.lossyScale;
                transform.localScale = new Vector3(scale.x, scale.y, -scale.z); // mirrored across XY plane

                foreach ((Transform from, Transform to) in from.Zip(to))
                {
                    from.GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);
                    to.SetLocalPositionAndRotation(position, rotation);
                    to.localScale = from.localScale;
                }
            }
        }
    }
}
