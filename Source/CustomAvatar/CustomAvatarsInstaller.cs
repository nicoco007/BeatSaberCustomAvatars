using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.StereoRendering;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar
{
    internal class CustomAvatarsInstaller : Installer
    {
        private Logger _logger;

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
            
            // managers
            Container.BindInterfacesAndSelfTo<PlayerAvatarManager>().AsSingle();
            Container.Bind<StereoRenderManager>().AsSingle();
            Container.Bind<AvatarLoader>().AsSingle();

            // helper classes
            Container.Bind<AvatarTailor>().AsTransient();
            Container.Bind<MirrorHelper>().AsTransient();
            Container.Bind<AvatarSpawner>().AsTransient();
            Container.Bind<GameScenesHelper>().AsTransient();

            // behaviours
            Container.Bind<TrackedDeviceManager>().FromNewComponentOnNewPrefab(new GameObject(nameof(TrackedDeviceManager))).AsSingle().NonLazy();
            Container.Bind<ShaderLoader>().FromNewComponentOnNewPrefab(new GameObject(nameof(ShaderLoader))).AsSingle().NonLazy();

            // not sure if this is a great idea but w/e
            if (!Container.HasBinding<MainSettingsModelSO>())
            {
                Container.Bind<MainSettingsModelSO>().FromInstance(Resources.FindObjectsOfTypeAll<MainSettingsModelSO>().First());
            }
        }
    }
}
