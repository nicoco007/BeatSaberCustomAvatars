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
