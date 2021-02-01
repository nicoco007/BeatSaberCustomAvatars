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

using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Lighting;
using CustomAvatar.Logging;
using CustomAvatar.Rendering;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using CustomAvatar.Tracking.OpenVR;
using CustomAvatar.Tracking.UnityXR;
using CustomAvatar.Utilities;
using UnityEngine.XR;
using Valve.VR;
using Zenject;
using Logger = IPA.Logging.Logger;
using System;
using IPA.Utilities;

namespace CustomAvatar.Zenject
{
    internal class CustomAvatarsInstaller : Installer
    {
        public static readonly int kPlayerAvatarManagerExecutionOrder = 1000;

        private readonly Logger _logger;
        private readonly PCAppInit _pcAppInit;

        public CustomAvatarsInstaller(Logger logger, PCAppInit pcAppInit)
        {
            _logger = logger;
            _pcAppInit = pcAppInit;
        }

        public override void InstallBindings()
        {
            // logging
            Container.Bind(typeof(ILogger<>)).FromMethodUntyped(CreateLogger).AsTransient().When(ShouldCreateLogger);
            
            // settings
            Container.BindInterfacesAndSelfTo<SettingsManager>().AsSingle();
            Container.Bind<Settings>().FromMethod((context) => context.Container.Resolve<SettingsManager>().settings);
            Container.BindInterfacesAndSelfTo<CalibrationData>().AsSingle();

            if (XRSettings.loadedDeviceName.Equals("openvr", StringComparison.InvariantCultureIgnoreCase) &&
                OpenVR.IsRuntimeInstalled() &&
                !Environment.GetCommandLineArgs().Contains("--force-xr"))
            {
                Container.Bind<OpenVRFacade>().AsTransient();
                Container.BindInterfacesAndSelfTo<OpenVRDeviceProvider>().AsSingle();
            }
            else
            {
                Container.BindInterfacesTo<UnityXRDeviceProvider>().AsSingle();
            }

            // managers
            Container.BindInterfacesAndSelfTo<PlayerAvatarManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ShaderLoader>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<DeviceManager>().AsSingle().NonLazy();

            // this prevents a race condition when registering components in AvatarSpawner
            Container.BindExecutionOrder<PlayerAvatarManager>(kPlayerAvatarManagerExecutionOrder);

            Container.Bind<AvatarLoader>().AsSingle();
            Container.Bind<AvatarSpawner>().AsSingle();
            Container.BindInterfacesAndSelfTo<VRPlayerInput>().AsSingle();
            Container.BindInterfacesAndSelfTo<FloorController>().AsSingle();
            Container.BindInterfacesAndSelfTo<LightingQualityController>().AsSingle();
            Container.BindInterfacesAndSelfTo<BeatSaberUtilities>().AsSingle();

            // helper classes
            Container.Bind<MirrorHelper>().AsTransient();
            Container.Bind<IKHelper>().AsTransient();
            Container.Bind<TrackingHelper>().AsTransient();

            Container.Bind<MainSettingsModelSO>().FromInstance(_pcAppInit.GetField<MainSettingsModelSO, PCAppInit>("_mainSettingsModel")).IfNotBound();
        }

        private object CreateLogger(InjectContext context)
        {
            return Activator.CreateInstance(typeof(IPALogger<>).MakeGenericType(context.MemberType.GenericTypeArguments[0]), _logger);
        }

        private bool ShouldCreateLogger(InjectContext context)
        {
            return context.ObjectType == context.MemberType.GenericTypeArguments[0];
        }
    }
}
