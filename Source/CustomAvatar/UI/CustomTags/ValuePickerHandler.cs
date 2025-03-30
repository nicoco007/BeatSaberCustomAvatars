//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using System.Collections.Generic;
using System.Globalization;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.TypeHandlers;

namespace CustomAvatar.UI.CustomTags
{
    [ComponentHandler(typeof(ValuePickerController))]
    internal class ValuePickerHandler : TypeHandler<ValuePickerController>
    {
        public override Dictionary<string, Action<ValuePickerController, string>> Setters => new()
        {
            { "minimum", (slider, text) => slider.minimum = float.Parse(text, CultureInfo.InvariantCulture) },
            { "maximum", (slider, text) => slider.maximum = float.Parse(text, CultureInfo.InvariantCulture) },
            { "step", (slider, text) => slider.step = float.Parse(text, CultureInfo.InvariantCulture) },
            { "interactable", (slider, text) => slider.interactable = bool.Parse(text) },
        };

        public override Dictionary<string, string[]> Props => new()
        {
            { "value", new[] { "value" } },
            { "minimum", new[] { "minimum" } },
            { "maximum", new[] { "maximum" } },
            { "step", new[] { "step" } },
            { "formatter", new[] { "formatter" } },
            { "interactable", new[] { "interactable" } },
        };

        public override void HandleType(BSMLParser.ComponentTypeWithData componentType, BSMLParserParams parserParams)
        {
            base.HandleType(componentType, parserParams);

            ValuePickerController component = (ValuePickerController)componentType.Component;

            if (componentType.Data.TryGetValue("formatter", out string formatterId))
            {
                if (!parserParams.Actions.TryGetValue(formatterId, out BSMLAction formatter))
                {
                    throw new Exception($"Formatter action '{formatterId}' not found");
                }

                component.formatter = formatter;
            }

            if (componentType.Data.TryGetValue("value", out string valueId))
            {
                if (!parserParams.Values.TryGetValue(valueId, out BSMLValue value))
                {
                    throw new Exception($"Value '{valueId}' not found");
                }

                component.associatedValue = value;
                component.value = (float)value.GetValue();

                BindValue(componentType, parserParams, value, _ => component.value = (float)value.GetValue());
            }
        }
    }
}
