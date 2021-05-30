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
    internal class SaberLightingController : IInitializable
    {
        private SaberManager _saberManager;
        private ColorManager _colorManager;
        private Settings _settings;

        [Inject]
        internal void Construct(SaberManager saberManager, ColorManager colorManager, Settings settings)
        {
            _saberManager = saberManager;
            _colorManager = colorManager;
            _settings = settings;
        }

        public void Initialize()
        {
            AddPointLight(_colorManager.ColorForSaberType(SaberType.SaberA), _saberManager.leftSaber.transform);
            AddPointLight(_colorManager.ColorForSaberType(SaberType.SaberB), _saberManager.rightSaber.transform);
        }

        private void AddPointLight(Color color, Transform parent)
        {
            Light light = new GameObject(parent.name + "Light").AddComponent<Light>();

            light.type = LightType.Point;
            light.color = color;
            light.intensity = 0.6f * _settings.lighting.sabers.intensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 1;
            light.renderMode = LightRenderMode.ForcePixel; // point lights don't do much when vertex rendered
            light.bounceIntensity = 0;
            light.range = 5;
            light.cullingMask = AvatarLayers.kAllLayersMask;

            light.transform.SetParent(parent, false);
            light.transform.localPosition = new Vector3(0, 0, 0.5f); // middle of saber
            light.transform.rotation = Quaternion.identity;
        }
    }
}
