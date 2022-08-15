using System;
using System.Collections.Generic;
using CustomAvatar.Logging;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class ActiveObjectManager<T> where T : Component
    {
        private readonly ILogger<ActiveObjectManager<T>> _logger;
        private readonly LinkedList<T> _objects = new LinkedList<T>();

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
