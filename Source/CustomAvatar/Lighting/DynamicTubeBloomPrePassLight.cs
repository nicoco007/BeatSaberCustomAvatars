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

using CustomAvatar.Avatar;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class DynamicTubeBloomPrePassLight : MonoBehaviour
    {
        private static readonly Vector3 kOrigin = new Vector3(0, 1, 0);

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

        private Light _light;
        private Vector3 _previousPosition;
        private float _distanceIntensity;
        private float _center;
        private float _offsetToMiddle;
        private float _colorAlphaMultiplier;
        private bool _limitAlpha;
        private float _minAlpha;
        private float _maxAlpha;
        private Color _color;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        [Inject]
        public void Construct(TubeBloomPrePassLight reference)
        {
            _reference = reference;

            _center = reference.GetPrivateField<float>("_center");
            _colorAlphaMultiplier = reference.GetPrivateField<float>("_colorAlphaMultiplier");
            _limitAlpha = reference.GetPrivateField<bool>("_limitAlpha");
            _minAlpha = reference.GetPrivateField<float>("_minAlpha");
            _maxAlpha = reference.GetPrivateField<float>("_maxAlpha");

            _offsetToMiddle = (0.5f - _center) * _reference.length;
        }

        private void Start()
        {
            _light = gameObject.AddComponent<Light>();

            _light.type = LightType.Directional;
            _light.cullingMask = AvatarLayers.kAllLayersMask;
            _light.renderMode = LightRenderMode.ForceVertex;

            UpdateIntensity();
        }

        private void Update()
        {
            UpdateIntensity();
        }

        #pragma warning restore IDE0051
        #endregion

        private void UpdateIntensity()
        {
            if (!_reference.isActiveAndEnabled || _reference.transform.position == _previousPosition) return;

            Vector3 position = _reference.transform.position;
            Vector3 up = (_reference.transform.rotation * Vector3.up).normalized;

            Vector3 projectionOnLight = Vector3.Project(position, up);
            Vector3 perpendicularOriginToLight = position - projectionOnLight;

            float sqrMinimumDistance = perpendicularOriginToLight.sqrMagnitude;

            // the two ends of the light
            Vector3 endA = position + (1.0f - _center) * _reference.length * up; // end at +Y
            Vector3 endB = position - _center * _reference.length * up;          // end at -Y

            // parallel to up
            Vector3 distanceOnLineA = endA - perpendicularOriginToLight;
            Vector3 distanceOnLineB = endB - perpendicularOriginToLight;

            float xA = distanceOnLineA.magnitude * Mathf.Sign(Vector3.Dot(distanceOnLineA, up));
            float xB = distanceOnLineB.magnitude * Mathf.Sign(Vector3.Dot(distanceOnLineB, up));

            transform.rotation = Quaternion.LookRotation(kOrigin - (_reference.transform.position + _offsetToMiddle * up));

            _distanceIntensity = Mathf.Abs(RelativeIntensityAlongLine(xB, sqrMinimumDistance) - RelativeIntensityAlongLine(xA, sqrMinimumDistance));
            _previousPosition = _reference.transform.position;
        }

        private void UpdateUnityLight()
        {
            if (!_light) return;

            _light.color = _color;
            _light.intensity = _distanceIntensity * _reference.width * _reference.lightWidthMultiplier * _reference.bloomFogIntensityMultiplier * GetActualAlpha(_color.a) * 10f;

            _light.enabled = _light.intensity > 0.001f;
        }

        private float GetActualAlpha(float absoluteAlpha)
        {
            float adjustedAlpha = absoluteAlpha * _colorAlphaMultiplier;
            return _limitAlpha ? Mathf.Clamp(adjustedAlpha, _minAlpha, _maxAlpha) : adjustedAlpha;
        }

        private float RelativeIntensityAlongLine(float x, float h2)
        {
            // integral is ∫ 1 / (1 + h^2 + x^2) dx = atan(x / sqrt(h^2 + 1)) / sqrt(h^2 + 1)
            float sqrt = Mathf.Sqrt(h2 + 1);
            return Mathf.Atan(x / sqrt) / sqrt;
        }
    }
}
