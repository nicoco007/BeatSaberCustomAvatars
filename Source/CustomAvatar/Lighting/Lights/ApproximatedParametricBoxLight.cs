//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System;
using UnityEngine;

namespace CustomAvatar.Lighting.Lights
{
    [Serializable]
    internal class ApproximatedParametricBoxLight : ApproximatedLineLight
    {
        [SerializeField]
        private readonly ParametricBoxController _parametricBoxController;

        [SerializeField]
        private readonly float _lightIntensityMultiplier;

        public override Color color => _parametricBoxController.color;

        protected override float lightIntensityMultiplier => _lightIntensityMultiplier;

        protected override float startAlpha => _parametricBoxController.alphaStart;

        protected override float endAlpha => _parametricBoxController.alphaEnd;

        protected override float startWidth => _parametricBoxController.widthStart;

        protected override float endWidth => _parametricBoxController.widthEnd;

        protected override float width => _parametricBoxController.length;

        protected override float length => _parametricBoxController.height;

        protected override float center => _parametricBoxController.heightCenter;

        protected override float minAlpha => _parametricBoxController.minAlpha;

        protected override float alphaMultiplier => _parametricBoxController.alphaMultiplier;

        public override Behaviour reference => _parametricBoxController;

        internal ApproximatedParametricBoxLight(ParametricBoxController parametricBoxController, float lightIntensityMultiplier)
        {
            _parametricBoxController = parametricBoxController;
            _lightIntensityMultiplier = lightIntensityMultiplier;

            this.origin = _parametricBoxController.transform.parent;
        }
    }
}
