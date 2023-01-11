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

using BeatSaberMarkupLanguage.Tags;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CustomAvatar.UI.CustomTags
{
    internal class ValuePickerTag : BSMLTag
    {
        private GameObject _valueControllerTemplate;

        public override string[] Aliases => new[] { "value-picker" };

        public void Init(DiContainer container)
        {
            _valueControllerTemplate = container.Resolve<SettingsNavigationController>().transform.Find("GraphicSettings/ViewPort/Content/VRRenderingScale/ValuePicker").gameObject;
        }

        public override GameObject CreateObject(Transform parent)
        {
            if (!_valueControllerTemplate)
            {
                throw new System.Exception($"{nameof(ValuePickerTag)} can only be used after the menu has loaded");
            }

            GameObject gameObject = Object.Instantiate(_valueControllerTemplate, parent, false);
            Object.Destroy(gameObject.GetComponent<StepValuePicker>());
            gameObject.name = "BSMLSlider";

            gameObject.AddComponent<ValuePickerController>();

            LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 30;

            return gameObject;
        }
    }
}
