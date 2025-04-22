﻿//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using Valve.VR;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar.Zenject
{
    internal class CustomAvatarsInstaller : Installer
    {
        private const string kXRHandsID = "Unity.XR.Hands";
        private const string kOpenVRID = "OpenVR";

        public static readonly int kPlayerAvatarManagerExecutionOrder = 1000;

        private static readonly VersionRange kXRHandsVersionRange = new("^1.1.0");
        private static readonly VersionRange kOpenVRVersionRange = new("^2.0.0");

        private static readonly MethodInfo kCreateLoggerMethod = typeof(ILoggerFactory).GetMethod(nameof(ILoggerFactory.CreateLogger), BindingFlags.Public | BindingFlags.Instance);
        private static readonly Assembly kAssembly = Assembly.GetExecutingAssembly();

        private readonly Logger _ipaLogger;
        private readonly ILogger<CustomAvatarsInstaller> _logger;
        private readonly PluginMetadata _pluginMetadata;

        public CustomAvatarsInstaller(Logger ipaLogger, PluginMetadata pluginMetadata)
        {
            _ipaLogger = ipaLogger;
            _logger = new IPALogger<CustomAvatarsInstaller>(ipaLogger);
            _pluginMetadata = pluginMetadata;
        }

        public override void InstallBindings()
        {
            // logging
            Container.Bind(typeof(ILoggerFactory)).To<IPALoggerFactory>().AsTransient().WithArguments(_ipaLogger);
            Container.Bind(typeof(ILogger<>)).FromMethodUntyped(CreateLogger).AsTransient();

            // settings
            Container.Bind(typeof(SettingsLoader), typeof(IDisposable)).To<SettingsLoader>().AsSingle();
            SettingsLoader settingsManager = Container.Resolve<SettingsLoader>();
            settingsManager.Load();
            Container.Bind<Settings>().FromInstance(settingsManager.settings).AsSingle();
            Container.Bind(typeof(CalibrationData), typeof(IInitializable), typeof(IDisposable)).To<CalibrationData>().AsSingle();

            Container.Bind<PluginMetadata>().FromInstance(_pluginMetadata).When(InjectedIntoThisAssembly);

            _logger.LogInformation($"Current Unity XR device: '{XRSettings.loadedDeviceName}'");

            if (XRSettings.loadedDeviceName.IndexOf("OpenXR", StringComparison.OrdinalIgnoreCase) >= 0 || XRSettings.loadedDeviceName.IndexOf("OpenVR", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Container.Bind(typeof(IDeviceProvider), typeof(IInitializable), typeof(IDisposable)).To<UnityXRDeviceProvider>().AsSingle();

                if (IsPluginLoadedAndMatchesVersion(kXRHandsID, kXRHandsVersionRange))
                {
                    Container.Bind(typeof(IFingerTrackingProvider), typeof(ITickable)).To<UnityXRFingerTrackingProvider>().AsSingle();
                }
                else
                {
                    Container.Bind(typeof(IFingerTrackingProvider)).To<DevicelessFingerTrackingProvider>().AsSingle();
                }

                // SteamVR doesn't yet support render models through OpenXR so we need this workaround
                if (IsPluginLoadedAndMatchesVersion(kOpenVRID, kOpenVRVersionRange))
                {
                    BindOpenVR();
                }
            }
            else
            {
                Container.Bind(typeof(IDeviceProvider), typeof(IInitializable), typeof(IDisposable)).To<GenericDeviceProvider>().AsSingle();
                Container.Bind(typeof(IFingerTrackingProvider)).To<DevicelessFingerTrackingProvider>().AsSingle();
            }

            // managers
            Container.Bind<PlayerAvatarManager>().FromNewComponentOnNewGameObject().AsSingle();
            Container.Bind(typeof(AssetLoader), typeof(IInitializable), typeof(IDisposable)).To<AssetLoader>().AsSingle().NonLazy();

            Container.Bind<AvatarLoader>().AsSingle();
            Container.Bind<AvatarSpawner>().AsSingle();
            Container.Bind<ActiveCameraManager>().AsSingle();
            Container.Bind<VRControllerVisualsManager>().AsSingle();
            Container.Bind(typeof(VRPlayerInput), typeof(IAvatarInput), typeof(IInitializable), typeof(IDisposable)).To<VRPlayerInput>().AsSingle();
            Container.Bind(typeof(IInitializable), typeof(IDisposable)).To<QualitySettingsController>().AsSingle();
            Container.Bind(typeof(BeatSaberUtilities), typeof(IInitializable), typeof(IDisposable)).To<BeatSaberUtilities>().AsSingle();

            // helper classes
            Container.Bind<MirrorHelper>().AsTransient();

            Container.Bind(typeof(IAffinity)).To<Patches.MirrorRendererSO>().AsSingle();

            Container.Bind<TrackingRig>().FromNewComponentOnNewGameObject().AsSingle();
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

        private void BindOpenVR()
        {
            if (OpenVRHelper.Initialize())
            {
                Container.Bind(typeof(OpenVRRenderModelLoader), typeof(IDisposable)).To<OpenVRRenderModelLoader>().AsSingle();
                Container.Bind(typeof(IRenderModelProvider)).To<OpenVRRenderModelProvider>().AsSingle();
            }
        }
    }
}
