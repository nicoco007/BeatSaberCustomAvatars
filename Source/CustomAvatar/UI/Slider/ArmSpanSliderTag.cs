using BeatSaberMarkupLanguage.Tags;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CustomAvatar.UI.Slider
{
    internal class ArmSpanSliderTag : BSMLTag
    {
        private GameObject _valueControllerTemplate;

        public override string[] Aliases => new[] { "slider" };

        public void Init(DiContainer container)
        {
            _valueControllerTemplate = container.Resolve<SettingsNavigationController>().transform.Find("GraphicSettings/ViewPort/Content/VRRenderingScale/ValuePicker").gameObject;
        }

        public override GameObject CreateObject(Transform parent)
        {
            if (!_valueControllerTemplate)
            {
                throw new System.Exception($"{nameof(ArmSpanSliderTag)} can only be used after the menu has loaded");
            }

            GameObject gameObject = Object.Instantiate(_valueControllerTemplate, parent, false);
            Object.Destroy(gameObject.GetComponent<StepValuePicker>());
            gameObject.name = "BSMLSlider";

            gameObject.AddComponent<ArmSpanSliderController>();

            LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 30;

            return gameObject;
        }
    }
}
