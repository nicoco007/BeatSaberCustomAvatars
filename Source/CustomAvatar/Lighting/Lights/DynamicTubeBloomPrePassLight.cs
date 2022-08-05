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

using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting.Lights
{
    internal class DynamicTubeBloomPrePassLight : ApproximatedLineLight
    {
        private readonly TubeBloomPrePassLight _tubeBloomPrePassLight;
        private readonly float _lightIntensityMultiplier;

        private readonly float _startAlpha;
        private readonly float _endAlpha;
        private readonly float _startWidth;
        private readonly float _endWidth;

        public override Color color => _tubeBloomPrePassLight.color;

        protected override float lightIntensityMultiplier => _lightIntensityMultiplier * _tubeBloomPrePassLight.lightWidthMultiplier * Mathf.Pow(_tubeBloomPrePassLight.bloomFogIntensityMultiplier, 0.1f);

        protected override float startAlpha => _startAlpha;

        protected override float endAlpha => _endAlpha;

        protected override float startWidth => _startWidth;

        protected override float endWidth => _endWidth;

        protected override float width => _tubeBloomPrePassLight.width;

        protected override float length => _tubeBloomPrePassLight.length;

        protected override float center => _tubeBloomPrePassLight.center;

        protected override Behaviour reference => _tubeBloomPrePassLight;

        [Inject]
        internal DynamicTubeBloomPrePassLight(TubeBloomPrePassLight tubeBloomPrePassLight, float lightIntensityMultiplier)
        {
            _tubeBloomPrePassLight = tubeBloomPrePassLight;
            _lightIntensityMultiplier = lightIntensityMultiplier;

            _startAlpha = tubeBloomPrePassLight.GetField<float, TubeBloomPrePassLight>("_startAlpha");
            _endAlpha = tubeBloomPrePassLight.GetField<float, TubeBloomPrePassLight>("_endAlpha");
            _startWidth = tubeBloomPrePassLight.GetField<float, TubeBloomPrePassLight>("_startWidth") * tubeBloomPrePassLight.width;
            _endWidth = tubeBloomPrePassLight.GetField<float, TubeBloomPrePassLight>("_endWidth") * tubeBloomPrePassLight.width;
        }
    }
}
