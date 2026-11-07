using BeatSaberMarkupLanguage.Tags;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomAvatar.UI.CustomTags
{
    internal class SmallTextSegmentedControlTag : TextSegmentedControlTag
    {
        public override string[] Aliases { get; } = ["ca.small-text-segments"];

        public override GameObject CreateObject(Transform parent)
        {
            GameObject gameObject = base.CreateObject(parent);

            HorizontalLayoutGroup horizontalLayoutGroup = gameObject.GetComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextSegmentedControl control = gameObject.GetComponent<TextSegmentedControl>();
            control.dataSource = new DataSource(control);

            return gameObject;
        }

        private class DataSource : SegmentedControl.IDataSource
        {
            private readonly TextSegmentedControl _textSegmentedControl;

            public DataSource(TextSegmentedControl textSegmentedControl)
            {
                _textSegmentedControl = textSegmentedControl;
            }

            public SegmentedControlCell CellForCellNumber(int cellNumber)
            {
                SegmentedControlCell cell = _textSegmentedControl.CellForCellNumber(cellNumber);

                cell.transform.Find("BG").gameObject.SetActive(true);
                cell.gameObject.AddComponent<StackLayoutGroup>();
                TextMeshProUGUI text = cell.transform.Find("Text").GetComponent<TextMeshProUGUI>();
                text.margin = new Vector4(2, 0, 2, 0);

                return cell;
            }

            public int NumberOfCells() => _textSegmentedControl.NumberOfCells();
        }
    }
}
