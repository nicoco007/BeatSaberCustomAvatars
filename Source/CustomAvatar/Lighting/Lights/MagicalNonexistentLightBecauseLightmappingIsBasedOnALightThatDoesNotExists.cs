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

using System.Diagnostics.CodeAnalysis;
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting.Lights
{
    internal class MagicalNonexistentLightBecauseLightmappingIsBasedOnALightThatDoesNotExists : RuntimeLightWithIds
    {
        private Light _light;

        protected override void ColorWasSet(Color color)
        {
            _light.color = color;
            _light.intensity = color.a;
        }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(Light light, LightIntensitiesWithId[] lightIntensitiesWithIds, float intensity)
        {
            _light = light;

            this.SetField<RuntimeLightWithIds, float>("_intensity", intensity);
            this.SetField<RuntimeLightWithIds, LightIntensitiesWithId[]>("_lightIntensityData", lightIntensitiesWithIds);

            SetNewLightsWithIds(lightIntensitiesWithIds);
        }
    }
}
