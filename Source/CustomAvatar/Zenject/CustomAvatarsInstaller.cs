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
using System.Reflection;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using CustomAvatar.Tracking;
using CustomAvatar.Tracking.OpenVR;
using CustomAvatar.Tracking.UnityXR;
using CustomAvatar.Utilities;
using Hive.Versioning;
using IPA.Loader;
using SiraUtil.Affinity;
using UnityEngine.XR;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar.Zenject
{
    internal class CustomAvatarsInstaller : Installer
    {
        public static readonly int kPlayerAvatarManagerExecutionOrder = 1000;

        private static readonly VersionRange kDynamicOpenVRVersionRange = new VersionRange("^0.5.0");
        private static readonly VersionRange kOpenXRHandsVersionRange = new VersionRange("^1.1.0");

        private static readonly MethodInfo kCreateLoggerMethod = typeof(ILoggerFactory).GetMethod(nameof(ILoggerFactory.CreateLogger), BindingFlags.Public | BindingFlags.Instance);
        private static readonly Assembly kAssembly = Assembly.GetExecutingAssembly();

        private readonly Logger _ipaLogger;
        private readonly ILogger<CustomAvatarsInstaller> _logger;
        private readonly PluginMetadata _pluginMetadata;
        private readonly PCAppInit _pcAppInit;

        public CustomAvatarsInstaller(Logger ipaLogger, PluginMetadata pluginMetadata, PCAppInit pcAppInit)
        {
            _ipaLogger = ipaLogger;
            _logger = new IPALogger<CustomAvatarsInstaller>(ipaLogger);
            _pluginMetadata = pluginMetadata;
            _pcAppInit = pcAppInit;
        }

        public override void InstallBindings()
        {
            // logging
            Container.Bind(typeof(ILoggerFactory)).To<IPALoggerFactory>().AsTransient().WithArguments(_ipaLogger);
            Container.Bind(typeof(ILogger<>)).FromMethodUntyped(CreateLogger).AsTransient();

            // settings
            SettingsManager settingsManager = Container.Instantiate<SettingsManager>();
            settingsManager.Load();

            Container.Bind<PluginMetadata>().FromInstance(_pluginMetadata).When(InjectedIntoThisAssembly);

            Container.Bind<SettingsManager>().FromInstance(settingsManager).AsSingle();
            Container.Bind<Settings>().FromMethod((ctx) => ctx.Container.Resolve<SettingsManager>().settings).AsTransient();
            Container.Bind(typeof(CalibrationData), typeof(IDisposable)).To<CalibrationData>().AsSingle();

            _logger.LogInformation($"Current Unity XR device: '{XRSettings.loadedDeviceName}'");

            if (ShouldUseOpenVR())
            {
                Container.Bind<OpenVRFacade>().AsTransient();
                Container.Bind(typeof(IDeviceProvider), typeof(ITickable)).To<OpenVRDeviceProvider>().AsSingle();
                Container.Bind(typeof(IFingerTrackingProvider), typeof(IInitializable), typeof(IDisposable)).To<OpenVRFingerTrackingProvider>().AsSingle();
            }
            else if (XRSettings.loadedDeviceName.IndexOf("OpenXR", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Container.Bind(typeof(IDeviceProvider), typeof(IInitializable), typeof(IDisposable)).To<UnityXRDeviceProvider>().AsSingle();

                if (IsPluginLoadedAndMatchesVersion("Unity.XR.Hands", kOpenXRHandsVersionRange))
                {
                    Container.Bind(typeof(IFingerTrackingProvider), typeof(ITickable)).To<UnityXRFingerTrackingProvider>().AsSingle();
                }
                else
                {
                    Container.Bind(typeof(IFingerTrackingProvider)).To<DevicelessFingerTrackingProvider>().AsSingle();
                }
            }
            else
            {
                Container.Bind(typeof(IDeviceProvider), typeof(IInitializable), typeof(IDisposable)).To<GenericDeviceProvider>().AsSingle();
                Container.Bind(typeof(IFingerTrackingProvider)).To<DevicelessFingerTrackingProvider>().AsSingle();
            }

            // managers
            Container.Bind(typeof(PlayerAvatarManager), typeof(IInitializable), typeof(IDisposable)).To<PlayerAvatarManager>().AsSingle().NonLazy();
            Container.Bind(typeof(ShaderLoader), typeof(IInitializable)).To<ShaderLoader>().AsSingle().NonLazy();

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

            Container.Bind<MainSettingsModelSO>().FromInstance(_pcAppInit._mainSettingsModel).IfNotBound();

            Container.Bind(typeof(IAffinity)).To<Patches.MirrorRendererSO>().AsSingle();
        }

        private bool ShouldUseOpenVR()
        {
            if (XRSettings.loadedDeviceName.IndexOf("OpenVR", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return false;
            }

            if (!IsPluginLoadedAndMatchesVersion("DynamicOpenVR", kDynamicOpenVRVersionRange))
            {
                _logger.LogError($"DynamicOpenVR is not installed or does not match expected version range '{kDynamicOpenVRVersionRange}'. OpenVR will not be used.");
                return false;
            }

            return true;
        }

        private bool IsPluginLoadedAndMatchesVersion(string id, VersionRange versionRange)
        {
            PluginMetadata plugin = PluginManager.GetPluginFromId(id);
            return plugin != null && versionRange.Matches(plugin.HVersion);
        }

        private object CreateLogger(InjectContext context)
        {
            Type genericType = context.MemberType.GenericTypeArguments[0];

            if (!genericType.IsAssignableFrom(context.ObjectType))
            {
                throw new InvalidOperationException($"Cannot create logger with generic type '{genericType}' for type '{context.ObjectType}'");
            }

            ILoggerFactory instance = context.Container.Resolve<ILoggerFactory>();

            return kCreateLoggerMethod.MakeGenericMethod(context.ObjectType).Invoke(instance, new object[] { null });
        }

        private bool InjectedIntoThisAssembly(InjectContext context)
        {
            return context.ObjectType.Assembly == kAssembly;
        }
    }
}
