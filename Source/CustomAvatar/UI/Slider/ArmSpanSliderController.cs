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

using BeatSaberMarkupLanguage.Parser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomAvatar.UI.Slider
{
    internal class ArmSpanSliderController : MonoBehaviour
    {
        private float _value;
        private float _minimum = float.MinValue;
        private float _maximum = float.MaxValue;
        private bool _interactable = true;

        private TextMeshProUGUI _text;
        private Button _incButton;
        private Button _decButton;

        public float value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp(Mathf.Round(value / step) * step, _minimum, _maximum);
                UpdateControl();
            }
        }

        public float minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;
                _value = Mathf.Clamp(Mathf.Round(_value / step) * step, value, _maximum);
                UpdateControl();
            }
        }

        public float maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;
                _value = Mathf.Clamp(Mathf.Round(_value / step) * step, _minimum, value);
                UpdateControl();
            }
        }

        public float step { get; set; } = 1;

        public BSMLAction formatter { get; set; }

        public BSMLValue associatedValue { get; set; }

        public bool interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                UpdateControl();
            }
        }

        internal void Awake()
        {
            Transform transform = base.transform;

            _text = transform.Find("ValueText").GetComponent<TextMeshProUGUI>();
            _incButton = transform.Find("IncButton").GetComponent<Button>();
            _decButton = transform.Find("DecButton").GetComponent<Button>();

            _incButton.onClick.RemoveAllListeners();
            _decButton.onClick.RemoveAllListeners();
        }

        internal void OnEnable()
        {
            _incButton.onClick.AddListener(OnIncButtonClicked);
            _decButton.onClick.AddListener(OnDecButtonClicked);

            UpdateControl();
        }

        internal void OnDisable()
        {
            _incButton.onClick.RemoveListener(OnIncButtonClicked);
            _decButton.onClick.RemoveListener(OnDecButtonClicked);
        }

        private void OnIncButtonClicked()
        {
            value += step;
            associatedValue?.SetValue(value);
        }

        private void OnDecButtonClicked()
        {
            value -= step;
            associatedValue?.SetValue(value);
        }

        private void UpdateControl()
        {
            _text.text = formatter?.Invoke(_value)?.ToString() ?? _value.ToString();

            _incButton.interactable = interactable && value + step <= maximum;
            _decButton.interactable = interactable && value - step >= minimum;
        }
    }
}
