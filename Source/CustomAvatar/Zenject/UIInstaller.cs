//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Player;
using CustomAvatar.UI;
using HMUI;
using UnityEngine;
using VRUIControls;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class UIInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<KeyboardInputHandler>().AsSingle().NonLazy();

            CreateViewController<AvatarListViewController>();
            CreateViewController<MirrorViewController>();
            CreateViewController<SettingsViewController>();

            Container.BindInterfacesAndSelfTo<AvatarMenuFlowCoordinator>().FromNewComponentOnNewGameObject(nameof(AvatarMenuFlowCoordinator));
        }

        private T CreateViewController<T>() where T : ViewController
        {
            GameObject gameObject = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(Touchable), typeof(Canvas), typeof(CanvasGroup));

            Container.InstantiateComponent<VRGraphicRaycaster>(gameObject);

            T viewController = Container.InstantiateComponent<T>(gameObject);

            RectTransform rectTransform = viewController.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(160, 0);
            rectTransform.offsetMin = new Vector2(-80, 0);
            rectTransform.offsetMax = new Vector2(80, 0);

            Canvas canvas = viewController.GetComponent<Canvas>();
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;

            Container.Bind<T>().FromInstance(viewController);

            return viewController;
        }
    }
}
