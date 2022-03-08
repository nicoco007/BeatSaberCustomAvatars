//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class DynamicTubeBloomPrePassLight : MonoBehaviour
    {
        private const float kTubeLightIntensityMultiplier = 15f;

        private static readonly Vector3 kOrigin = new Vector3(0, 1, 0);
        private static readonly FieldAccessor<TubeBloomPrePassLight, float>.Accessor kCenterAccessor = FieldAccessor<TubeBloomPrePassLight, float>.GetAccessor("_center");

        public Color color
        {
            get => _color;
            set
            {
                _color = value;
                UpdateUnityLight();
            }
        }

        public TubeBloomPrePassLight reference => _reference;

        private TubeBloomPrePassLight _reference;
        private Settings _settings;

        private Light _light;
        private Vector3 _previousPosition;
        private float _distanceIntensity;
        private float _center;
        private Color _color;

        #region Behaviour Lifecycle

        [Inject]
        internal void Construct(TubeBloomPrePassLight reference, Settings settings)
        {
            _reference = reference;
            _settings = settings;

            _center = kCenterAccessor(ref _reference);
        }

        internal void Start()
        {
            _light = gameObject.AddComponent<Light>();

            _light.type = LightType.Directional;
            _light.cullingMask = AvatarLayers.kAllLayersMask;
            _light.renderMode = LightRenderMode.ForceVertex;
            _light.shadows = LightShadows.None;

            UpdateIntensity();
        }

        internal void Update()
        {
            UpdateIntensity();
        }

        #endregion

        private void UpdateIntensity()
        {
            if (_reference.transform.position == _previousPosition) return;

            Vector3 lightPosition = _reference.transform.position;
            Vector3 up = _reference.transform.up;

            var projectionOfPositionOnLight = Vector3.Project(lightPosition, up);

            float perpendicularPointToCenter = projectionOfPositionOnLight.magnitude;
            float sqrMinimumDistance = (lightPosition - projectionOfPositionOnLight).sqrMagnitude;

            float xA = perpendicularPointToCenter + _center * _reference.length;
            float xB = perpendicularPointToCenter - (1.0f - _center) * _reference.length;

            float closestPointToPlayer = Mathf.Clamp(projectionOfPositionOnLight.magnitude, Mathf.Min(xA, xB), Mathf.Max(xA, xB));
            transform.rotation = Quaternion.LookRotation(kOrigin - (lightPosition + closestPointToPlayer * up));

            _distanceIntensity = Mathf.Abs(RelativeIntensityAlongLine(xB, sqrMinimumDistance) - RelativeIntensityAlongLine(xA, sqrMinimumDistance));
            _previousPosition = lightPosition;

            UpdateUnityLight();
        }

        private void UpdateUnityLight()
        {
            if (!_light) return;

            _light.color = _color;
            _light.intensity = _distanceIntensity * _reference.width * _color.a * Mathf.Sqrt(_reference.bloomFogIntensityMultiplier) * kTubeLightIntensityMultiplier * _settings.lighting.environment.intensity;

            _light.enabled = _light.intensity > 0.001f;
        }

        private float RelativeIntensityAlongLine(float x, float h2)
        {
            // integral is ∫ 1 / (1 + h^2 + x^2) dx = atan(x / sqrt(h^2 + 1)) / sqrt(h^2 + 1)
            float sqrt = Mathf.Sqrt(h2 + 1);
            return Mathf.Atan(x / sqrt) / sqrt;
        }
    }
}
