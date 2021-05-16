using CustomAvatar.Configuration;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class GameEnvironmentObjectManager : IInitializable
    {
        private readonly DiContainer _container;
        private readonly Settings _settings;

        internal GameEnvironmentObjectManager(DiContainer container, Settings settings)
        {
            _container = container;
            _settings = settings;
        }

        public void Initialize()
        {
            switch (_settings.floorHeightAdjust.value)
            {
                case FloorHeightAdjust.EntireEnvironment:
                    _container.InstantiateComponent<EnvironmentObject>(GameObject.Find("/Environment"));
                    break;

                case FloorHeightAdjust.PlayersPlaceOnly:
                    var environment = GameObject.Find("/Environment");

                    _container.InstantiateComponent<EnvironmentObject>(environment.transform.Find("PlayersPlace").gameObject);

                    Transform shadow = environment.transform.Find("PlayersPlaceShadow");
                    if (shadow) _container.InstantiateComponent<EnvironmentObject>(shadow.gameObject);

                    break;
            }
        }
    }
}
