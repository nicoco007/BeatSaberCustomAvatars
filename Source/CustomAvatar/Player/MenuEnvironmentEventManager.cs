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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class MenuEnvironmentEventManager : IInitializable, IDisposable
    {
        private readonly PlayerAvatarManager _playerAvatarManager;
        private readonly GameScenesManager _gameScenesManager;
        private readonly MenuEnvironmentManager.MenuEnvironmentObjects[] _data;
        private readonly List<EventTrigger> _eventTriggers = new List<EventTrigger>();

        internal MenuEnvironmentEventManager(PlayerAvatarManager playerAvatarManager, GameScenesManager gameScenesManager, MenuEnvironmentManager menuEnvironmentManager)
        {
            _playerAvatarManager = playerAvatarManager;
            _gameScenesManager = gameScenesManager;
            _data = menuEnvironmentManager.GetField<MenuEnvironmentManager.MenuEnvironmentObjects[], MenuEnvironmentManager>("_data");
        }

        public void Initialize()
        {
            foreach (MenuEnvironmentManager.MenuEnvironmentObjects environmentObjects in _data.GroupBy((item) => item.menuEnvironmentType).Select((items) => items.First()))
            {
                EventTrigger eventTrigger = environmentObjects.wrapper.AddComponent<EventTrigger>();

                switch (environmentObjects.menuEnvironmentType)
                {
                    case MenuEnvironmentManager.MenuEnvironmentType.Default:
                        eventTrigger.Init(_playerAvatarManager, _gameScenesManager, (eventManager) => eventManager.menuEntered.Invoke());
                        break;

                    case MenuEnvironmentManager.MenuEnvironmentType.Lobby:
                        eventTrigger.Init(_playerAvatarManager, _gameScenesManager, (eventManager) => eventManager.multiplayerLobbyEntered.Invoke());
                        break;
                }
            }
        }

        public void Dispose()
        {
            foreach (EventTrigger eventTrigger in _eventTriggers)
            {
                UnityEngine.Object.Destroy(eventTrigger);
            }
        }

        private class EventTrigger : MonoBehaviour
        {
            private PlayerAvatarManager _playerAvatarManager;
            private GameScenesManager _gameScenesManager;
            private Action<EventManager> _callback;

            public void Init(PlayerAvatarManager playerAvatarManager, GameScenesManager gameScenesManager, Action<EventManager> callback)
            {
                _playerAvatarManager = playerAvatarManager;
                _gameScenesManager = gameScenesManager;
                _callback = callback;
            }

            private void OnEnable()
            {
                StartCoroutine(TriggerEventCoroutine());
            }

            private void Start()
            {
                StartCoroutine(TriggerEventCoroutine());
            }

            private IEnumerator TriggerEventCoroutine()
            {
                if (_gameScenesManager == null)
                {
                    yield break;
                }

                yield return _gameScenesManager.waitUntilSceneTransitionFinish;

                if (_playerAvatarManager?.currentlySpawnedAvatar == null)
                {
                    yield break;
                }

                if (!_playerAvatarManager.currentlySpawnedAvatar.TryGetComponent(out EventManager eventManager))
                {
                    yield break;
                }

                _callback?.Invoke(eventManager);
            }
        }
    }
}
