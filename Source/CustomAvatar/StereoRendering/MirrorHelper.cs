//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
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
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.StereoRendering
{
    internal class MirrorHelper
    {
        private static readonly int kCutout = Shader.PropertyToID("_Cutout");

        private readonly ILogger<MirrorHelper> _logger;
        private readonly DiContainer _container;
        private readonly ShaderLoader _shaderLoader;
        private readonly Settings _settings;

        public MirrorHelper(ILoggerProvider loggerProvider, DiContainer container, ShaderLoader shaderLoader, Settings settings)
        {
            _logger = loggerProvider.CreateLogger<MirrorHelper>();
            _container = container;
            _shaderLoader = shaderLoader;
            _settings = settings;
        }

        public void CreateMirror(Vector3 position, Quaternion rotation, Vector2 size, Transform container, Vector3? origin = null)
        {
            if (!_shaderLoader.stereoMirrorShader)
            {
                _logger.Error("Stereo Mirror shader not loaded; mirror will not be created");
                return;
            }

            Vector3 scale = new Vector3(size.x / 10, 1, size.y / 10); // plane is 10 units in size at scale 1, width is x and height is z

            GameObject mirrorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            mirrorPlane.transform.SetParent(container);
            mirrorPlane.name = "Stereo Mirror";
            mirrorPlane.transform.localScale = scale;
            mirrorPlane.transform.localPosition = position;
            mirrorPlane.transform.localRotation = rotation;

            Material material = new Material(_shaderLoader.stereoMirrorShader);
            material.SetFloat(kCutout, 0f);
            
            Renderer renderer = mirrorPlane.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            GameObject stereoCameraHead = new GameObject($"Stereo Camera Head [{mirrorPlane.name}]");
            stereoCameraHead.transform.SetParent(mirrorPlane.transform, false);
            stereoCameraHead.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);

            GameObject stereoCameraEyeObject = new GameObject($"Stereo Camera Eye [{mirrorPlane.name}]");
            stereoCameraEyeObject.transform.SetParent(mirrorPlane.transform, false);

            Camera stereoCameraEye = stereoCameraEyeObject.AddComponent<Camera>();
            stereoCameraEye.enabled = false;
            stereoCameraEye.cullingMask = AvatarLayers.kAllLayersMask;
            stereoCameraEye.clearFlags = CameraClearFlags.SolidColor;

            // kind of hacky but setting the color to pure black or white causes the camera to
            // to give nothing to the render texture when there are no objects to render,
            // resulting in a black rectangle instead of a transparent mirror
            stereoCameraEye.backgroundColor = new Color(0, 1, 0, 1f);

            StereoRenderer stereoRenderer = _container.InstantiateComponent<StereoRenderer>(mirrorPlane);
            stereoRenderer.stereoCameraHead = stereoCameraHead;
            stereoRenderer.stereoCameraEye = stereoCameraEye;
            stereoRenderer.isMirror = true;
            stereoRenderer.useScissor = false;
            stereoRenderer.canvasOriginPos = origin ?? mirrorPlane.transform.position;
            stereoRenderer.canvasOriginRot = mirrorPlane.transform.rotation;
            stereoRenderer.renderScale = _settings.mirror.renderScale;
        }
    }
}
