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

using CustomAvatar.Configuration;
using CustomAvatar.Player;
using HMUI;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    internal class RealMirrorProvider : MonoBehaviour
    {
        private StereoMirrorRenderer _mirror;

        private IInstantiator _instantiator;
        private SettingsManager _settingsManager;
        private MirrorHelper _mirrorHelper;
        private HierarchyManager _hierarchyManager;
        private Settings _settings;

        [Inject]
        protected void Construct(IInstantiator instantiator, MirrorHelper mirrorHelper, Settings settings, SettingsManager settingsManager, HierarchyManager hierarchyManager)
        {
            _instantiator = instantiator;
            _mirrorHelper = mirrorHelper;
            _settings = settings;
            _settingsManager = settingsManager;
            _hierarchyManager = hierarchyManager;
        }

        protected void Awake()
        {
            Vector2 mirrorSize = new(4, 2);
            _mirror = _mirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, _hierarchyManager.GetComponent<ScreenSystem>().topScreen.transform.position.z), Quaternion.Euler(-90f, 0, 0), mirrorSize, transform);

            if (_mirror == null)
            {
                return;
            }

            _instantiator.InstantiateComponent<AutoResizeMirror>(_mirror.gameObject);
        }

        protected void OnEnable()
        {
            _settings.mirror.renderScale.changed += OnMirrorRenderScaleChanged;
            _settings.mirror.antiAliasingLevel.changed += OnMirrorAntiAliasingLevelChanged;

            UpdateMirrorRenderSettings(_settings.mirror.renderScale, _settings.mirror.antiAliasingLevel);
        }

        protected void OnDisable()
        {
            _settings.mirror.renderScale.changed -= OnMirrorRenderScaleChanged;
            _settings.mirror.antiAliasingLevel.changed -= OnMirrorAntiAliasingLevelChanged;
        }

        private void UpdateMirrorRenderSettings(float scale, int antiAliasingLevel)
        {
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
