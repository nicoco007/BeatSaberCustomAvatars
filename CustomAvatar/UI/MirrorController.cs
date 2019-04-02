using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
			CutoutShader = myLoadedAssetBundle.LoadAsset<Shader>("Assets/Shaders/sh_custom_unlit_transparent.shader");
			StartCoroutine(SpawnMirror());
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
			_cam.orthographicSize = 0.8f;
			_cam.clearFlags = CameraClearFlags.Depth;
			_cam.depthTextureMode = DepthTextureMode.Depth;

			int layer1 = 3;
			int layer2 = 4;
			int layerMask1 = 1 << layer1;
			int layerMask2 = 1 << layer2;
			int finalMask = layerMask1 | layerMask2;


			_cam.cullingMask = finalMask;


			var _liv = _cam.GetComponent<LIV.SDK.Unity.LIV>();
			if (_liv)
				Destroy(_liv);

			mirrorCamObj.SetActive(true);

			mirrorCamObj.transform.position = new Vector3(0, 1.3f, 5);
			mirrorCamObj.transform.rotation = Quaternion.Euler(0, 180, 0);


			_camRenderTexture = new RenderTexture(1, 1, 24);
			_camRenderTexture.width = _cam.pixelWidth;
			_camRenderTexture.height = _cam.pixelHeight;
			_camRenderTexture.useDynamicScale = false;
			_camRenderTexture.Create();

			_cam.targetTexture = _camRenderTexture;



			_mirrorMaterial = new Material(CutoutShader);
			_mirrorMaterial.SetTexture("_Tex", _camRenderTexture);

			_quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			DontDestroyOnLoad(_quad);
			DestroyImmediate(_quad.GetComponent<Collider>());
			_quad.GetComponent<MeshRenderer>().material = _mirrorMaterial;
			_quad.transform.parent = mirrorCamObj.transform;
			_quad.transform.localPosition = new Vector3(0,0,3);
			_quad.transform.localEulerAngles = new Vector3(0, 180, 0);
			_quad.transform.localScale = new Vector3(_cam.aspect*3, 3, 3);
		}
	}
}
