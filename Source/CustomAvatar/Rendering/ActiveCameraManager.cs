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

namespace CustomAvatar.Rendering
{
    internal class ActiveCameraManager
    {
        private readonly ILogger<ActiveCameraManager> _logger;
        private readonly LinkedList<MainCamera> _objects = new();

        public MainCamera current => _objects.Last?.Value;

        public event Action<MainCamera> changed;

        internal ActiveCameraManager(ILogger<ActiveCameraManager> logger)
        {
            _logger = logger;
        }

        public void Add(MainCamera camera)
        {
            if (current == camera)
            {
                return;
            }

            _objects.Remove(camera);
            _objects.AddLast(camera);

            InvokeChanged();
        }

        public void Remove(MainCamera camera)
        {
            bool notify = current == camera;

            _objects.Remove(camera);

            if (notify || _objects.Count == 0)
            {
                InvokeChanged();
            }
        }

        private void InvokeChanged()
        {
            MainCamera camera = current;

            if (camera != null)
            {
                _logger.LogInformation($"Changed to {camera}");
            }
            else
            {
                _logger.LogInformation("Changed to none");
            }

            changed?.Invoke(camera);
        }
    }
}
