//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using CustomAvatar.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar.Rendering
{
    internal class FakeMirrorProvider : IMirrorProvider
    {
        private static readonly Dictionary<Type, Type[]> kRequireComponentCache = [];

        private GameObject _mirroredAvatarContainer;

        public void ShowAvatar(SpawnedAvatar avatar)
        {
            HideAvatar();

            if (avatar == null)
            {
                return;
            }

            _mirroredAvatarContainer = new GameObject("MirroredAvatarContainer");
            _mirroredAvatarContainer.SetActive(false);

            // mirrored at the line where the player platform usually ends (i.e. mirror plane is 0.75 m in front of the player)
            Transform mirroredAvatarTransform = _mirroredAvatarContainer.transform;
            mirroredAvatarTransform.SetPositionAndRotation(new Vector3(0, 0, 1.5f), Quaternion.identity);

            GameObject mirroredAvatar = Object.Instantiate(avatar.gameObject, mirroredAvatarTransform);

            foreach (GameObject gameObject in mirroredAvatar.GetComponentsInChildren<Transform>().Select(t => t.gameObject))
            {
                SafeDestroyImmediate(gameObject);
            }

            FakeMirror fakeMirror = _mirroredAvatarContainer.AddComponent<FakeMirror>();
            fakeMirror.root = avatar.transform.parent;
            fakeMirror.from = [.. Traverse(avatar.transform)];
            fakeMirror.to = [.. Traverse(mirroredAvatar.transform)];

            foreach (Transform transform in fakeMirror.to)
            {
                transform.gameObject.layer = AvatarLayers.kMirror;
            }

            _mirroredAvatarContainer.SetActive(true);
            mirroredAvatar.SetActive(true);
        }

        public void HideAvatar()
        {
            Object.Destroy(_mirroredAvatarContainer);
        }

        public void Enable()
        {
        }

        public void Disable()
        {
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
                Object.DestroyImmediate(monoBehaviour);
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
            public Transform root;
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
                Vector3 scale = root.lossyScale;
                transform.localScale = new Vector3(scale.x, scale.y, -scale.z); // mirrored across XY plane

                foreach ((Transform from, Transform to) in from.Zip(to))
                {
                    from.GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);
                    to.SetLocalPositionAndRotation(position, rotation);
                    to.localScale = from.localScale;
                }
            }
        }
    }
}
