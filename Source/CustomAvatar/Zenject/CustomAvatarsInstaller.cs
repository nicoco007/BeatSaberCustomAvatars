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

using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Lighting;
using CustomAvatar.Logging;
using CustomAvatar.StereoRendering;
using CustomAvatar.Tracking.OpenVR;
using CustomAvatar.Tracking.UnityXR;
using CustomAvatar.Utilities;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar.Zenject
{
    internal class CustomAvatarsInstaller : Installer
    {
        private readonly Logger _logger;

        public CustomAvatarsInstaller(Logger logger)
        {
            _logger = logger;
        }

        public override void InstallBindings()
        {
            // logging
            Container.Bind<ILoggerProvider>().FromMethod((context) => context.Container.Instantiate<IPALoggerProvider>(new object[] { _logger })).AsTransient();

            // settings
            Container.BindInterfacesAndSelfTo<SettingsManager>().AsSingle();
            Container.Bind<Settings>().FromMethod((context) => context.Container.Resolve<SettingsManager>().settings);
            Container.BindInterfacesAndSelfTo<CalibrationData>().AsSingle();

            if (XRSettings.loadedDeviceName.Equals("openvr", System.StringComparison.InvariantCultureIgnoreCase) && OpenVR.IsRuntimeInstalled())
            {
                Container.BindInterfacesAndSelfTo<OpenVRFacade>().AsTransient();
                Container.BindInterfacesTo<OpenVRDeviceManager>().AsSingle().NonLazy();
            }
            else
            {
                Container.BindInterfacesTo<UnityXRDeviceManager>().AsSingle().NonLazy();
            }

            // managers
            Container.BindInterfacesAndSelfTo<PlayerAvatarManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<MainCameraController>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<KeyboardInputHandler>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ShaderLoader>().AsSingle().NonLazy();

            Container.Bind<AvatarLoader>().AsSingle();
            Container.BindInterfacesAndSelfTo<LightingQualityController>().AsSingle();

            // helper classes
            Container.Bind<AvatarTailor>().AsTransient();
            Container.Bind<MirrorHelper>().AsTransient();
            Container.Bind<AvatarSpawner>().AsTransient();
            Container.Bind<IKHelper>().AsTransient();

            // not sure if this is a great idea but w/e
            if (!Container.HasBinding<MainSettingsModelSO>())
            {
                Container.Bind<MainSettingsModelSO>().FromInstance(Resources.FindObjectsOfTypeAll<MainSettingsModelSO>().First());
            }
        }
    }
}
