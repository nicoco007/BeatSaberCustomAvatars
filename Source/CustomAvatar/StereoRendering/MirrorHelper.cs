using UnityEngine;

namespace CustomAvatar.StereoRendering
{
    internal static class MirrorHelper
    {
        private static readonly int kCutout = Shader.PropertyToID("_Cutout");

        public static void CreateMirror(Vector3 position, Quaternion rotation, Vector2 size, Transform container)
        {
            Vector3 scale = new Vector3(size.x / 10, 1, size.y / 10); // plane is 10 units in size at scale 1, width is x and height is z

            GameObject mirrorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            mirrorPlane.transform.SetParent(container);
            mirrorPlane.name = "Stereo Mirror";
            mirrorPlane.transform.localScale = scale;
            mirrorPlane.transform.localPosition = position;
            mirrorPlane.transform.localRotation = rotation;

            Material material = new Material(ShaderLoader.stereoMirrorShader);
            material.SetFloat(kCutout, 0.01f);
            
            Renderer renderer = mirrorPlane.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            GameObject stereoCameraHead = new GameObject("Stereo Camera Head [Stereo Mirror]");
            stereoCameraHead.transform.SetParent(mirrorPlane.transform, false);
            stereoCameraHead.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);

            GameObject stereoCameraEyeObject = new GameObject("Stereo Camera Eye [Stereo Mirror]");
            stereoCameraEyeObject.transform.SetParent(mirrorPlane.transform, false);

            Camera stereoCameraEye = stereoCameraEyeObject.AddComponent<Camera>();
            stereoCameraEye.enabled = false;
            stereoCameraEye.cullingMask = (1 << AvatarLayers.AlwaysVisible) | (1 << AvatarLayers.OnlyInThirdPerson);
            stereoCameraEye.clearFlags = CameraClearFlags.SolidColor;
            stereoCameraEye.backgroundColor = new Color(0, 0, 0, 1f);

            StereoRenderer stereoRenderer = mirrorPlane.AddComponent<StereoRenderer>();
            stereoRenderer.stereoCameraHead = stereoCameraHead;
            stereoRenderer.stereoCameraEye = stereoCameraEye;
            stereoRenderer.isMirror = true;
            stereoRenderer.useScissor = false;
            stereoRenderer.canvasOriginPos = mirrorPlane.transform.position + new Vector3(-10f, 0, 0);
            stereoRenderer.canvasOriginRot = mirrorPlane.transform.rotation;
        }
    }
}
