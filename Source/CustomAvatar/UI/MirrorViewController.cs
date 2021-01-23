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

using CustomAvatar.Configuration;
using CustomAvatar.StereoRendering;
using UnityEngine;
using Zenject;
using CustomAvatar.Avatar;
using CustomAvatar.Player;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Attributes;
using System;
using HMUI;

namespace CustomAvatar.UI
{
    internal class MirrorViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.Views.Mirror.bsml";

        private GameObject _mirrorContainer;

        private MirrorHelper _mirrorHelper;
        private Settings _settings;
        private PlayerAvatarManager _avatarManager;
        private FloorController _floorController;

        #region Components
        #pragma warning disable CS0649

        [UIComponent("loader")] private readonly Transform _loader;
        [UIComponent("error-text")] private readonly CurvedTextMeshPro _errorText;

        #pragma warning restore CS0649
        #endregion

        #region Behaviour Lifecycle

        [Inject]
        private void Inject(MirrorHelper mirrorHelper, Settings settings, PlayerAvatarManager avatarManager, FloorController floorController)
        {
            _mirrorHelper = mirrorHelper;
            _settings = settings;
            _avatarManager = avatarManager;
            _floorController = floorController;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            name = nameof(MirrorViewController);

            if (addedToHierarchy)
            {
                Transform screenSystem = GameObject.Find("Wrapper/ScreenSystem").transform;

                _mirrorContainer = new GameObject("Mirror Container");
                _mirrorContainer.transform.SetParent(screenSystem, false);
                _mirrorContainer.transform.position = new Vector3(0, _floorController.floorPosition, 0);

                Vector2 mirrorSize = _settings.mirror.size;
                _mirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, 2.6f), Quaternion.Euler(-90f, 0, 0), mirrorSize, _mirrorContainer.transform);

                _avatarManager.avatarStartedLoading += OnAvatarStartedLoading;
                _avatarManager.avatarChanged += OnAvatarChanged;
                _avatarManager.avatarLoadFailed += OnAvatarLoadFailed;

                _floorController.floorPositionChanged += OnFloorPositionChanged;

                SetLoading(false);
            }
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            if (removedFromHierarchy)
            {
                _avatarManager.avatarStartedLoading -= OnAvatarStartedLoading;
                _avatarManager.avatarChanged -= OnAvatarChanged;
                _avatarManager.avatarLoadFailed -= OnAvatarLoadFailed;

                _floorController.floorPositionChanged -= OnFloorPositionChanged;
            }

            Destroy(_mirrorContainer);
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

            _errorText.color = new Color(0.85f, 0.85f, 0.85f, 0.8f);
            _errorText.text = $"Failed to load selected avatar\n<size=3>{exception.Message}</size>";
            _errorText.gameObject.SetActive(true);
        }

        private void OnFloorPositionChanged(float y)
        {
            Vector3 position = _mirrorContainer.transform.position;
            _mirrorContainer.transform.position = new Vector3(position.x, y, position.z);
        }

        private void SetLoading(bool loading)
        {
            _loader.gameObject.SetActive(loading);
            _errorText.gameObject.SetActive(false);
        }
    }
}
