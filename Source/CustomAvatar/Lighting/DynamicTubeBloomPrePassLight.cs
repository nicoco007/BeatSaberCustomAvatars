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
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class DynamicTubeBloomPrePassLight : MonoBehaviour
    {
        private const float kTubeLightIntensityMultiplier = 10f;

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
            _light.shadowStrength = 1;

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

            Vector3 position = _reference.transform.position;
            Vector3 up = _reference.transform.rotation * Vector3.up;

            var projectionOfPositionOnLight = Vector3.Project(position, up);

            float perpendicularPointToCenter = projectionOfPositionOnLight.magnitude;
            float sqrMinimumDistance = (position - projectionOfPositionOnLight).sqrMagnitude;

            float xA = perpendicularPointToCenter + (1.0f - _center) * _reference.length;
            float xB = perpendicularPointToCenter - _center * _reference.length;

            float closestPointToPlayer = Mathf.Clamp(projectionOfPositionOnLight.magnitude, Mathf.Min(xA, xB), Mathf.Max(xA, xB));
            transform.rotation = Quaternion.LookRotation(kOrigin - (_reference.transform.position + closestPointToPlayer * up));

            _distanceIntensity = Mathf.Abs(RelativeIntensityAlongLine(xB, sqrMinimumDistance) - RelativeIntensityAlongLine(xA, sqrMinimumDistance));
            _previousPosition = _reference.transform.position;
        }

        private void UpdateUnityLight()
        {
            if (!_light) return;

            _light.color = _color;
            _light.intensity = _distanceIntensity * _reference.width * _reference.lightWidthMultiplier * _color.a * _reference.colorAlphaMultiplier * kTubeLightIntensityMultiplier * _settings.lighting.environment.intensity;

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
