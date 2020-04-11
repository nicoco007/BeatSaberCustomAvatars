using System.Linq;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar
{
    internal class CustomAvatarsInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<AvatarManager>().AsSingle();
            Container.Bind<AvatarTailor>().AsTransient();
            Container.Bind<TrackedDeviceManager>().FromNewComponentOnNewPrefab(new GameObject(nameof(TrackedDeviceManager))).AsSingle();

            // not sure if this is a great idea but w/e
            if (!Container.HasBinding<MainSettingsModelSO>())
            {
                Container.Bind<MainSettingsModelSO>().FromInstance(Resources.FindObjectsOfTypeAll<MainSettingsModelSO>().First());
            }
        }
    }
}
