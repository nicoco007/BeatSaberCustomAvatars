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

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    internal class CustomAvatarsMainCameraController : MonoBehaviour
    {
        private ILogger<CustomAvatarsMainCameraController> _logger;
        private Settings _settings;

        private Camera _camera;

        public void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        [Inject]
        public void Construct(ILogger<CustomAvatarsMainCameraController> logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public void Start()
        {
            // prevent errors if this is instantiated via Object.Instantiate
            if (_logger == null || _settings == null)
            {
                Destroy(this);
                return;
            }

            UpdateCameraMask();
        }

        private void UpdateCameraMask()
        {
            _logger.Info($"Setting avatar culling mask and near clip plane on '{_camera.name}'");

            _camera.cullingMask = (_camera.cullingMask & ~AvatarLayers.kOnlyInThirdPersonMask) | AvatarLayers.kAlwaysVisibleMask;
            _camera.nearClipPlane = _settings.cameraNearClipPlane;
        }
    }
}
