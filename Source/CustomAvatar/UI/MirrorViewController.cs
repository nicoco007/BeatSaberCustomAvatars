//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
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

namespace CustomAvatar.UI
{
    internal class MirrorViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.Views.Mirror.bsml";

        private GameObject _mirrorContainer;

        private MirrorHelper _mirrorHelper;
        private Settings _settings;
        private PlayerAvatarManager _avatarManager;

        #region Components
        #pragma warning disable CS0649

        [UIComponent("loader")]
        private readonly Transform _loader;

        #pragma warning restore CS0649
        #endregion

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        [Inject]
        private void Inject(MirrorHelper mirrorHelper, Settings settings, PlayerAvatarManager avatarManager)
        {
            _mirrorHelper = mirrorHelper;
            _settings = settings;
            _avatarManager = avatarManager;
        }

        #pragma warning restore IDE0051
        #endregion

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (addedToHierarchy)
            {
                _mirrorContainer = new GameObject();
                Vector2 mirrorSize = _settings.mirror.size;
                _mirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, 2), Quaternion.Euler(-90f, 0, 0), mirrorSize, _mirrorContainer.transform);

                _avatarManager.avatarStartedLoading += OnAvatarStartedLoading;
                _avatarManager.avatarChanged += OnAvatarChanged;

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
            }

            Destroy(_mirrorContainer);
        }

        private void OnAvatarStartedLoading(string fileName)
        {
            SetLoading(true);
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            SetLoading(false);
        }

        private void SetLoading(bool loading)
        {
            _loader.gameObject.SetActive(loading);
        }
    }
}
