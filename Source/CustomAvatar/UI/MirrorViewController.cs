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
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using HMUI;
using UnityEngine;
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

        #region Components
#pragma warning disable CS0649

        [UIComponent("loader")] private readonly Transform _loader;
        [UIComponent("error-text")] private readonly CurvedTextMeshPro _errorText;

#pragma warning restore CS0649
        #endregion

        #region Behaviour Lifecycle

        [Inject]
        internal void Construct(DiContainer container, MirrorHelper mirrorHelper, Settings settings, MainSettingsModelSO mainSettingsModel, PlayerAvatarManager avatarManager, HierarchyManager hierarchyManager)
        {
            _container = container;
            _mirrorHelper = mirrorHelper;
            _settings = settings;
            _mainSettingsModel = mainSettingsModel;
            _avatarManager = avatarManager;
            _hierarchyManager = hierarchyManager;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            _avatarManager.avatarStartedLoading += OnAvatarStartedLoading;
            _avatarManager.avatarChanged += OnAvatarChanged;
            _avatarManager.avatarLoadFailed += OnAvatarLoadFailed;

            _settings.mirrorRenderScale.changed += OnMirrorRenderScaleChanged;

            SetLoading(false);

            if (addedToHierarchy)
            {
                var mirrorSize = new Vector2(4, 2);
                _mirror = _mirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, _hierarchyManager.transform.Find("ScreenContainer").position.z), Quaternion.Euler(-90f, 0, 0), mirrorSize, null);

                if (!_mirror) return;

                _mirror.antiAliasing = _mainSettingsModel.antiAliasingLevel;

                _container.InstantiateComponent<AutoResizeMirror>(_mirror.gameObject);
            }

            OnMirrorRenderScaleChanged(_settings.mirrorRenderScale);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            if (removedFromHierarchy)
            {
                Destroy(_mirror.gameObject);
            }

            _avatarManager.avatarStartedLoading -= OnAvatarStartedLoading;
            _avatarManager.avatarChanged -= OnAvatarChanged;
            _avatarManager.avatarLoadFailed -= OnAvatarLoadFailed;

            _settings.mirrorRenderScale.changed -= OnMirrorRenderScaleChanged;
        }

        #endregion

        private void OnAvatarStartedLoading(string fileName)
        {
            SetLoading(true);
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            SetLoading(false);
        }

        private void OnAvatarLoadFailed(Exception exception)
        {
            SetLoading(false);

            _errorText.text = $"Failed to load selected avatar\n<size=75%>{exception.Message}</size>";
            _errorText.gameObject.SetActive(true);
        }

        private void OnMirrorRenderScaleChanged(float scale)
        {
            if (!_mirror) return;

            int baseWidth = XRSettings.eyeTextureWidth;
            int baseHeight = XRSettings.eyeTextureHeight;

            if (baseWidth <= 0)
            {
                baseWidth = UnityEngine.Screen.width;
            }

            if (baseHeight <= 0)
            {
                baseHeight = UnityEngine.Screen.height;
            }

            _mirror.renderWidth = Mathf.RoundToInt(baseWidth * scale * _mainSettingsModel.vrResolutionScale);
            _mirror.renderHeight = Mathf.RoundToInt(baseHeight * scale * _mainSettingsModel.vrResolutionScale);
        }

        private void SetLoading(bool loading)
        {
            _loader.gameObject.SetActive(loading);
            _errorText.gameObject.SetActive(false);
        }

        private class AutoResizeMirror : EnvironmentObject
        {
            protected override void UpdateOffset()
            {
                float floorOffset = _playerAvatarManager.GetFloorOffset();

                if (_settings.moveFloorWithRoomAdjust)
                {
                    floorOffset += _beatSaberUtilities.roomCenter.y;
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
