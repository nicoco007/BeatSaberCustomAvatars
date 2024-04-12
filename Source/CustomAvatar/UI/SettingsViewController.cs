//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using CustomAvatar.Tracking;
using HMUI;
using IPA.Loader;
using UnityEngine;
using Zenject;

namespace CustomAvatar.UI
{
    [ViewDefinition("CustomAvatar.UI.Views.Settings.bsml")]
    [HotReload(RelativePathToLayout = "Views/Settings.bsml")]
    internal class SettingsViewController : BSMLAutomaticViewController
    {
        #region Values

        protected GeneralSettingsHost generalSettingsHost;
        protected AvatarSpecificSettingsHost avatarSpecificSettingsHost;
        protected AutomaticFbtCalibrationHost automaticFbtCalibrationHost;
        protected InterfaceSettingsHost interfaceSettingsHost;
        protected AdditionalTab[] additionalMenuTabs;
        protected string versionText;

        #endregion

        private List<IAvatarsMenuTab> _avatarsMenuTabs;
        private PluginMetadata _pluginMetadata;
        private TrackingRig _trackingRig;
        private BSMLParser _bsmlParser;

        [Inject]
        internal void Construct(
            List<IAvatarsMenuTab> avatarsMenuTabs,
            PluginMetadata pluginMetadata,
            TrackingRig trackingRig,
            GeneralSettingsHost generalSettingsHost,
            AvatarSpecificSettingsHost avatarSpecificSettingsHost,
            AutomaticFbtCalibrationHost automaticFbtCalibrationHost,
            InterfaceSettingsHost interfaceSettingsHost,
            BSMLParser bsmlParser)
        {
            _avatarsMenuTabs = avatarsMenuTabs;
            _pluginMetadata = pluginMetadata;
            _trackingRig = trackingRig;
            _bsmlParser = bsmlParser;
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

            versionText = $"Custom Avatars v{_pluginMetadata.HVersion}";

            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (firstActivation)
            {
                for (int i = 0; i < _avatarsMenuTabs.Count; ++i)
                {
                    try
                    {
                        IAvatarsMenuTab tab = _avatarsMenuTabs[i];
                        AdditionalTab menuTab = additionalMenuTabs[i];
                        menuTab.name = tab.name;
                        _bsmlParser.Parse(tab.GetContent(), menuTab.gameObject, tab.host);
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

            foreach (IViewControllerHost viewControllerHost in _avatarsMenuTabs.Select(t => t.host).OfType<IViewControllerHost>())
            {
                viewControllerHost.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            }

            _trackingRig.showRenderModels = true;
        }

        public override void __Init(HMUI.Screen screen, ViewController parentViewController, ContainerViewController containerViewController)
        {
            base.__Init(screen, parentViewController, containerViewController);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            generalSettingsHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            avatarSpecificSettingsHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            automaticFbtCalibrationHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            foreach (IViewControllerHost viewControllerHost in _avatarsMenuTabs.Select(t => t.host).OfType<IViewControllerHost>())
            {
                viewControllerHost.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            }

            _trackingRig.showRenderModels = false;
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
