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
    internal class ApproximatedParametric3SliceSpriteLight : ApproximatedLineLight
    {
        private readonly Parametric3SliceSpriteController _parametric3SliceSpriteController;
        private readonly float _lightIntensityMultiplier;
        private readonly float _widthMultiplier;

        public override Color color => _parametric3SliceSpriteController.color;

        protected override float lightIntensityMultiplier => _lightIntensityMultiplier * _widthMultiplier;

        protected override float startAlpha => _parametric3SliceSpriteController.alphaStart;

        protected override float endAlpha => _parametric3SliceSpriteController.alphaEnd;

        protected override float startWidth => _parametric3SliceSpriteController.widthStart;

        protected override float endWidth => _parametric3SliceSpriteController.widthEnd;

        protected override float width => _parametric3SliceSpriteController.width;

        protected override float length => _parametric3SliceSpriteController.length;

        protected override float center => _parametric3SliceSpriteController.center;

        protected override float minAlpha => _parametric3SliceSpriteController.minAlpha;

        protected override float alphaMultiplier => _parametric3SliceSpriteController.alphaMultiplier;

        protected override Behaviour reference => _parametric3SliceSpriteController;

        [Inject]
        internal ApproximatedParametric3SliceSpriteLight(Parametric3SliceSpriteController parametric3SliceSpriteController, float lightIntensityMultiplier)
        {
            _parametric3SliceSpriteController = parametric3SliceSpriteController;
            _lightIntensityMultiplier = lightIntensityMultiplier;
            _widthMultiplier = parametric3SliceSpriteController.GetField<float, Parametric3SliceSpriteController>("_widthMultiplier");
        }
    }
}
