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

using System.ComponentModel;
using HMUI;
using IPA.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CustomAvatar.UI
{
    internal class AvatarListTableCell : TableCell
    {
        private static readonly Color kSelectedBackgroundColor = new Color(0, 0.75f, 1, 1);
        private static readonly Color kHighlightedBackgroundColor = new Color(1, 1, 1, 0.2f);
        private static readonly Color kSelectedAndHighlightedBackgroundColor = new Color(0, 0.75f, 1, 0.75f);

        private static readonly Color kRegularTextColor = Color.white;
        private static readonly Color kErrorTextColor = new Color(0.65f, 0.11f, 0.16f);

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private ImageView _backgroundImage;
        [SerializeField] private CurvedTextMeshPro _nameText;
        [SerializeField] private CurvedTextMeshPro _authorText;
        [SerializeField] private ImageView _cover;
        [SerializeField] private Signal _wasPressedSignal;
        [SerializeField] private GameObject _progressObject;
        [SerializeField] private Image _progressBar;
        [SerializeField] private TextMeshProUGUI _progressText;

        private AvatarListItem _listItem;

        public AvatarListItem listItem
        {
            get => _listItem;
            set
            {
                if (_listItem != null)
                {
                    _listItem.PropertyChanged -= OnPropertyChanged;
                }

                _listItem = value;
                _listItem.PropertyChanged += OnPropertyChanged;
                UpdateData();
            }
        }

        public void Init(LevelListTableCell originalTableCell, PlatformLeaderboardViewController platformLeaderboardViewController)
        {
            _canvasGroup = originalTableCell.GetField<CanvasGroup, LevelListTableCell>("_canvasGroup");
            _backgroundImage = transform.Find("Background").GetComponent<ImageView>();
            _nameText = transform.Find("SongName").GetComponent<CurvedTextMeshPro>();
            _authorText = transform.Find("SongAuthor").GetComponent<CurvedTextMeshPro>();
            _cover = transform.Find("CoverImage").GetComponent<ImageView>();

            _nameText.richText = false;
            _authorText.richText = false;

            _nameText.name = "AvatarName";
            _authorText.name = "AvatarAuthor";

            _nameText.rectTransform.offsetMax = new Vector2(0, _nameText.rectTransform.offsetMax.y);
            _authorText.rectTransform.offsetMax = new Vector2(0, _authorText.rectTransform.offsetMax.y);

            _wasPressedSignal = originalTableCell.GetField<Signal, SelectableCell>("_wasPressedSignal");

            var containerTransform = (RectTransform)Instantiate(platformLeaderboardViewController.transform.Find("Container/LeaderboardTableView/LoadingControl/DownloadingContainer"));
            containerTransform.SetParent(gameObject.transform, false);
            containerTransform.name = "ProgressContainer";
            containerTransform.anchorMin = new Vector2(0, 0.5f);
            containerTransform.anchorMax = new Vector2(1, 0.5f);
            containerTransform.offsetMin = new Vector2(6, -0.3f);
            containerTransform.offsetMax = new Vector2(0, -3.3f);

            _progressObject = containerTransform.gameObject;
            _progressObject.SetActive(true);

            var progressBarTransform = (RectTransform)containerTransform.Find("DownloadingProgress");
            progressBarTransform.name = "ProgressBar";
            progressBarTransform.offsetMin = new Vector2(4, -0.75f);
            progressBarTransform.offsetMax = new Vector2(-10, 0.75f);
            _progressBar = progressBarTransform.GetComponent<Image>();
            _progressBar.fillAmount = 0;

            var progressBackgroundTransform = (RectTransform)containerTransform.Find("DownloadingBG");
            progressBackgroundTransform.name = "ProgressBG";
            progressBackgroundTransform.offsetMin = new Vector2(4, -0.75f);
            progressBackgroundTransform.offsetMax = new Vector2(-10, 0.75f);

            var progressTitleTransform = (RectTransform)containerTransform.Find("DownloadingText");
            DestroyImmediate(progressTitleTransform.gameObject);

            var progressTextObject = new GameObject("ProgressText", typeof(RectTransform));
            var progressTextTransform = (RectTransform)progressTextObject.transform;
            progressTextTransform.SetParent(containerTransform, false);
            _progressText = progressTextObject.AddComponent<CurvedTextMeshPro>();
            _progressText.font = _nameText.font;
            _progressText.fontMaterial = _nameText.fontMaterial;
            _progressText.fontSize = 3;
            _progressText.alignment = TextAlignmentOptions.BaselineRight;
            _progressText.enableWordWrapping = false;
            _progressText.text = "0%";
            _progressText.fontStyle = FontStyles.Italic;
            _progressText.autoSizeTextContainer = false;
            progressTextTransform.anchorMin = new Vector2(1, 0.5f);
            progressTextTransform.anchorMax = new Vector2(1, 0.5f);
            progressTextTransform.sizeDelta = new Vector2(6, 5);
            progressTextTransform.anchoredPosition = new Vector2(-6, -0.9f);

            _progressObject.SetActive(false);
        }

        protected override void Start()
        {
            base.Start();

            // this is needed because TMPro sometimes forgets the font size after instantiating
            _nameText.fontSize = 4;
            _authorText.fontSize = 3;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (interactable)
            {
                _wasPressedSignal?.Raise();
            }

            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            _wasPressedSignal?.Raise();
        }

        protected override void HighlightDidChange(TransitionType transitionType)
        {
            RefreshVisuals();
        }

        protected override void SelectionDidChange(TransitionType transitionType)
        {
            RefreshVisuals();
        }

        private void OnDestroy()
        {
            if (_listItem != null)
            {
                _listItem.PropertyChanged -= OnPropertyChanged;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            UpdateData();
        }

        private void UpdateData()
        {
            _cover.sprite = listItem.icon;

            if (listItem.loadException != null)
            {
                interactable = false;
                _nameText.text = $"Failed to load {listItem.name}";
                _nameText.color = kErrorTextColor;
                _authorText.text = $"{listItem.loadException.GetType().Name}: {listItem.loadException.Message}";
                _authorText.color = kErrorTextColor;
                _authorText.gameObject.SetActive(true);
                _progressObject.SetActive(false);
            }
            else
            {
                interactable = listItem.isLoaded;
                _nameText.text = listItem.name;
                _nameText.color = kRegularTextColor;
                _authorText.text = listItem.author;
                _authorText.color = kRegularTextColor;
                _authorText.gameObject.SetActive(listItem.isLoaded);
                _progressObject.SetActive(!listItem.isLoaded);
                _progressBar.fillAmount = listItem.loadProgress;
                _progressText.text = $"{listItem.loadProgress * 100:0}%";
            }

            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            _canvasGroup.alpha = 1f;
            _authorText.alpha = 0.75f;

            if (selected && highlighted)
            {
                _backgroundImage.color = kSelectedAndHighlightedBackgroundColor;
            }
            else if (highlighted)
            {
                _backgroundImage.color = kHighlightedBackgroundColor;
            }
            else if (selected)
            {
                _backgroundImage.color = kSelectedBackgroundColor;
            }

            _backgroundImage.enabled = interactable && (selected || highlighted);
        }
    }
}
