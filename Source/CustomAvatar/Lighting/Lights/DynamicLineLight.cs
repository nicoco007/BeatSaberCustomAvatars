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

using System.Collections.Generic;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting.Lights
{
    internal class DynamicLineLight : MonoBehaviour
    {
        private Settings _settings;
        private Light _light;

#if DEBUG
        private ShaderLoader _shaderLoader;
#endif

        public List<IApproximatedLight> lights { get; } = new List<IApproximatedLight>();

        [Inject]
        public void Construct(Settings settings, ShaderLoader shaderLoader)
        {
            _settings = settings;

#if DEBUG
            _shaderLoader = shaderLoader;
#endif
        }

        private void Start()
        {
            _light = gameObject.AddComponent<Light>();

            _light.type = LightType.Directional;
            _light.cullingMask = AvatarLayers.kAllLayersMask;
            _light.shadows = LightShadows.None;
            _light.intensity = 0;

#if DEBUG
            foreach (ApproximatedLineLight light in lights)
            {
                light.SetUp(_shaderLoader);
            }
#endif
        }

        private void Update()
        {
            if (lights.Count == 0) return;

            float intensity = 0;
            var color = new Color();
            Vector3 brightestPoint = Vector3.zero;

            foreach (IApproximatedLight light in lights)
            {
                light.Update();
                intensity += light.intensity;
                color += light.color;
                brightestPoint += light.brightestPoint * light.intensity;
            }

            _light.intensity = Mathf.Sqrt(intensity / lights.Count * _settings.lighting.environment.intensity);
            _light.enabled = _light.intensity > 0.0001f;
            _light.color = color / lights.Count;

            Vector3 position = brightestPoint / intensity;

            if (Mathf.Abs(position.sqrMagnitude) > 1e-3)
            {
                transform.rotation = Quaternion.LookRotation(-position);
            }
        }
    }
}
