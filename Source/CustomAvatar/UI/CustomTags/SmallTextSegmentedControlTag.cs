//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
