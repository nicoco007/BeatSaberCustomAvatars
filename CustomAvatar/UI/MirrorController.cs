using CustomAvatar.StereoRendering;
using System.Collections;
using UnityEngine;

namespace CustomAvatar
{
	class MirrorController : MonoBehaviour
	{
		public static MirrorController Instance;

		private static readonly Vector3 MIRROR_POSITION = new Vector3(0, 0.4f, 2.5f); // origin is bottom center
		private static readonly Quaternion MIRROR_ROTATION = Quaternion.Euler(-90f, 0, 0);
		private static readonly Vector3 MIRROR_SCALE = new Vector3(0.30f, 1f, 0.20f);

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

			StartCoroutine(SpawnMirror());
			shadersBundle.Unload(false);
		}

		IEnumerator SpawnMirror()
		{
			yield return new WaitUntil(() => Camera.main);

			GameObject mirrorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
			mirrorPlane.name = "Stereo Mirror";
			mirrorPlane.transform.parent = transform;
			mirrorPlane.transform.localScale = MIRROR_SCALE;
			mirrorPlane.transform.position = MIRROR_POSITION + new Vector3(0, MIRROR_SCALE.z * 5, 0); // plane is 10 units in size at scale 1
			mirrorPlane.transform.rotation = MIRROR_ROTATION;

			Renderer renderer = mirrorPlane.GetComponent<Renderer>();
			renderer.sharedMaterial = new Material(stereoRenderShader);

			GameObject stereoCameraHead = new GameObject("Stereo Camera Head [Stereo Mirror]");
			stereoCameraHead.transform.parent = transform;

			GameObject stereoCameraEyeObject = CopyCamera(stereoCameraHead.transform);
			Camera stereoCameraEye = stereoCameraEyeObject.GetComponent<Camera>();
			stereoCameraEye.enabled = false;

			StereoRenderer stereoRenderer = mirrorPlane.AddComponent<StereoRenderer>();
			stereoRenderer.stereoCameraHead = stereoCameraHead;
			stereoRenderer.stereoCameraEye = stereoCameraEye;
			stereoRenderer.isMirror = true;
			stereoRenderer.useScissor = false;
			stereoRenderer.canvasOrigin = mirrorPlane.transform;
		}

		private GameObject CopyCamera(Transform parent)
		{
			GameObject cameraObject = Instantiate(Camera.main.gameObject, parent);

			cameraObject.name = "Stereo Camera Eye [Stereo Mirror]";
			cameraObject.tag = "Untagged";

			while (cameraObject.transform.childCount > 0) DestroyImmediate(cameraObject.transform.GetChild(0).gameObject);
			DestroyImmediate(cameraObject.GetComponent("CameraRenderCallbacksManager"));
			DestroyImmediate(cameraObject.GetComponent("AudioListener"));
			DestroyImmediate(cameraObject.GetComponent("MeshCollider"));

			Camera camera = cameraObject.GetComponent<Camera>();

			var _liv = camera.GetComponent<LIV.SDK.Unity.LIV>();
			if (_liv)
				Destroy(_liv);

			return cameraObject;
		}
	}
}
