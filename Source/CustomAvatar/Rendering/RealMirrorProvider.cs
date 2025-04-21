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

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using HMUI;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    internal class RealMirrorProvider : IMirrorProvider
    {
        private StereoMirrorRenderer _mirror;
        private GameObject _mirrorGameObject;

        private readonly DiContainer _container;
        private readonly SettingsManager _settingsManager;
        private readonly MirrorHelper _mirrorHelper;
        private readonly HierarchyManager _hierarchyManager;
        private readonly Settings _settings;

        internal RealMirrorProvider(DiContainer container, MirrorHelper mirrorHelper, Settings settings, SettingsManager settingsManager, HierarchyManager hierarchyManager)
        {
            _container = container;
            _mirrorHelper = mirrorHelper;
            _settings = settings;
            _settingsManager = settingsManager;
            _hierarchyManager = hierarchyManager;
        }

        public void Initialize()
        {
            Vector2 mirrorSize = new(4, 2);
            _mirror = _mirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, _hierarchyManager.GetComponent<ScreenSystem>().topScreen.transform.position.z), Quaternion.Euler(-90f, 0, 0), mirrorSize, null);

            if (_mirror == null)
            {
                return;
            }

            _mirrorGameObject = _mirror.gameObject;
            _mirrorGameObject.SetActive(false);
            _container.InstantiateComponent<AutoResizeMirror>(_mirrorGameObject);
        }

        public void Destroy()
        {
            Object.Destroy(_mirrorGameObject);
        }

        public void Enable()
        {
            _settings.mirror.renderScale.changed += OnMirrorRenderScaleChanged;
            _settings.mirror.antiAliasingLevel.changed += OnMirrorAntiAliasingLevelChanged;

            OnMirrorRenderScaleChanged(_settings.mirror.renderScale);

            _mirrorGameObject.SetActive(true);
        }

        public void Disable()
        {
            _settings.mirror.renderScale.changed -= OnMirrorRenderScaleChanged;
            _settings.mirror.antiAliasingLevel.changed -= OnMirrorAntiAliasingLevelChanged;

            _mirrorGameObject.SetActive(false);
        }

        public void ShowAvatar(SpawnedAvatar avatar)
        {
        }

        public void HideAvatar()
        {
        }

        private void UpdateMirrorRenderSettings(float scale, int antiAliasingLevel)
        {
            if (_mirror == null)
            {
                return;
            }

            _mirror.renderScale = scale * _settingsManager.settings.quality.vrResolutionScale;
            _mirror.antiAliasing = antiAliasingLevel;
        }

        private void OnMirrorRenderScaleChanged(float renderScale)
        {
            UpdateMirrorRenderSettings(renderScale, _settings.mirror.antiAliasingLevel);
        }

        private void OnMirrorAntiAliasingLevelChanged(int antiAliasingLevel)
        {
            UpdateMirrorRenderSettings(_settings.mirror.renderScale, antiAliasingLevel);
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
    }
}
