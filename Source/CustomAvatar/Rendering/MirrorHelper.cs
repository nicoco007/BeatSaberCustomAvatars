//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Rendering
{
    internal class MirrorHelper
    {
        private readonly ILogger<MirrorHelper> _logger;
        private readonly DiContainer _container;
        private readonly ShaderLoader _shaderLoader;

        public MirrorHelper(ILogger<MirrorHelper> logger, DiContainer container, ShaderLoader shaderLoader)
        {
            _logger = logger;
            _container = container;
            _shaderLoader = shaderLoader;
        }

        public StereoMirrorRenderer CreateMirror(Vector3 position, Quaternion rotation, Vector2 size, Transform container)
        {
            if (!_shaderLoader.stereoMirrorShader)
            {
                _logger.LogError("Stereo Mirror shader not loaded; mirror will not be created");
                return null;
            }

            // plane is 10 m in size at scale 1, width is x and height is z
            var scale = new Vector3(size.x / 10, 1, size.y / 10);

            var mirrorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            mirrorPlane.layer = 5;
            Object.Destroy(mirrorPlane.GetComponent<Collider>());
            mirrorPlane.transform.SetParent(container);
            mirrorPlane.name = "Stereo Mirror";
            mirrorPlane.transform.localScale = scale;
            mirrorPlane.transform.localPosition = position;
            mirrorPlane.transform.localRotation = rotation;

            return _container.InstantiateComponent<StereoMirrorRenderer>(mirrorPlane);
        }
    }
}
