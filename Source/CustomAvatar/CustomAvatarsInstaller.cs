using System.Linq;
using CustomAvatar.Avatar;
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
            Container.Bind<ILoggerFactory>().FromMethod((context) => context.Container.Instantiate<IPALoggerFactory>(new object[] { _logger })).AsTransient();

            // settings
            Container.Bind<SettingsManager>().AsSingle();
            Container.Bind<Settings>().FromMethod((context) => context.Container.Resolve<SettingsManager>().Load()).AsSingle();
            
            // managers & helper classes
            Container.BindInterfacesAndSelfTo<PlayerAvatarManager>().AsSingle();
            Container.Bind<StereoRenderManager>().AsSingle();
            Container.Bind<AvatarTailor>().AsTransient();
            Container.Bind<MirrorHelper>().AsTransient();
            Container.Bind<AvatarLoader>().AsTransient();
            Container.Bind<AvatarSpawner>().AsTransient();

            // behaviours
            Container.Bind<TrackedDeviceManager>().FromNewComponentOnNewPrefab(new GameObject(nameof(TrackedDeviceManager))).AsSingle();
            Container.Bind<ShaderLoader>().FromNewComponentOnNewPrefab(new GameObject(nameof(ShaderLoader))).AsSingle().NonLazy();

            // not sure if this is a great idea but w/e
            if (!Container.HasBinding<MainSettingsModelSO>())
            {
                Container.Bind<MainSettingsModelSO>().FromInstance(Resources.FindObjectsOfTypeAll<MainSettingsModelSO>().First());
            }
        }
    }
}
