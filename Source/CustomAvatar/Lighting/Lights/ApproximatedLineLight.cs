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

using CustomAvatar.Utilities;

using UnityEngine;

namespace CustomAvatar.Lighting.Lights
{
    internal abstract class ApproximatedLineLight
    {
        private static readonly Vector3 kOrigin = new Vector3(0, 1.5f, 0);

#if DEBUG
#pragma warning disable IDE0044
        private static bool _debugLighting = false;
        private static bool _showOrigin = true;
        private static bool _showStart = true;
        private static bool _showEnd = true;
        private static bool _showBrightest = false;
#pragma warning restore IDE0044

        private Transform _origin;
        private Transform _start;
        private Transform _end;
        private Transform _brightest;
#endif // DEBUG

        public float intensity { get; private set; }

        public Vector3 brightestPoint { get; private set; }

        public abstract Color color { get; }

        protected abstract float lightIntensityMultiplier { get; }

        protected abstract float startAlpha { get; }

        protected abstract float endAlpha { get; }

        protected abstract float startWidth { get; }

        protected abstract float endWidth { get; }

        protected abstract float width { get; }

        protected abstract float length { get; }

        protected abstract float center { get; }

        protected virtual float minAlpha => 0;

        protected virtual float alphaMultiplier => 1;

        protected abstract Behaviour reference { get; }

        protected Transform origin { get; set; }

        public void Initialize(ShaderLoader shaderLoader)
        {
            if (this.origin == null)
            {
                this.origin = reference.transform;
            }

#if DEBUG
            if (_debugLighting)
            {
                _origin = Sphere("Origin", shaderLoader.unlitShader, Color.red);
                _start = Sphere("Start", shaderLoader.unlitShader, Color.green);
                _end = Sphere("End", shaderLoader.unlitShader, Color.blue);
                _brightest = Sphere("Brightest", shaderLoader.unlitShader, Color.magenta);
            }
#endif
        }

#if DEBUG
        private Transform Sphere(string name, Shader shader, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Transform t = go.transform;

            t.localScale = Vector3.one * 0.1f;
            t.SetParent(origin, false);

            Material m = go.GetComponent<MeshRenderer>().material;
            m.color = color;
            m.shader = shader;

            return t;
        }
#endif // DEBUG

        public void Update()
        {
            if (!reference.isActiveAndEnabled)
            {
                this.brightestPoint = Vector3.zero;
                intensity = 0;
                return;
            }

            Vector3 lightPosition = origin.position - kOrigin;
            Vector3 lightUp = origin.up;

            var projectionOfPositionOnLight = Vector3.Project(lightPosition, lightUp);
            Vector3 originToProjection = lightPosition - projectionOfPositionOnLight;
            float sqrMinimumDistance = originToProjection.sqrMagnitude;

            Vector3 start = origin.TransformPoint(center * length * Vector3.down) - kOrigin;
            Vector3 end = origin.TransformPoint((1.0f - center) * length * Vector3.up) - kOrigin;

            Vector3 pStart = start - originToProjection;
            Vector3 pEnd = end - originToProjection;

            float xStart = Vector3.Dot(lightUp, pStart) >= 0 ? pStart.magnitude : -pStart.magnitude;
            float xEnd = Vector3.Dot(lightUp, pEnd) >= 0 ? pEnd.magnitude : -pEnd.magnitude;

            float brightestPoint;

            // TODO: figure out what needs to happen if startAlpha and endAlpha aren't 0 & 1
            if (startAlpha > endAlpha)
            {
                brightestPoint = xStart;
            }
            else if (startAlpha < endAlpha)
            {
                brightestPoint = xEnd;
            }
            else
            {
                brightestPoint = Mathf.Clamp(0, Mathf.Min(xStart, xEnd), Mathf.Max(xStart, xEnd));
            }

            float distanceIntensity = (IntensitySquareFalloff(xEnd, sqrMinimumDistance, xStart, xEnd) - IntensitySquareFalloff(xStart, sqrMinimumDistance, xStart, xEnd)) * origin.TransformVector(width * Vector3.right).magnitude;
            this.brightestPoint = originToProjection + brightestPoint * lightUp;

#if DEBUG
            if (_debugLighting)
            {
                _origin.position = origin.position;
                _start.position = originToProjection + xStart * lightUp + kOrigin;
                _end.position = originToProjection + xEnd * lightUp + kOrigin;
                _brightest.position = this.brightestPoint + kOrigin;

                _origin.gameObject.SetActive(_showOrigin);
                _start.gameObject.SetActive(_showStart);
                _end.gameObject.SetActive(_showEnd);
                _brightest.gameObject.SetActive(_showBrightest);
            }
#endif

            this.intensity = distanceIntensity * Mathf.Min(Mathf.Max(color.a, minAlpha) * alphaMultiplier, 1) * lightIntensityMultiplier;
        }

        private float IntensitySquareFalloff(float x, float h2, float xMax, float xMin)
        {
            // welcome to the cursed integral
            // start to end of light: t = (x - xMin) / (xMax - xMin) => 0 at x = xMin, 1 at x = xMax
            // brightness at a given point: alphaStart * widthStart + (alphaEnd * widthEnd - alphaStart * widthStart) * t
            // distance from point to origin: sqrt(1 + h^2 + x^2)
            // we want square falloff: 1 / (2 + h^2 + x^2)
            // integral is therefore ∫ (alphaStart * widthStart + (alphaEnd * widthEnd - alphaStart * widthStart) * (x - xMin) / (xMax - xMin)) * 1 / (2 + h^2 + x^2) dx
            return (Mathf.Atan(x / Mathf.Sqrt(h2 + 1)) * (xMin * startAlpha * startWidth - xMax * endAlpha * endWidth) / Mathf.Sqrt(h2 + 1) + 0.5f * Mathf.Log(h2 + x * x + 1) * (endAlpha * endWidth - startAlpha * startWidth)) / (xMin - xMax);
        }
    }
}
