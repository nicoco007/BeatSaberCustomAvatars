using System;
using System.Collections.Generic;
using CustomAvatar.Logging;
using UnityEngine;

namespace CustomAvatar.Player
{
    internal class ActivePlayerSpaceManager
    {
        private readonly ILogger<ActivePlayerSpaceManager> _logger;
        private readonly LinkedList<Transform> _playerSpaces = new LinkedList<Transform>();

        public Transform activePlayerSpace => _playerSpaces.Last?.Value;

        public event Action<Transform> activePlayerSpaceChanged;

        internal ActivePlayerSpaceManager(ILogger<ActivePlayerSpaceManager> logger)
        {
            _logger = logger;
        }

        public void Add(Transform playerSpace)
        {
            if (activePlayerSpace == playerSpace)
                return;

            _playerSpaces.Remove(playerSpace);
            _playerSpaces.AddLast(playerSpace);

            InvokeActivePlayerSpaceChanged();
        }

        public void Remove(Transform playerSpace)
        {
            bool notify = false;

            if (activePlayerSpace == playerSpace)
                notify = true;

            _playerSpaces.Remove(playerSpace);

            if (notify)
                InvokeActivePlayerSpaceChanged();
        }

        private void InvokeActivePlayerSpaceChanged()
        {
            Transform playerSpace = activePlayerSpace;

            _logger.LogInformation($"Active player space changed: '{GetTransformPath(playerSpace)}'");

            activePlayerSpaceChanged?.Invoke(playerSpace);
        }

        private string GetTransformPath(Transform transform)
        {
            var parts = new List<string>();

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
