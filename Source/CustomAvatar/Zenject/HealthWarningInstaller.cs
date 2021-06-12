using CustomAvatar.Lighting;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class HealthWarningInstaller : Installer
    {
        private readonly ILogger<HealthWarningInstaller> _logger;

        internal HealthWarningInstaller(ILogger<HealthWarningInstaller> logger)
        {
            _logger = logger;
        }

        public override void InstallBindings()
        {
            TryAddEnvironmentObject("/BasicMenuGround");
            TryAddEnvironmentObject("/MenuFogRing");

            Container.BindInterfacesAndSelfTo<MenuLightingCreator>().AsSingle().NonLazy();
        }

        private void TryAddEnvironmentObject(string name)
        {
            var gameObject = GameObject.Find(name);

            if (gameObject)
            {
                Container.InstantiateComponent<EnvironmentObject>(gameObject);
            }
            else
            {
                _logger.Error($"GameObject '{name}' does not exist!");
            }
        }
    }
}
