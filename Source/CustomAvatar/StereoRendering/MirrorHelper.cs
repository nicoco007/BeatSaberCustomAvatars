using UnityEngine;
using Zenject;

namespace CustomAvatar.StereoRendering
{
    internal class MirrorHelper
    {
        private static readonly int kCutout = Shader.PropertyToID("_Cutout");

        private readonly DiContainer _container;
        private readonly ShaderLoader _shaderLoader;

        public MirrorHelper(DiContainer container, ShaderLoader shaderLoader)
        {
            _container = container;
            _shaderLoader = shaderLoader;
        }

        public void CreateMirror(Vector3 position, Quaternion rotation, Vector2 size, Transform container, Vector3? origin = null)
        {
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
            stereoCameraEye.cullingMask = (1 << AvatarLayers.kAlwaysVisible) | (1 << AvatarLayers.kOnlyInThirdPerson);
            stereoCameraEye.clearFlags = CameraClearFlags.SolidColor;
            stereoCameraEye.backgroundColor = new Color(0, 0, 0, 1f);

            StereoRenderer stereoRenderer = _container.InstantiateComponent<StereoRenderer>(mirrorPlane);
            stereoRenderer.stereoCameraHead = stereoCameraHead;
            stereoRenderer.stereoCameraEye = stereoCameraEye;
            stereoRenderer.isMirror = true;
            stereoRenderer.useScissor = false;
            stereoRenderer.canvasOriginPos = origin ?? mirrorPlane.transform.position;
            stereoRenderer.canvasOriginRot = mirrorPlane.transform.rotation;
        }
    }
}
