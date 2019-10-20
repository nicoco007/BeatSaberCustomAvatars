using CustomAvatar.StereoRendering;
using System.Collections;
using UnityEngine;
using VRUI;

namespace CustomAvatar.UI
{
	internal class MirrorController : VRUINavigationController
	{
		private static readonly Vector3 MirrorPosition = new Vector3(0, 0, 1.5f); // origin is bottom center
		private static readonly Quaternion MirrorRotation = Quaternion.Euler(-90f, 0, 0);
		private static readonly Vector3 MirrorScale = new Vector3(0.50f, 1f, 0.25f);

		private GameObject mirrorPlane;
		private Shader stereoRenderShader;

		protected override void DidActivate(bool firstActivation, ActivationType activationType)
		{
			base.DidActivate(firstActivation, activationType);

			if (firstActivation)
			{
				var shadersBundle = AssetBundle.LoadFromFile("CustomAvatars/Shaders/customavatars.assetbundle");
				stereoRenderShader = shadersBundle.LoadAsset<Shader>("Assets/Shaders/StereoRenderShader-Unlit.shader");

				if (stereoRenderShader)
				{
					StartCoroutine(SpawnMirror());
				}
				else
				{
					Plugin.Logger.Error("Failed to load mirror shader!");
				}

				shadersBundle.Unload(false);
			}
		}

		protected override void DidDeactivate(DeactivationType deactivationType)
		{
			base.DidDeactivate(deactivationType);

			Destroy(mirrorPlane);
		}

		IEnumerator SpawnMirror()
		{
			yield return new WaitUntil(() => Camera.main);

			mirrorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
			mirrorPlane.name = "Stereo Mirror";
			mirrorPlane.transform.localScale = MirrorScale;
			mirrorPlane.transform.position = MirrorPosition + new Vector3(0, MirrorScale.z * 5, 0); // plane is 10 units in size at scale 1
			mirrorPlane.transform.rotation = MirrorRotation;

			Renderer renderer = mirrorPlane.GetComponent<Renderer>();
			renderer.sharedMaterial = new Material(stereoRenderShader);

			GameObject stereoCameraHead = new GameObject("Stereo Camera Head [Stereo Mirror]");
			stereoCameraHead.transform.SetParent(mirrorPlane.transform, false);
			stereoCameraHead.transform.localScale = new Vector3(1 / MirrorScale.x, 1 / MirrorScale.y, 1 / MirrorScale.z);

			GameObject stereoCameraEyeObject = new GameObject("Stereo Camera Eye [Stereo Mirror]");
			stereoCameraEyeObject.transform.SetParent(mirrorPlane.transform, false);
			Camera stereoCameraEye = stereoCameraEyeObject.AddComponent<Camera>();
			stereoCameraEye.cullingMask = 0;
			stereoCameraEye.cullingMask |= 1 << AvatarLayers.OnlyInThirdPerson;
			stereoCameraEye.cullingMask |= 1 << AvatarLayers.Global;
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
