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
    internal class DynamicTubeBloomPrePassLight : MonoBehaviour, ISerializationCallbackReceiver
    {
        private Settings _settings;

#if DEBUG
        private ShaderLoader _shaderLoader;
#endif

        [SerializeField]
        private Light _light;

        [SerializeField]
        private ApproximatedTubeBloomPrePassLight _tubeBloomPrePassLight;

        [SerializeField]
        private ApproximatedParametric3SliceSpriteLight _parametric3SliceSpriteLight;

        [SerializeField]
        private ApproximatedParametricBoxLight _parametricBoxLight;

        private readonly List<ApproximatedLineLight> _approximatedLineLights = new List<ApproximatedLineLight>(3);

        [Inject]
        public void Construct(Settings settings, ShaderLoader shaderLoader)
        {
            _settings = settings;

#if DEBUG
            _shaderLoader = shaderLoader;
#endif
        }

        public void Init(ApproximatedTubeBloomPrePassLight tubeBloomPrePassLight, ApproximatedParametric3SliceSpriteLight parametric3SliceSpriteLight, ApproximatedParametricBoxLight parametricBoxLight)
        {
            _light = GetComponent<Light>();
            _tubeBloomPrePassLight = tubeBloomPrePassLight;
            _parametric3SliceSpriteLight = parametric3SliceSpriteLight;
            _parametricBoxLight = parametricBoxLight;

            PopulateList();
        }

#if DEBUG
        private void Start()
        {
            foreach (ApproximatedLineLight light in _approximatedLineLights)
            {
                light.SetUp(_shaderLoader);
            }
        }
#endif

        private void Update()
        {
            if (_approximatedLineLights.Count == 0)
            {
                return;
            }

            float intensity = 0;
            var color = new Color();
            Vector3 brightestPoint = Vector3.zero;

            foreach (ApproximatedLineLight light in _approximatedLineLights)
            {
                light.Update();
                intensity += light.intensity;
                color += light.color;
                brightestPoint += light.brightestPoint * light.intensity;
            }

            _light.intensity = Mathf.Sqrt(intensity / _approximatedLineLights.Count * _settings.lighting.environment.intensity);
            _light.enabled = _light.intensity > 0.0001f;
            _light.color = color / _approximatedLineLights.Count;

            Vector3 position = brightestPoint / intensity;

            if (Mathf.Abs(position.sqrMagnitude) > 1e-3)
            {
                transform.rotation = Quaternion.LookRotation(-position);
            }
        }

        public void OnBeforeSerialize()
        {
            // nothing to do here
        }

        public void OnAfterDeserialize()
        {
            PopulateList();
        }

        private void PopulateList()
        {
            _approximatedLineLights.Clear();

            if (_tubeBloomPrePassLight != null)
            {
                _approximatedLineLights.Add(_tubeBloomPrePassLight);
            }

            if (_parametric3SliceSpriteLight != null)
            {
                _approximatedLineLights.Add(_parametric3SliceSpriteLight);
            }

            if (_parametricBoxLight != null)
            {
                _approximatedLineLights.Add(_parametricBoxLight);
            }
        }
    }
}
