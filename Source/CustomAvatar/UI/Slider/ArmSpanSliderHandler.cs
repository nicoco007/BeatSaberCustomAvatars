using System;
using System.Collections.Generic;
using System.Globalization;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.TypeHandlers;

namespace CustomAvatar.UI.Slider
{
    [ComponentHandler(typeof(ArmSpanSliderController))]
    internal class ArmSpanSliderHandler : TypeHandler<ArmSpanSliderController>
    {
        public override Dictionary<string, Action<ArmSpanSliderController, string>> Setters => new Dictionary<string, Action<ArmSpanSliderController, string>>
        {
            { "minimum", (slider, text) => slider.minimum = float.Parse(text, CultureInfo.InvariantCulture) },
            { "maximum", (slider, text) => slider.maximum = float.Parse(text, CultureInfo.InvariantCulture) },
            { "step", (slider, text) => slider.step = float.Parse(text, CultureInfo.InvariantCulture) },
            { "interactable", (slider, text) => slider.interactable = bool.Parse(text) },
        };

        public override Dictionary<string, string[]> Props => new Dictionary<string, string[]>
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

            var component = (ArmSpanSliderController)componentType.component;

            if (componentType.data.TryGetValue("formatter", out string formatterId))
            {
                if (!parserParams.actions.TryGetValue(formatterId, out BSMLAction formatter))
                {
                    throw new Exception($"Formatter action '{formatterId}' not found");
                }

                component.formatter = formatter;
            }

            if (componentType.data.TryGetValue("value", out string valueId))
            {
                if (!parserParams.values.TryGetValue(valueId, out BSMLValue value))
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
