//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace CustomAvatar.UI
{
    [ViewDefinition("CustomAvatar.UI.Views.Mirror.bsml")]
    [HotReload(RelativePathToLayout = "Views/Mirror.bsml")]
    internal class MirrorViewController : BSMLAutomaticViewController, IProgress<float>
    {
        private Settings _settings;
        private PlayerAvatarManager _avatarManager;
        private TrackingRig _trackingRig;
        private AssetLoader _assetLoader;

        private bool _isLoaderActive;
        private string _errorMessage;
        private bool _progressActive;
        private float _progress;
        private string _progressTitle;

        private IInstantiator _instantiator;

        private GameObject _container;
        private GameObject _floatingScreen;
        private GameObject _fakeMirror;
        private GameObject _realMirror;

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

        protected bool progressActive
        {
            get => _progressActive;
            set
            {
                _progressActive = value;
                NotifyPropertyChanged();
            }
        }

        protected float progress
        {
            get => _progress;
            set
            {
                _progress = value;
                NotifyPropertyChanged();
            }
        }

        protected string progressTitle
        {
            get => _progressTitle;
            set
            {
                _progressTitle = value;
                NotifyPropertyChanged();
            }
        }

        protected bool isErrorMessageVisible => !string.IsNullOrEmpty(errorMessage);

        protected bool isCalibrationMessageVisible => _trackingRig.isCalibrating;

        [Inject]
        [UsedImplicitly]
        private void Construct(
            IInstantiator instantiator,
            Settings settings,
            PlayerAvatarManager avatarManager,
            TrackingRig trackingRig,
            AssetLoader assetLoader)
        {
            _instantiator = instantiator;
            _settings = settings;
            _avatarManager = avatarManager;
            _trackingRig = trackingRig;
            _assetLoader = assetLoader;
        }

        public void Report(float progress)
        {
            this.progress = progress;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            _avatarManager.avatarLoading += OnAvatarLoading;
            _avatarManager.avatarChanged += OnAvatarChanged;
            _avatarManager.avatarLoadFailed += OnAvatarLoadFailed;

            _settings.mirror.useFakeMirrorBeta.changed += OnUseFakeMirrorChanged;
            _settings.playerEyeHeight.changed += OnEyeHeightChanged;

            _trackingRig.isCalibratingChanged += OnIsCalibratingChanged;

            if (firstActivation)
            {
                _fakeMirror = _instantiator.InstantiatePrefab(_assetLoader.fakeMirror, new Vector3(0, 0, 0.75f), Quaternion.identity, null);
                _fakeMirror.transform.localScale = new Vector3(1, 1, -1);

                _realMirror = new("RealMirror");
                _realMirror.SetActive(false);
                _instantiator.InstantiateComponent<RealMirrorProvider>(_realMirror);

                _container = new GameObject("Container");
                RectTransform rectTransform = _container.AddComponent<RectTransform>();
                rectTransform.SetParent(transform, false);

                foreach (Transform transform in transform)
                {
                    transform.SetParent(rectTransform, true);
                }

                _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(160, 80), false, new Vector3(0, 1f, 0.75f), Quaternion.Euler(0, 0, 0)).gameObject;
                BSMLParser.Instance.Parse(Content, _floatingScreen, this);

                Transform floatingScreenTransform = _floatingScreen.transform;
                floatingScreenTransform.localScale *= 0.5f;
            }

            SetLoading(false);
            OnUseFakeMirrorChanged(_settings.mirror.useFakeMirrorBeta);
            OnEyeHeightChanged(_settings.playerEyeHeight);
            OnIsCalibratingChanged(_trackingRig.isCalibrating);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            _fakeMirror.SetActive(false);
            _realMirror.SetActive(false);

            _floatingScreen.SetActive(false);

            _avatarManager.avatarLoading -= OnAvatarLoading;
            _avatarManager.avatarChanged -= OnAvatarChanged;
            _avatarManager.avatarLoadFailed -= OnAvatarLoadFailed;

            _settings.mirror.useFakeMirrorBeta.changed -= OnUseFakeMirrorChanged;

            _trackingRig.isCalibratingChanged -= OnIsCalibratingChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Destroy(_fakeMirror);
            Destroy(_realMirror);

            Destroy(_floatingScreen);
        }

        private void OnAvatarLoading(string filePath, string name)
        {
            progressTitle = $"Loading {name}";
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

        private void OnUseFakeMirrorChanged(bool value)
        {
            _fakeMirror.SetActive(value);
            _floatingScreen.SetActive(value);

            _realMirror.SetActive(!value);
            _container.SetActive(!value);
        }

        private void OnEyeHeightChanged(float eyeHeight)
        {
            Transform transform = _floatingScreen.transform;
            Vector3 position = transform.position;
            position.y = eyeHeight * 0.9f;
            transform.position = position;
        }

        private void OnIsCalibratingChanged(bool isCalibrating)
        {
            NotifyPropertyChanged(nameof(isCalibrationMessageVisible));
        }

        private void SetLoading(bool loading)
        {
            progressActive = loading;
            errorMessage = null;
        }

        [UsedImplicitly]
        private void OnCancelButtonClicked()
        {
            _trackingRig.EndCalibration();
        }
    }
}
