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
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Lighting;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using CustomAvatar.Tracking;
using CustomAvatar.Tracking.OpenVR;
using CustomAvatar.Tracking.UnityXR;
using CustomAvatar.Utilities;
using IPA.Logging;
using IPA.Utilities;
using SiraUtil.Affinity;
using UnityEngine.XR;
using Valve.VR;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar.Zenject
{
    internal class CustomAvatarsInstaller : Installer
    {
        public static readonly int kPlayerAvatarManagerExecutionOrder = 1000;
        private static readonly LightIntensityData kLightIntensityData = new LightIntensityData
        {
            ambient = 0.4f,
            bloomNonLightRenderer = 0.5f,
            directionalLight = 0.25f,
            materialLightIntensityMultiplier = 2,
            tubeBloomPrePassLight = 0.4f,
            parametric3SliceSprite = 2f,
            parametricBoxLight = 12f,
            spriteLight = 0.8f,
        };

        private readonly Logger _ipaLogger;
        private readonly PCAppInit _pcAppInit;

        public CustomAvatarsInstaller(Logger ipaLogger, PCAppInit pcAppInit)
        {
            _ipaLogger = ipaLogger;
            _pcAppInit = pcAppInit;
        }

        public override void InstallBindings()
        {
            // logging
            Container.Bind(typeof(ILogger<>)).FromMethodUntyped(CreateLogger).AsTransient();

            // settings
            SettingsManager settingsManager = Container.Instantiate<SettingsManager>();
            settingsManager.Load();

            Container.Bind<SettingsManager>().FromInstance(settingsManager).AsSingle();
            Container.Bind<Settings>().FromMethod((ctx) => ctx.Container.Resolve<SettingsManager>().settings).AsTransient();
            Container.Bind(typeof(CalibrationData), typeof(IDisposable)).To<CalibrationData>().AsSingle();

            if (XRSettings.loadedDeviceName.Equals("openvr", StringComparison.InvariantCultureIgnoreCase) &&
                OpenVR.IsRuntimeInstalled() &&
                OpenVR.System != null &&
                !Environment.GetCommandLineArgs().Contains("--force-xr"))
            {
                Container.Bind<OpenVRFacade>().AsTransient();
                Container.Bind(typeof(IDeviceProvider)).To<OpenVRDeviceProvider>().AsSingle();
            }
            else
            {
                Container.Bind(typeof(IDeviceProvider), typeof(IInitializable), typeof(IDisposable)).To<UnityXRDeviceProvider>().AsSingle();
            }

            // managers
            Container.Bind(typeof(PlayerAvatarManager), typeof(IInitializable), typeof(IDisposable)).To<PlayerAvatarManager>().AsSingle().NonLazy();
            Container.Bind(typeof(ShaderLoader), typeof(IInitializable)).To<ShaderLoader>().AsSingle().NonLazy();
            Container.Bind(typeof(DeviceManager), typeof(ITickable)).To<DeviceManager>().AsSingle().NonLazy();

            // this prevents a race condition when registering components in AvatarSpawner
            Container.BindExecutionOrder<PlayerAvatarManager>(kPlayerAvatarManagerExecutionOrder);

            Container.Bind<AvatarLoader>().AsSingle();
            Container.Bind<AvatarSpawner>().AsSingle();
            Container.Bind<ActiveCameraManager>().AsSingle();
            Container.Bind<ActivePlayerSpaceManager>().AsSingle();
            Container.Bind(typeof(VRPlayerInput), typeof(IInitializable), typeof(IDisposable)).To<VRPlayerInput>().AsSingle();
            Container.Bind(typeof(VRPlayerInputInternal), typeof(IInitializable), typeof(IDisposable)).To<VRPlayerInputInternal>().AsSingle();
            Container.Bind(typeof(IInitializable), typeof(IDisposable)).To<QualitySettingsController>().AsSingle();
            Container.Bind(typeof(BeatSaberUtilities), typeof(IInitializable), typeof(IDisposable)).To<BeatSaberUtilities>().AsSingle();

#pragma warning disable CS0612
            Container.Bind(typeof(FloorController), typeof(IInitializable), typeof(IDisposable)).To<FloorController>().AsSingle();
#pragma warning restore CS0612

            // helper classes
            Container.Bind<MirrorHelper>().AsTransient();
            Container.Bind<IKHelper>().AsTransient();
            Container.Bind<TrackingHelper>().AsTransient();

            Container.Bind<MainSettingsModelSO>().FromInstance(_pcAppInit.GetField<MainSettingsModelSO, PCAppInit>("_mainSettingsModel")).IfNotBound();

            if (settingsManager.settings.lighting.environment.enabled)
            {
                Container.Bind(typeof(DynamicLightCreator), typeof(IInitializable), typeof(IDisposable)).To<DynamicLightCreator>().AsSingle().NonLazy();
                Container.Bind<LightIntensityData>().FromInstance(kLightIntensityData);
            }

            Container.Bind(typeof(IAffinity)).To<Patches.MirrorRendererSO>().AsSingle();
        }

        private object CreateLogger(InjectContext context)
        {
            Type genericType = context.MemberType.GenericTypeArguments[0];

            return genericType.IsAssignableFrom(context.ObjectType)
                ? Activator.CreateInstance(typeof(IPALogger<>).MakeGenericType(genericType), _ipaLogger.GetChildLogger(genericType.Name))
                : throw new InvalidOperationException($"Cannot create logger with generic type '{genericType}' for type '{context.ObjectType}'");
        }
    }
}
