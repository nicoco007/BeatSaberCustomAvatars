using CustomAvatar.StereoRendering;
using System.Collections;
using UnityEngine;
using VRUI;

namespace CustomAvatar.UI
{
	internal class MirrorViewController : VRUINavigationController
	{
		private static readonly Vector3 MirrorPosition = new Vector3(0, 0, 1.5f); // origin is bottom center
		private static readonly Quaternion MirrorRotation = Quaternion.Euler(-90f, 0, 0);
		private static readonly Vector3 MirrorScale = new Vector3(0.50f, 1f, 0.25f);

		private GameObject mirrorPlane;

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

			Destroy(mirrorPlane);
		}

		IEnumerator SpawnMirror()
		{
			AssetBundleCreateRequest shadersBundleCreateRequest = AssetBundle.LoadFromFileAsync("CustomAvatars/Shaders/customavatars.assetbundle");
			yield return shadersBundleCreateRequest;

			if (!shadersBundleCreateRequest.isDone || shadersBundleCreateRequest.assetBundle == null)
			{
				Plugin.Logger.Error("Failed to load stereo mirror shader");
				yield break;
			}

			AssetBundleRequest assetBundleRequest = shadersBundleCreateRequest.assetBundle.LoadAssetAsync<Shader>("Assets/Shaders/StereoRenderShader-Unlit.shader");
			yield return assetBundleRequest;
			shadersBundleCreateRequest.assetBundle.Unload(false);

			if (!assetBundleRequest.isDone || assetBundleRequest.asset == null)
			{
				Plugin.Logger.Error("Failed to load stereo mirror shader");
				yield break;
			}

			Shader stereoRenderShader = assetBundleRequest.asset as Shader;

			mirrorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
			mirrorPlane.name = "Stereo Mirror";
			mirrorPlane.transform.localScale = MirrorScale;
			mirrorPlane.transform.position = MirrorPosition + new Vector3(0, MirrorScale.z * 5, 0); // plane is 10 units in size at scale 1
			mirrorPlane.transform.rotation = MirrorRotation;

			Material material = new Material(stereoRenderShader);
			material.SetFloat("_Cutout", 0.01f);
			
			Renderer renderer = mirrorPlane.GetComponent<Renderer>();
			renderer.sharedMaterial = material;

			GameObject stereoCameraHead = new GameObject("Stereo Camera Head [Stereo Mirror]");
			stereoCameraHead.transform.SetParent(mirrorPlane.transform, false);
			stereoCameraHead.transform.localScale = new Vector3(1 / MirrorScale.x, 1 / MirrorScale.y, 1 / MirrorScale.z);

			GameObject stereoCameraEyeObject = new GameObject("Stereo Camera Eye [Stereo Mirror]");
			stereoCameraEyeObject.transform.SetParent(mirrorPlane.transform, false);

			Camera stereoCameraEye = stereoCameraEyeObject.AddComponent<Camera>();
			stereoCameraEye.enabled = false;
			stereoCameraEye.cullingMask = (1 << AvatarLayers.OnlyInThirdPerson) | (1 << AvatarLayers.Global);
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
