using CustomAvatar.StereoRendering;
using System.Collections;
using UnityEngine;

namespace CustomAvatar
{
	class MirrorController : MonoBehaviour
	{
		public static MirrorController Instance;

		private static readonly Vector3 MIRROR_POSITION = new Vector3(0, 0, 1.5f); // origin is bottom center
		private static readonly Quaternion MIRROR_ROTATION = Quaternion.Euler(-90f, 0, 0);
		private static readonly Vector3 MIRROR_SCALE = new Vector3(0.50f, 1f, 0.25f);

		private Shader stereoRenderShader;

		public static void OnLoad()
		{
			if (Instance) return;
			Instance = new GameObject("Mirror").AddComponent<MirrorController>();
		}

		void Awake()
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

		IEnumerator SpawnMirror()
		{
			yield return new WaitUntil(() => Camera.main);

			GameObject mirrorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
			mirrorPlane.name = "Stereo Mirror";
			mirrorPlane.transform.SetParent(transform, false);
			mirrorPlane.transform.localScale = MIRROR_SCALE;
			mirrorPlane.transform.position = MIRROR_POSITION + new Vector3(0, MIRROR_SCALE.z * 5, 0); // plane is 10 units in size at scale 1
			mirrorPlane.transform.rotation = MIRROR_ROTATION;

			Renderer renderer = mirrorPlane.GetComponent<Renderer>();
			renderer.sharedMaterial = new Material(stereoRenderShader);

			if (true)
			{
				GameObject stereoCameraHead = new GameObject("Stereo Camera Head [Stereo Mirror]");
				stereoCameraHead.transform.SetParent(transform, false);
				stereoCameraHead.transform.localScale = new Vector3(1 / MIRROR_SCALE.x, 1 / MIRROR_SCALE.y, 1 / MIRROR_SCALE.z);

				GameObject stereoCameraEyeObject = new GameObject("Stereo Camera Eye [Stereo Mirror]");
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
}
