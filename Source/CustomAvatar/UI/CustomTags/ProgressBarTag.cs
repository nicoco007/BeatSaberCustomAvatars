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
using BGLib.Polyglot;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomAvatar.UI.CustomTags
{
    internal class ProgressBarTag : BSMLTag
    {
        private GameObject _prefab;

        public override string[] Aliases { get; } = ["ca.progress-bar"];

        public override void Initialize()
        {
            base.Initialize();

            PlatformLeaderboardViewController _platformLeaderboardViewController = DiContainer.Resolve<PlatformLeaderboardViewController>();

            RectTransform containerTransform = (RectTransform)Object.Instantiate(_platformLeaderboardViewController.transform.Find("Container/LeaderboardTableView/LoadingControl/DownloadingContainer"));
            containerTransform.name = "ProgressContainer";
            containerTransform.anchorMin = new Vector2(0.5f, 1);
            containerTransform.anchorMax = new Vector2(0.5f, 0);
            containerTransform.sizeDelta = new Vector2(120, 0);

            RectTransform progressBarTransform = (RectTransform)containerTransform.Find("DownloadingProgress");
            progressBarTransform.name = "ProgressBar";
            progressBarTransform.anchorMin = new Vector2(0.2f, 0.5f);
            progressBarTransform.anchorMax = new Vector2(0.8f, 0.5f);

            RectTransform progressBackgroundTransform = (RectTransform)containerTransform.Find("DownloadingBG");
            progressBackgroundTransform.name = "ProgressBG";
            progressBackgroundTransform.anchorMin = new Vector2(0.2f, 0.5f);
            progressBackgroundTransform.anchorMax = new Vector2(0.8f, 0.5f);

            Image progressBackgroundImage = progressBackgroundTransform.GetComponent<Image>();
            progressBackgroundImage.color = new Color(1, 1, 1, 0.2f);

            RectTransform progressTitleTransform = (RectTransform)containerTransform.Find("DownloadingText");
            progressTitleTransform.name = "ProgressTitle";

            Object.Destroy(progressTitleTransform.GetComponent<LocalizedTextMeshProUGUI>());

            TextMeshProUGUI title = progressTitleTransform.GetComponent<TextMeshProUGUI>();
            title.overflowMode = TextOverflowModes.Ellipsis;

            GameObject progressTextObject = new("ProgressText", typeof(RectTransform));
            RectTransform progressTextTransform = (RectTransform)progressTextObject.transform;
            progressTextTransform.SetParent(containerTransform, false);
            progressTextTransform.anchorMin = new Vector2(1, 0.5f);
            progressTextTransform.anchorMax = new Vector2(0, 0.5f);
            progressTextTransform.anchoredPosition = new Vector2(0, -4);

            // CurvedTextMeshPro doesn't save fontSize properly when inactive
            GameObject containerGameObject = containerTransform.gameObject;
            containerGameObject.SetActive(true);

            CurvedTextMeshPro description = progressTextObject.AddComponent<CurvedTextMeshPro>();
            description.fontMaterial = title.fontMaterial;
            description.fontSize = 3;
            description.alignment = TextAlignmentOptions.Center;
            description.enableWordWrapping = false;
            description.fontStyle = FontStyles.Italic;

            containerGameObject.SetActive(false);

            containerGameObject.AddComponent<ProgressBar>().Init(progressBarTransform.GetComponent<Image>(), title, description);

            _prefab = containerGameObject;
        }

        public override GameObject CreateObject(Transform parent)
        {
            return Object.Instantiate(_prefab, parent);
        }
    }
}
