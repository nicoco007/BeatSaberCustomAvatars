using System;
using System.Collections;
using UnityEngine;
using Logger = CustomAvatar.Util.Logger;

namespace CustomAvatar
{
	class MirrorController : MonoBehaviour
	{
		public static MirrorController Instance;

		GameObject mirrorCamObj;
		Camera _cam;
		RenderTexture _camRenderTexture;
		GameObject _quad;
		Material _mirrorMaterial;
		Shader CutoutShader;

		public static void OnLoad()
		{
			if (Instance) return;
			Instance = new GameObject("Mirror").AddComponent<MirrorController>();
		}

		void Awake()
		{
			var myLoadedAssetBundle = AssetBundle.LoadFromFile("CustomAvatars/Shaders/customavatars.assetbundle");
			CutoutShader = myLoadedAssetBundle.LoadAsset<Shader>("Assets/Shaders/Custom/sh_custom_unlit_transparent_Luminance.shader");
			StartCoroutine(SpawnMirror());
			myLoadedAssetBundle.Unload(false);
		}

		void OnDestroy()
		{
			Destroy(mirrorCamObj);
		}

		IEnumerator SpawnMirror()
		{
			yield return new WaitUntil(() => Camera.main);

			mirrorCamObj = Instantiate(Camera.main.gameObject);

			mirrorCamObj.SetActive (false);
			mirrorCamObj.name = "mirrorCamObj";
			mirrorCamObj.tag = "Untagged";
			mirrorCamObj.transform.parent = null;

			while (mirrorCamObj.transform.childCount > 0) DestroyImmediate(mirrorCamObj.transform.GetChild(0).gameObject);
			DestroyImmediate(mirrorCamObj.GetComponent("CameraRenderCallbacksManager"));
			DestroyImmediate(mirrorCamObj.GetComponent("AudioListener"));
			DestroyImmediate(mirrorCamObj.GetComponent("MeshCollider"));

			_cam = mirrorCamObj.GetComponent<Camera>();
			_cam.stereoTargetEye = StereoTargetEyeMask.None;
			_cam.enabled = true;
			_cam.orthographic = true;
			_cam.aspect = 1.4f;
			_cam.orthographicSize = 1.25f;
			_cam.clearFlags = CameraClearFlags.SolidColor;
			_cam.backgroundColor = new Color(0, 0, 0, 5/255f);
			_cam.farClipPlane = 10;

			int layer1 = 3;
			int layer2 = 4;
			int layer3 = 0;
			int layerMask1 = 1 << layer1;
			int layerMask2 = 1 << layer2;
			int layerMask3 = 1 << layer3;
			int finalMask = layerMask1 | layerMask2 | layerMask3;


			_cam.cullingMask = finalMask;


			var _liv = _cam.GetComponent<LIV.SDK.Unity.LIV>();
			if (_liv)
				Destroy(_liv);

			mirrorCamObj.SetActive(true);

			mirrorCamObj.transform.position = new Vector3(0, 1.25f, 1.45f);
			mirrorCamObj.transform.rotation = Quaternion.Euler(0, 180, 0);


			_camRenderTexture = new RenderTexture(1, 1, 24);
			_camRenderTexture.width = _cam.pixelWidth;
			_camRenderTexture.height = _cam.pixelHeight;
			_camRenderTexture.useDynamicScale = false;
			_camRenderTexture.Create();

			_cam.targetTexture = _camRenderTexture;



			_mirrorMaterial = new Material(CutoutShader);
			_mirrorMaterial.SetTexture("_Tex", _camRenderTexture);
			_mirrorMaterial.SetFloat("_Cutout", .01f);

			_quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			DontDestroyOnLoad(_quad);
			DestroyImmediate(_quad.GetComponent<Collider>());
			_quad.GetComponent<MeshRenderer>().material = _mirrorMaterial;
			_quad.transform.parent = mirrorCamObj.transform;
			_quad.transform.localPosition = new Vector3(0,0,-.05f);
			_quad.transform.localEulerAngles = new Vector3(0, 0, 0);
			_quad.transform.localScale = new Vector3(2.5f*_cam.aspect,2.5f,2.5f);
			Logger.Log($"Mirror Resolution: {_cam.pixelWidth}x{_cam.pixelHeight}");
		}
	}
}
