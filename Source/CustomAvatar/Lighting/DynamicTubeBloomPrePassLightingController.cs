//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Lighting
{
    internal class DynamicTubeBloomPrePassLightingController : MonoBehaviour
    {
        private ILogger<DynamicTubeBloomPrePassLightingController> _logger;
        private LightWithIdManager _lightManager;
        private DiContainer _container;

        private List<DynamicTubeBloomPrePassLight>[] _lights;
        
        #region Behaviour Lifecycle

        [Inject]
        private void Inject(ILoggerProvider loggerProvider, LightWithIdManager lightManager, DiContainer container)
        {
            name = nameof(DynamicTubeBloomPrePassLightingController);

            _logger = loggerProvider.CreateLogger<DynamicTubeBloomPrePassLightingController>();
            _lightManager = lightManager;
            _container = container;
        }

        private void Start()
        {
            _lightManager.didSetColorForIdEvent += OnSetColorForId;

            CreateLights();
        }

        private void OnDestroy()
        {
            _lightManager.didSetColorForIdEvent -= OnSetColorForId;
        }

        #endregion

        private void CreateLights()
        {
            List<ILightWithId>[] lightsWithId = _lightManager.GetPrivateField<List<ILightWithId>[]>("_lights");
            int maxLightId = _lightManager.GetPrivateField<int>("kMaxLightId");

            _lights = new List<DynamicTubeBloomPrePassLight>[maxLightId + 1];
            
            for (int id = 0; id < lightsWithId.Length; id++)
            {
                if (lightsWithId[id] == null) continue;

                foreach (ILightWithId lightWithId in lightsWithId[id])
                {
                    if (lightWithId is TubeBloomPrePassLightWithId tubeLightWithId)
                    {
                        TubeBloomPrePassLight tubeLight = tubeLightWithId.GetPrivateField<TubeBloomPrePassLight>("_tubeBloomPrePassLight");

                        DynamicTubeBloomPrePassLight light = _container.InstantiateComponent<DynamicTubeBloomPrePassLight>(new GameObject($"DynamicTubeBloomPrePassLight({tubeLight.name})"), new[] { tubeLight });

                        if (_lights[id] == null)
                        {
                            _lights[id] = new List<DynamicTubeBloomPrePassLight>(10);
                        }

                        _lights[id].Add(light);

                        light.transform.parent = transform;
                        light.transform.position = Vector3.zero;
                        light.transform.rotation = Quaternion.identity;
                    }
                }
            }

            _logger.Trace($"Created {_lights.Sum(l => l?.Count)} DynamicTubeBloomPrePassLights");
        }

        private void OnSetColorForId(int id, Color color)
        {
            if (_lights[id] != null)
            {
                foreach (DynamicTubeBloomPrePassLight light in _lights[id])
                {
                    light.color = color;
                }
            }
        }
    }
}
