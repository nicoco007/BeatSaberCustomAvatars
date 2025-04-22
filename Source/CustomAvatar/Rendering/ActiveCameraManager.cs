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
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Rendering
{
    internal class ActiveCameraManager
    {
        private readonly ILogger<ActiveCameraManager> _logger;
        private readonly LinkedList<Element> _objects = new();

        public Element current => _objects.Last?.Value;

        public event Action<Element> changed;

        internal ActiveCameraManager(ILogger<ActiveCameraManager> logger)
        {
            _logger = logger;
        }

        public Element Add(Camera camera, Transform playerSpace, Transform origin, bool showAvatar)
        {
            if (current?.camera == camera)
            {
                return current;
            }

            Element obj = new(camera, playerSpace, origin, showAvatar);

            _objects.Remove(obj);
            _objects.AddLast(obj);

            InvokeChanged();

            return obj;
        }

        public void Remove(Element obj)
        {
            bool notify = false;

            if (current?.camera == obj.camera)
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
            Element obj = current;

            if (obj != null)
            {
                _logger.LogInformation($"Changed to {obj}");
            }
            else
            {
                _logger.LogInformation("Changed to none");
            }

            changed?.Invoke(obj);
        }

        internal record Element(Camera camera, Transform playerSpace, Transform origin, bool showAvatar)
        {
            public override string ToString()
            {
                return $"{{ {nameof(camera)} = {UnityUtilities.GetTransformPath(camera)}, {nameof(playerSpace)} = {UnityUtilities.GetTransformPath(playerSpace)}, {nameof(origin)} = {UnityUtilities.GetTransformPath(origin)}, {nameof(showAvatar)} = {showAvatar} }}";
            }
        }
    }
}
