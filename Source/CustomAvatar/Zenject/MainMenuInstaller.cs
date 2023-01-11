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
using CustomAvatar.Lighting;
using CustomAvatar.Player;
using CustomAvatar.UI;
using CustomAvatar.UI.CustomTags;
using HMUI;
using UnityEngine;
using VRUIControls;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class MainMenuInstaller : Installer
    {
        private const float kCenterViewControllerWidth = 160;
        private const float kSideViewControllerWidth = 120;

        private readonly ValuePickerTag _armSpanSliderTag;

        public MainMenuInstaller(ValuePickerTag armSpanSliderTag)
        {
            _armSpanSliderTag = armSpanSliderTag;
        }

        public override void InstallBindings()
        {
            Container.Bind(typeof(ITickable)).To<KeyboardInputHandler>().AsSingle().NonLazy();

            CreateAndBindViewController<AvatarListViewController>(kSideViewControllerWidth);
            CreateAndBindViewController<MirrorViewController>(kCenterViewControllerWidth);
            SettingsViewController settingsViewController = CreateAndBindViewController<SettingsViewController>(kSideViewControllerWidth);

            Container.Bind<GeneralSettingsHost>().AsSingle();
            Container.Bind<AvatarSpecificSettingsHost>().AsSingle();
            Container.Bind<AutomaticFbtCalibrationHost>().AsSingle();
            Container.Bind<InterfaceSettingsHost>().AsSingle();

            Container.Bind<ArmSpanMeasurer>().FromNewComponentOn(settingsViewController.gameObject).AsSingle();
            Container.Bind<ManualCalibrationHelper>().FromNewComponentOn(settingsViewController.gameObject).AsSingle();

            Container.Bind(typeof(IInitializable), typeof(IDisposable)).To<AvatarMenuFlowCoordinator>().FromNewComponentOnNewGameObject();

            Container.Bind(typeof(IInitializable), typeof(IDisposable)).To<MenuLightingCreator>().AsSingle().NonLazy();

            _armSpanSliderTag.Init(Container);
        }

        private T CreateAndBindViewController<T>(float width) where T : ViewController
        {
            var gameObject = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(Touchable), typeof(Canvas), typeof(CanvasGroup), typeof(VRGraphicRaycaster), typeof(T));

            T viewController = gameObject.GetComponent<T>();
            viewController.gameObject.layer = 5;

            RectTransform rectTransform = viewController.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(width, 0);
            rectTransform.offsetMin = new Vector2(-width / 2f, 0);
            rectTransform.offsetMax = new Vector2(width / 2f, 0);

            Canvas canvas = viewController.GetComponent<Canvas>();
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;

            Container.QueueForInject(gameObject.GetComponent<VRGraphicRaycaster>());
            Container.QueueForInject(viewController);

            Container.Bind<T>().FromInstance(viewController);

            return viewController;
        }
    }
}
