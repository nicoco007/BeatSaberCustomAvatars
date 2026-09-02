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
using System.Collections.Generic;
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Player;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    internal class FakeMirrorProvider : MonoBehaviour
    {
        private static readonly Dictionary<Type, Type[]> kRequireComponentCache = [];

        private PlayerAvatarManager _playerAvatarManager;

        private GameObject _mirroredAvatar;
        private FakeMirror _fakeMirror;

        [Inject]
        protected void Construct(PlayerAvatarManager playerAvatarManager)
        {
            _playerAvatarManager = playerAvatarManager;
        }

        protected void Awake()
        {
            // mirrored at the line where the player platform usually ends (i.e. 0.75 m in front of the player)
            Transform mirroredAvatarContainerTransform = transform;
            mirroredAvatarContainerTransform.SetPositionAndRotation(new Vector3(0, 0, 0.75f), Quaternion.identity);
            mirroredAvatarContainerTransform.localScale = new Vector3(1, 1, -1f); // mirrored across XY plane

            _fakeMirror = gameObject.AddComponent<FakeMirror>();
            _fakeMirror.enabled = false;
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

            _mirroredAvatar = new GameObject("MirroredAvatar");
            _mirroredAvatar.SetActive(false);
            Transform mirroredAvatarTransform = _mirroredAvatar.transform;
            mirroredAvatarTransform.SetParent(transform);
            GameObject mirroredAvatar = Instantiate(avatar.gameObject, mirroredAvatarTransform);

            foreach (GameObject gameObject in mirroredAvatar.GetComponentsInChildren<Transform>().Select(t => t.gameObject))
            {
                SafeDestroyImmediate(gameObject);
            }

            _fakeMirror.fromRoot = avatar.transform.parent;
            _fakeMirror.toRoot = mirroredAvatarTransform;
            _fakeMirror.from = [.. Traverse(avatar.transform)];
            _fakeMirror.to = [.. Traverse(mirroredAvatar.transform)];
            _fakeMirror.enabled = true;

            foreach (Transform transform in _fakeMirror.to)
            {
                transform.gameObject.layer = AvatarLayers.kMirror;
            }

            mirroredAvatar.SetActive(true);
            _mirroredAvatar.SetActive(true);
        }

        private void DestroyAvatar()
        {
            _fakeMirror.enabled = false;
            Destroy(_mirroredAvatar);
        }

        private void SafeDestroyImmediate(GameObject gameObject)
        {
            MonoBehaviour[] monoBehaviours = gameObject.GetComponents<MonoBehaviour>();

            Array.Sort(monoBehaviours, (a, b) =>
            {
                if (DependsOn(a, b))
                {
                    return -1;
                }
                else if (DependsOn(b, a))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            });

            foreach (MonoBehaviour monoBehaviour in monoBehaviours)
            {
                DestroyImmediate(monoBehaviour);
            }
        }

        private bool DependsOn(MonoBehaviour dependent, MonoBehaviour dependency)
        {
            if (dependent == null || dependency == null)
            {
                return false;
            }

            Type dependentType = dependent.GetType();

            if (!kRequireComponentCache.TryGetValue(dependentType, out Type[] dependencies))
            {
                dependencies = CacheRequiredComponents(dependentType);
                kRequireComponentCache.Add(dependentType, dependencies);
            }

            return dependencies.Any(rc => rc.IsAssignableFrom(dependency.GetType()));
        }

        private Type[] CacheRequiredComponents(Type type)
        {
            RequireComponent[] requireComponents = (RequireComponent[])Attribute.GetCustomAttributes(type, typeof(RequireComponent), true);
            List<Type> list = new(requireComponents.Length * 3);

            foreach (RequireComponent requireComponent in requireComponents)
            {
                if (requireComponent.m_Type0 != null)
                {
                    list.Add(requireComponent.m_Type0);
                }

                if (requireComponent.m_Type1 != null)
                {
                    list.Add(requireComponent.m_Type1);
                }

                if (requireComponent.m_Type2 != null)
                {
                    list.Add(requireComponent.m_Type2);
                }
            }

            return [.. list];
        }

        private IEnumerable<Transform> Traverse(Transform transform)
        {
            yield return transform;

            for (int i = 0; i < transform.childCount; i++)
            {
                foreach (Transform child in Traverse(transform.GetChild(i)))
                {
                    yield return child;
                }
            }
        }

        // TODO: blend shapes and possibly other things
        private class FakeMirror : MonoBehaviour
        {
            public Transform fromRoot;
            public Transform toRoot;
            public Transform[] from;
            public Transform[] to;

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
                transform.GetPositionAndRotation(out Vector3 containerPosition, out Quaternion containerRotation);
                Quaternion inverseContainerRotation = Quaternion.Inverse(containerRotation);
                fromRoot.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
                toRoot.SetLocalPositionAndRotation(inverseContainerRotation * (position - containerPosition), inverseContainerRotation * rotation);
                toRoot.localScale = fromRoot.lossyScale;

                foreach ((Transform from, Transform to) in from.Zip(to))
                {
                    to.SetLocalPose(from.GetLocalPose());
                    to.localScale = from.localScale;
                }
            }
        }
    }
}
