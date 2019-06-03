using CustomAvatar.StereoRendering;
using System.Collections;
using UnityEngine;

namespace CustomAvatar
{
	class MirrorController : MonoBehaviour
	{
		public static MirrorController Instance;

		private static readonly Vector3 MIRROR_POSITION = new Vector3(0, 0, 2.499f); // origin is bottom center
		private static readonly Quaternion MIRROR_ROTATION = Quaternion.Euler(-90f, 0, 0);
		private static readonly Vector3 MIRROR_SCALE = new Vector3(0.3f, 1f, 0.24f);

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
			mirrorPlane.transform.parent = transform;
			mirrorPlane.transform.localScale = MIRROR_SCALE;
			mirrorPlane.transform.position = MIRROR_POSITION + new Vector3(0, MIRROR_SCALE.z * 5, 0); // plane is 10 units in size at scale 1
			mirrorPlane.transform.rotation = MIRROR_ROTATION;

			Renderer renderer = mirrorPlane.GetComponent<Renderer>();
			renderer.sharedMaterial = new Material(stereoRenderShader);

			StereoRenderer stereoRenderer = mirrorPlane.AddComponent<StereoRenderer>();
			stereoRenderer.isMirror = true;
			stereoRenderer.useScissor = false;

			stereoRenderer.canvasOriginPos = mirrorPlane.transform.position;
			stereoRenderer.canvasOriginRot = mirrorPlane.transform.rotation;
		}
	}
}
