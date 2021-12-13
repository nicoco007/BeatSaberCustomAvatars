//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
