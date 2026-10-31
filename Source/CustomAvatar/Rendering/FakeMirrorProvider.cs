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

using System.Collections.Generic;
using CustomAvatar.Avatar;
using CustomAvatar.Player;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using Zenject;

namespace CustomAvatar.Rendering
{
    internal class FakeMirrorProvider : MonoBehaviour
    {
        private PlayerAvatarManager _playerAvatarManager;

        private FakeMirror _fakeMirror;

        [Inject]
        protected void Construct(PlayerAvatarManager playerAvatarManager)
        {
            _playerAvatarManager = playerAvatarManager;
        }

        protected void Awake()
        {
            // mirrored at the line where the player platform usually ends (i.e. 0.75 m in front of the player)
            Transform transform = this.transform;
            transform.SetPositionAndRotation(new Vector3(0, 0, 0.75f), Quaternion.identity);
            transform.localScale = new Vector3(1, 1, -1f); // mirrored across XY plane

            GameObject gameObject = new("MirroredAvatar");
            gameObject.SetActive(false);
            gameObject.transform.SetParent(transform, false);
            _fakeMirror = gameObject.AddComponent<FakeMirror>();
        }

        protected void OnEnable()
        {
            _playerAvatarManager.avatarLoading += OnAvatarLoading;
            _playerAvatarManager.avatarChanged += OnAvatarChanged;

            DestroyAvatar();
            CreateAvatar(_playerAvatarManager.currentlySpawnedAvatar);
        }

        protected void OnDisable()
        {
            _playerAvatarManager.avatarLoading -= OnAvatarLoading;
            _playerAvatarManager.avatarChanged -= OnAvatarChanged;
        }

        private void OnAvatarLoading(string fullPath, string name)
        {
            DestroyAvatar();
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            CreateAvatar(avatar);
        }

        private void CreateAvatar(SpawnedAvatar avatar)
        {
            if (avatar == null)
            {
                return;
            }

            _fakeMirror.Set(avatar.transform);
        }

        private void DestroyAvatar()
        {
            _fakeMirror.Clear();
        }

        // TODO: blend shapes and possibly other things
        private class FakeMirror : MonoBehaviour
        {
            private static readonly ProfilerMarker kOnBeforeRenderProfilerMarker = new($"{nameof(FakeMirror)}.{nameof(OnBeforeRender)}");

            private readonly List<Transform> _from = new(64);
            private readonly List<Transform> _to = new(64);

            public void Set(Transform from)
            {
                gameObject.SetActive(false);

                GameObject to = Instantiate(from.gameObject, Vector3.zero, Quaternion.identity, new InstantiateParameters() { worldSpace = false, parent = transform });

                List<MonoBehaviour> monoBehaviours = [];
                to.GetComponentsInChildren(true, monoBehaviours);
                monoBehaviours.Reverse(); // so Unity doesn't complain about required components being destroyed first

                foreach (MonoBehaviour monoBehaviour in monoBehaviours)
                {
                    DestroyImmediate(monoBehaviour);
                }

                from.GetComponentsInChildren(true, _from);
                to.GetComponentsInChildren(true, _to);

                for (int i = 0; i < _to.Count; i++)
                {
                    _to[i].gameObject.layer = AvatarLayers.kMirror;
                }

                gameObject.SetActive(true);
            }

            public void Clear()
            {
                if (_to.Count > 0)
                {
                    Destroy(_to[0].gameObject);
                }

                _from.Clear();
                _to.Clear();

                gameObject.SetActive(false);
            }

            protected void OnEnable()
            {
                Application.onBeforeRender += OnBeforeRender;
            }

            protected void OnDisable()
            {
                Application.onBeforeRender -= OnBeforeRender;
            }

            private void OnBeforeRender()
            {
                kOnBeforeRenderProfilerMarker.Begin(this);

                try
                {
                    transform.GetPositionAndRotation(out Vector3 containerPosition, out Quaternion containerRotation);
                    Quaternion inverseContainerRotation = Quaternion.Inverse(containerRotation);

                    Transform fromRoot = _from[0];
                    Transform toRoot = _to[0];

                    fromRoot.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
                    toRoot.SetLocalPositionAndRotation(inverseContainerRotation * (position - containerPosition), inverseContainerRotation * rotation);
                    toRoot.localScale = fromRoot.lossyScale;

                    for (int i = 1; i < _from.Count; i++)
                    {
                        Transform from = _from[i];
                        Transform to = _to[i];

                        from.GetLocalPositionAndRotation(out position, out rotation);
                        to.SetLocalPositionAndRotation(position, rotation);
                        to.localScale = from.localScale;
                    }
                }
                finally
                {
                    kOnBeforeRenderProfilerMarker.End();
                }
            }
        }
    }
}
