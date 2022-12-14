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

using CustomAvatar.Configuration;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting.Lights
{
    internal class DynamicTubeBloomPrePassLight : MonoBehaviour
    {
        [SerializeReference]
        private Settings _settings;

        [SerializeReference]
        private ShaderLoader _shaderLoader;

        [SerializeField]
        private Light _light;

        [SerializeField]
        private ApproximatedTubeBloomPrePassLight _tubeBloomPrePassLight;

        [SerializeField]
        private ApproximatedParametric3SliceSpriteLight _parametric3SliceSpriteLight;

        [SerializeField]
        private ApproximatedParametricBoxLight _parametricBoxLight;

        [Inject]
        public void Construct(Settings settings, ShaderLoader shaderLoader)
        {
            _settings = settings;
            _shaderLoader = shaderLoader;
        }

        public void Init(ApproximatedTubeBloomPrePassLight tubeBloomPrePassLight, ApproximatedParametric3SliceSpriteLight parametric3SliceSpriteLight, ApproximatedParametricBoxLight parametricBoxLight)
        {
            _light = GetComponent<Light>();
            _tubeBloomPrePassLight = tubeBloomPrePassLight;
            _parametric3SliceSpriteLight = parametric3SliceSpriteLight;
            _parametricBoxLight = parametricBoxLight;
        }

        private void Start()
        {
            _tubeBloomPrePassLight?.Initialize(_shaderLoader);
            _parametric3SliceSpriteLight?.Initialize(_shaderLoader);
            _parametricBoxLight?.Initialize(_shaderLoader);
        }

        private void Update()
        {
            float intensity = 0;
            int count = 0;
            var color = new Color();
            Vector3 brightestPoint = Vector3.zero;

            void UpdateLight(ApproximatedLineLight light)
            {
                if (light == null || !light.reference.isActiveAndEnabled)
                {
                    return;
                }

                light.Update();
                intensity += light.intensity;
                color += light.color;
                brightestPoint += light.brightestPoint * light.intensity;
                count++;
            }

            UpdateLight(_tubeBloomPrePassLight);
            UpdateLight(_parametric3SliceSpriteLight);
            UpdateLight(_parametricBoxLight);

            _light.intensity = Mathf.Sqrt(intensity / count * _settings.lighting.environment.intensity);
            _light.enabled = _light.intensity > 0.0001f;
            _light.color = color / count;

            Vector3 position = brightestPoint / intensity;

            if (Mathf.Abs(position.sqrMagnitude) > 1e-3)
            {
                transform.rotation = Quaternion.LookRotation(-position);
            }
        }
    }
}
