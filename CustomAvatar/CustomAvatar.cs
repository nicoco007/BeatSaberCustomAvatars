using System;
using System.Collections;
using UnityEngine;

namespace CustomAvatar
{
	public class CustomAvatar
	{
		private const string GameObjectName = "_CustomAvatar";

		private float? eyeheight;
		private AvatarDescriptor descriptor;

		internal CustomAvatar(string fullPath)
		{
			FullPath = fullPath;
		}

		public string FullPath { get; }
		public GameObject GameObject { get; private set; }
		public bool IsLoaded { get; private set; }
		public string Name => descriptor?.Name;
		public string AuthorName => descriptor?.Author;
		public Sprite CoverImage => descriptor?.Cover;
		public bool AllowHeightCalibration => descriptor != null ? descriptor.AllowHeightCalibration : true;
		public Transform ViewPoint { get; private set; }

		public float EyeHeight
		{
			get
			{
				if (GameObject == null) return BeatSaberUtil.GetPlayerViewPointHeight();
				if (eyeheight == null)
				{
					eyeheight = AvatarMeasurement.MeasureEyeHeight(GameObject, ViewPoint);
				}

				return eyeheight.Value;
			}
		}

		public void Load(Action<CustomAvatar, AvatarLoadResult> loadedCallback)
		{
			if (IsLoaded)
			{
				loadedCallback(this, AvatarLoadResult.Completed);
				return;
			}

			SharedCoroutineStarter.instance.StartCoroutine(LoadAvatar(loadedCallback));
		}

		private IEnumerator LoadAvatar(Action<CustomAvatar, AvatarLoadResult> loadedCallback)
		{
			AssetBundleCreateRequest createRequest = AssetBundle.LoadFromFileAsync(FullPath);
			yield return createRequest;

			if (!createRequest.isDone || createRequest.assetBundle == null)
			{
				loadedCallback(this, AvatarLoadResult.Failed);
				yield break;
			}

			AssetBundleRequest assetBundleRequest = createRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(GameObjectName);
			yield return assetBundleRequest;

			GameObject = (GameObject)assetBundleRequest.asset;

			if (GameObject == null)
			{
				loadedCallback(this, AvatarLoadResult.Invalid);
			}

			descriptor = GameObject.GetComponent<AvatarDescriptor>();
			ViewPoint = GameObject.transform.Find("Head");

			IsLoaded = true;
			loadedCallback(this, AvatarLoadResult.Completed);
		}
	}
}
