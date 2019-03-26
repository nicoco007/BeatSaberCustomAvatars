using UnityEngine;
using System.Linq;

namespace CustomAvatar
{
	public static class AvatarSpawner
	{
		public static SpawnedAvatar SpawnAvatar(CustomAvatar customAvatar, IAvatarInput avatarInput)
		{
			if (customAvatar.GameObject == null)
			{
				Plugin.Log("Can't spawn " + customAvatar.FullPath + " because it hasn't been loaded!");
				return null;
			}

			var avatarGameObject = Object.Instantiate(customAvatar.GameObject);

			var behaviour = avatarGameObject.AddComponent<AvatarBehaviour>();
			behaviour.Init(avatarInput);

			avatarGameObject.AddComponent<AvatarEventsPlayer>();

			/* Don't have the patience to make this work rn
			 
			var mainCamera = Camera.main;

			foreach (Camera cam in avatarGameObject.GetComponentsInChildren<Camera>())
			{
				if(mainCamera)
				{
					var newCamObj = Object.Instantiate(mainCamera, cam.transform);
					newCamObj.tag = "Untagged";
					while (newCamObj.transform.childCount > 0) Object.DestroyImmediate(newCamObj.transform.GetChild(0).gameObject);
					Object.DestroyImmediate(newCamObj.GetComponent("CameraRenderCallbacksManager"));
					Object.DestroyImmediate(newCamObj.GetComponent("AudioListener"));
					Object.DestroyImmediate(newCamObj.GetComponent("MeshCollider"));

					var newCam = newCamObj.GetComponent<Camera>();
					newCam.stereoTargetEye = StereoTargetEyeMask.None;
					newCam.cullingMask = cam.cullingMask;

					var _liv = newCam.GetComponent<LIV.SDK.Unity.LIV>();
					if (_liv)
						Object.Destroy(_liv);

					var _screenCamera = new GameObject("Screen Camera").AddComponent<ScreenCameraBehaviour>();

					if (_previewMaterial == null)
						_previewMaterial = new Material(Shader.Find("Hidden/BlitCopyWithDepth"));


					cam.enabled = false;
				}
			}
			*/
			
			Object.DontDestroyOnLoad(avatarGameObject);

			var spawnedAvatar = new SpawnedAvatar(customAvatar, avatarGameObject);
			
			return spawnedAvatar;
		}
	}
}
