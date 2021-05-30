using CustomAvatar.Player;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class HealthWarningInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.InstantiateComponent<EnvironmentObject>(GameObject.Find("/Ground"));
            Container.InstantiateComponent<EnvironmentObject>(GameObject.Find("/MenuFogRing"));
        }
    }
}
