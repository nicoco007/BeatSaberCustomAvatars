//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using HMUI;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CustomAvatar.UI
{
    internal class AvatarListTableCell : TableCell
    {
        private static readonly Color kSelectedBackgroundColor = new Color(0, 0.75f, 1, 1);
        private static readonly Color kHighlightedBackgroundColor = new Color(1, 1, 1, 0.2f);
        private static readonly Color kSelectedAndHighlightedBackgroundColor = new Color(0, 0.75f, 1, 0.75f);

        public ImageView backgroundImage => _backgroundImage;
        public CurvedTextMeshPro nameText => _nameText;
        public CurvedTextMeshPro authorText => _authorText;
        public ImageView cover => _cover;

        [SerializeField] private ImageView _backgroundImage;
        [SerializeField] private CurvedTextMeshPro _nameText;
        [SerializeField] private CurvedTextMeshPro _authorText;
        [SerializeField] private ImageView _cover;
        [SerializeField] private Signal _wasPressedSignal;

        public void Init(LevelListTableCell originalTableCell)
        {
            _backgroundImage = transform.Find("Background").GetComponent<ImageView>();
            _nameText = transform.Find("SongName").GetComponent<CurvedTextMeshPro>();
            _authorText = transform.Find("SongAuthor").GetComponent<CurvedTextMeshPro>();
            _cover = transform.Find("CoverImage").GetComponent<ImageView>();

            _nameText.richText = false;
            _authorText.richText = false;

            _nameText.name = "AvatarName";
            _authorText.name = "AvatarAuthor";

            _wasPressedSignal = originalTableCell.GetField<Signal, SelectableCell>("_wasPressedSignal");
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
            base.OnPointerClick(eventData);
            _wasPressedSignal?.Raise();
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

        private void RefreshVisuals()
        {
            if (selected && highlighted)
            {
                backgroundImage.color = kSelectedAndHighlightedBackgroundColor;
            }
            else if (highlighted)
            {
                backgroundImage.color = kHighlightedBackgroundColor;
            }
            else if (selected)
            {
                backgroundImage.color = kSelectedBackgroundColor;
            }

            backgroundImage.enabled = selected || highlighted;
        }
    }
}
