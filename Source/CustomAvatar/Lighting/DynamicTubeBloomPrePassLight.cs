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

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class DynamicTubeBloomPrePassLight : MonoBehaviour
    {
        public Color color
        {
            get => _color;
            set
            {
                _color = value;
                UpdateUnityLight();
            }
        }

        private TubeBloomPrePassLight _reference;
        private Settings _settings;

        private Light _light;
        private Color _color;

        #region Behaviour Lifecycle

        [Inject]
        internal void Construct(TubeBloomPrePassLight reference, Settings settings)
        {
            _reference = reference;
            _settings = settings;
        }

        internal void Start()
        {
            _light = gameObject.AddComponent<Light>();

            _light.type = LightType.Point;
            _light.cullingMask = AvatarLayers.kAllLayersMask;
            _light.renderMode = LightRenderMode.Auto;
            _light.shadows = LightShadows.None;
            _light.shadowStrength = 1;
            _light.range = 100;

            UpdateUnityLight();
        }

        #endregion

        private void UpdateUnityLight()
        {
            if (!_light) return;

            _light.color = _color;
            _light.intensity = _color.a * _settings.lighting.environment.intensity;
            _light.range = _reference.width * _reference.lightWidthMultiplier * 100;
        }
    }
}
