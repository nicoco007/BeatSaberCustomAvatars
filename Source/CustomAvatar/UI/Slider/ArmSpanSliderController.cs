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
