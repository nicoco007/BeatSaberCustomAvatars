using CustomAvatar.StereoRendering;
using System.Collections.Generic;
using UnityEngine;
using HMUI;

namespace CustomAvatar.UI
{
    internal class MirrorViewController : ViewController
    {
        private static readonly Vector3 kMirrorPosition = new Vector3(0, 0, 1.5f); // origin is bottom center
        private static readonly Quaternion kMirrorRotation = Quaternion.Euler(-90f, 0, 0);
        private static readonly Vector3 kMirrorScale = new Vector3(0.50f, 1f, 0.25f);

        private GameObject _mirrorPlane;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            base.DidActivate(firstActivation, activationType);

            if (firstActivation)
            {
                StartCoroutine(SpawnMirror());
            }
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);

            Destroy(_mirrorPlane);
        }

        IEnumerator<AsyncOperation> SpawnMirror()
        {
            AssetBundleCreateRequest shadersBundleCreateRequest = AssetBundle.LoadFromFileAsync("CustomAvatars/Shaders/customavatars.assetbundle");
            yield return shadersBundleCreateRequest;

            if (!shadersBundleCreateRequest.isDone || shadersBundleCreateRequest.assetBundle == null)
            {
                Plugin.logger.Error("Failed to load stereo mirror shader");
                yield break;
            }

            AssetBundleRequest assetBundleRequest = shadersBundleCreateRequest.assetBundle.LoadAssetAsync<Shader>("Assets/Shaders/StereoRenderShader-Unlit.shader");
            yield return assetBundleRequest;
            shadersBundleCreateRequest.assetBundle.Unload(false);

            if (!assetBundleRequest.isDone || assetBundleRequest.asset == null)
            {
                Plugin.logger.Error("Failed to load stereo mirror shader");
                yield break;
            }

            Shader stereoRenderShader = assetBundleRequest.asset as Shader;

            _mirrorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _mirrorPlane.name = "Stereo Mirror";
            _mirrorPlane.transform.localScale = kMirrorScale;
            _mirrorPlane.transform.position = kMirrorPosition + new Vector3(0, kMirrorScale.z * 5, 0); // plane is 10 units in size at scale 1
            _mirrorPlane.transform.rotation = kMirrorRotation;

            Material material = new Material(stereoRenderShader);
            material.SetFloat("_Cutout", 0.01f);
            
            Renderer renderer = _mirrorPlane.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            GameObject stereoCameraHead = new GameObject("Stereo Camera Head [Stereo Mirror]");
            stereoCameraHead.transform.SetParent(_mirrorPlane.transform, false);
            stereoCameraHead.transform.localScale = new Vector3(1 / kMirrorScale.x, 1 / kMirrorScale.y, 1 / kMirrorScale.z);

            GameObject stereoCameraEyeObject = new GameObject("Stereo Camera Eye [Stereo Mirror]");
            stereoCameraEyeObject.transform.SetParent(_mirrorPlane.transform, false);

            Camera stereoCameraEye = stereoCameraEyeObject.AddComponent<Camera>();
            stereoCameraEye.enabled = false;
            stereoCameraEye.cullingMask = (1 << AvatarLayers.AlwaysVisible) | (1 << AvatarLayers.OnlyInThirdPerson);
            stereoCameraEye.clearFlags = CameraClearFlags.SolidColor;
            stereoCameraEye.backgroundColor = new Color(0, 0, 0, 1f);

            StereoRenderer stereoRenderer = _mirrorPlane.AddComponent<StereoRenderer>();
            stereoRenderer.stereoCameraHead = stereoCameraHead;
            stereoRenderer.stereoCameraEye = stereoCameraEye;
            stereoRenderer.isMirror = true;
            stereoRenderer.useScissor = false;
            stereoRenderer.canvasOriginPos = _mirrorPlane.transform.position + new Vector3(-10f, 0, 0);
            stereoRenderer.canvasOriginRot = _mirrorPlane.transform.rotation;
        }
    }
}