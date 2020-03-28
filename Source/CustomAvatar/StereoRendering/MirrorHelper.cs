using System.Collections.Generic;
using UnityEngine;

namespace CustomAvatar.StereoRendering
{
    internal static class MirrorHelper
    {
        public static IEnumerator<AsyncOperation> SpawnMirror(Vector3 position, Quaternion rotation, Vector3 scale, Transform container)
        {
            GameObject mirrorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            mirrorPlane.transform.SetParent(container);
            mirrorPlane.name = "Stereo Mirror";
            mirrorPlane.transform.localScale = scale;
            mirrorPlane.transform.localPosition = position + new Vector3(0, scale.z * 5, 0); // plane is 10 units in size at scale 1
            mirrorPlane.transform.localRotation = rotation;

            Material material = new Material(ShaderLoader.stereoMirrorShader);
            material.SetFloat("_Cutout", 0.01f);
            
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

            yield break;
        }
    }
}
