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
using CustomAvatar.Logging;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class ActiveObjectManager<T> where T : Component
    {
        private readonly ILogger<ActiveObjectManager<T>> _logger;
        private readonly LinkedList<T> _objects = new();

        public T current => _objects.Last?.Value;

        public event Action<T> changed;

        internal ActiveObjectManager(ILogger<ActiveObjectManager<T>> logger)
        {
            _logger = logger;
        }

        public void Add(T obj)
        {
            if (current == obj)
            {
                return;
            }

            _objects.Remove(obj);
            _objects.AddLast(obj);

            InvokeChanged();
        }

        public void Remove(T obj)
        {
            bool notify = false;

            if (current == obj)
            {
                notify = true;
            }

            _objects.Remove(obj);

            if (notify)
            {
                InvokeChanged();
            }
        }

        private void InvokeChanged()
        {
            T obj = current;

            if (obj != null)
            {
                _logger.LogInformation($"Changed to '{GetTransformPath(obj)}'");
            }
            else
            {
                _logger.LogInformation($"Changed to none");
            }

            changed?.Invoke(obj);
        }

        private string GetTransformPath(Component component)
        {
            var parts = new List<string>();

            Transform transform = component.transform;

            while (transform != null)
            {
                parts.Add(transform.name);
                transform = transform.parent;
            }

            parts.Reverse();

            return string.Join("/", parts);
        }
    }
}
