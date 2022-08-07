﻿//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
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

using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting.Lights
{
    internal class DynamicBloomPrePassBackgroundColorsGradient : MonoBehaviour
    {
        private BloomPrePassBackgroundColorsGradient _bloomPrePassBackgroundColorsGradient;
        private float _intensity;

        [Inject]
        internal void Construct(BloomPrePassBackgroundColorsGradient bloomPrePassBackgroundColorsGradient, float intensity)
        {
            _bloomPrePassBackgroundColorsGradient = bloomPrePassBackgroundColorsGradient;
            _intensity = intensity;
        }

        private void Start()
        {
            RenderSettings.ambientIntensity = 1;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        }

        private void Update()
        {
            Color tintColor = _bloomPrePassBackgroundColorsGradient.tintColor;
            RenderSettings.ambientSkyColor = _bloomPrePassBackgroundColorsGradient.EvaluateColor(0) * tintColor * _intensity;
            RenderSettings.ambientEquatorColor = _bloomPrePassBackgroundColorsGradient.EvaluateColor(0.5f) * tintColor * _intensity;
            RenderSettings.ambientGroundColor = _bloomPrePassBackgroundColorsGradient.EvaluateColor(1) * tintColor * _intensity;
        }
    }
}