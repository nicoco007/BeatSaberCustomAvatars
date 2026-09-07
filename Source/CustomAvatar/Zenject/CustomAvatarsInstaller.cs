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

using System;
using System.Threading.Tasks;
using BGLib.AppFlow.Initialization;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using CustomAvatar.Tracking;
using CustomAvatar.Tracking.OpenVR;
using CustomAvatar.Tracking.UnityXR;
using CustomAvatar.Utilities;
using Hive.Versioning;
using SiraUtil.Affinity;
using UnityEngine.XR;
using Valve.VR;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class CustomAvatarsInstaller : AsyncInstaller
    {
        private const string kXRHandsID = "Unity.XR.Hands";
        private const string kOpenVRID = "OpenVR";

        public static readonly int kPlayerAvatarManagerExecutionOrder = 1000;

        private static readonly VersionRange kXRHandsVersionRange = new("^1.1.0");
        private static readonly VersionRange kOpenVRVersionRange = new("^2.0.0");

        private AssetLoader _assetLoader;
        private SettingsLoader _settingsLoader;

        public override void InstallBindings()
        {
            // settings
            Container.Bind<IDisposable>().To<SettingsLoader>().FromInstance(_settingsLoader);
            Container.Bind<Settings>().FromInstance(_settingsLoader.settings);
            Container.Bind(typeof(CalibrationData), typeof(IInitializable), typeof(IDisposable)).To<CalibrationData>().AsSingle();

            if (XRSettings.loadedDeviceName.IndexOf("OpenXR", StringComparison.OrdinalIgnoreCase) >= 0 || XRSettings.loadedDeviceName.IndexOf("OpenVR", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Container.Bind(typeof(IDeviceProvider), typeof(IInitializable), typeof(IDisposable)).To<UnityXRDeviceProvider>().AsSingle();

                if (PluginUtilities.IsPluginLoadedAndMatchesVersion(kXRHandsID, kXRHandsVersionRange))
                {
                    Container.Bind(typeof(IFingerTrackingProvider), typeof(ITickable)).To<UnityXRFingerTrackingProvider>().AsSingle();
                }
                else
                {
                    Container.Bind(typeof(IFingerTrackingProvider)).To<DevicelessFingerTrackingProvider>().AsSingle();
                }

                // SteamVR doesn't yet support render models through OpenXR so we need this workaround
                if (PluginUtilities.IsPluginLoadedAndMatchesVersion(kOpenVRID, kOpenVRVersionRange))
                {
                    BindOpenVR();
                }
            }
            else
            {
                Container.Bind(typeof(IDeviceProvider), typeof(IInitializable), typeof(IDisposable)).To<GenericDeviceProvider>().AsSingle();
                Container.Bind(typeof(IFingerTrackingProvider)).To<DevicelessFingerTrackingProvider>().AsSingle();
            }

            Container.Bind<TrackingRig>().FromComponentInNewPrefab(_assetLoader.trackingRig).AsSingle();
            Container.Bind<PlayerAvatarManager>().FromComponentInNewPrefab(_assetLoader.playerAvatarManager).AsSingle();

            Container.Bind(typeof(AssetLoader), typeof(IDisposable)).FromInstance(_assetLoader);

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
        }

        protected override void LoadResourcesBeforeInstall(IInstallerRegistry registry, DiContainer container)
        {
        }

        protected override Task LoadResourcesBeforeInstallAsync(IInstallerRegistry registry, DiContainer container)
        {
            _assetLoader = container.Instantiate<AssetLoader>();
            _settingsLoader = container.Instantiate<SettingsLoader>();
            return Task.WhenAll(_assetLoader.LoadAssetsAsync(), _settingsLoader.LoadAsync());
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
