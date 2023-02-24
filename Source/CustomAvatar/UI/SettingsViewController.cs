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

using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using Polyglot;
using TMPro;
using UnityEngine;
using Zenject;

namespace CustomAvatar.UI
{
    internal class SettingsViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.UI.Views.Settings.bsml";

        #region Values

        internal GeneralSettingsHost generalSettingsHost;
        internal AvatarSpecificSettingsHost avatarSpecificSettingsHost;
        internal AutomaticFbtCalibrationHost automaticFbtCalibrationHost;
        internal InterfaceSettingsHost interfaceSettingsHost;
        internal AdditionalTab[] additionalMenuTabs;

        #endregion

        private PlatformLeaderboardViewController _leaderboardViewController;
        private List<IAvatarsMenuTab> _avatarsMenuTabs;

        [Inject]
        internal void Construct(
            PlatformLeaderboardViewController leaderboardViewController,
            List<IAvatarsMenuTab> avatarsMenuTabs,
            GeneralSettingsHost generalSettingsHost,
            AvatarSpecificSettingsHost avatarSpecificSettingsHost,
            AutomaticFbtCalibrationHost automaticFbtCalibrationHost,
            InterfaceSettingsHost interfaceSettingsHost)
        {
            _leaderboardViewController = leaderboardViewController;
            _avatarsMenuTabs = avatarsMenuTabs;
            this.generalSettingsHost = generalSettingsHost;
            this.avatarSpecificSettingsHost = avatarSpecificSettingsHost;
            this.automaticFbtCalibrationHost = automaticFbtCalibrationHost;
            this.interfaceSettingsHost = interfaceSettingsHost;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                additionalMenuTabs = new AdditionalTab[_avatarsMenuTabs.Count];

                for (int i = 0; i < _avatarsMenuTabs.Count; ++i)
                {
                    additionalMenuTabs[i] = new AdditionalTab { name = _avatarsMenuTabs[i].name };
                }
            }

            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (firstActivation)
            {
                RectTransform header = Instantiate((RectTransform)_leaderboardViewController.transform.Find("HeaderPanel"), rectTransform, false);

                header.name = "HeaderPanel";

                Destroy(header.GetComponentInChildren<LocalizedTextMeshProUGUI>());

                TextMeshProUGUI textMesh = header.Find("Text").GetComponent<TextMeshProUGUI>();
                textMesh.text = "Settings";

                ImageView bg = header.Find("BG").GetComponent<ImageView>();
                bg.color0 = new Color(1, 1, 1, 0);
                bg.color1 = new Color(1, 1, 1, 1);

                for (int i = 0; i < _avatarsMenuTabs.Count; ++i)
                {
                    try
                    {
                        IAvatarsMenuTab tab = _avatarsMenuTabs[i];
                        AdditionalTab menuTab = additionalMenuTabs[i];
                        menuTab.name = tab.name;
                        BSMLParser.instance.Parse(tab.GetContent(), menuTab.gameObject, tab.host);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                    }
                }
            }

            generalSettingsHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            avatarSpecificSettingsHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            automaticFbtCalibrationHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            foreach (IAvatarsMenuTab tab in _avatarsMenuTabs)
            {
                if (tab.host is IViewControllerHost viewControllerHost)
                {
                    viewControllerHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
                }
            }
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            generalSettingsHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            avatarSpecificSettingsHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            automaticFbtCalibrationHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            foreach (IAvatarsMenuTab tab in _avatarsMenuTabs)
            {
                if (tab.host is IViewControllerHost viewControllerHost)
                {
                    viewControllerHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
                }
            }
        }

        public class AdditionalTab
        {
#pragma warning disable CS0649 // Field is never assigned to and will always have its default value
            [UIObject("plugin-tab")]
            internal GameObject gameObject;
#pragma warning restore CS0649

            public string name { get; set; }
        }
    }
}
