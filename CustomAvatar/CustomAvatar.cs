using System;
using UnityEngine;

namespace CustomAvatar
{
	public class CustomAvatar
	{
		private const string GameObjectName = "_CustomAvatar";

		private float? _height;
		private AssetBundleCreateRequest assetBundleCreateRequest;
		private AssetBundleRequest assetBundleRequest;
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

		public float Height
		{
			get
			{
				if (GameObject == null) return AvatarMeasurement.DefaultPlayerHeight;
				if (_height == null)
				{
					_height = AvatarMeasurement.MeasureHeight(GameObject, ViewPoint);
				}

				return _height.Value;
			}
		}

		public void Load(Action<CustomAvatar, AvatarLoadResult> loadedCallback)
		{
			if (IsLoaded)
			{
				loadedCallback(this, AvatarLoadResult.Completed);
				return;
			}

			assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(FullPath);
			assetBundleCreateRequest.completed += asyncOperation => AssetBundleLoaded(loadedCallback);
		}

		private void AssetBundleLoaded(Action<CustomAvatar, AvatarLoadResult> loadedCallback)
		{
			if (!assetBundleCreateRequest.isDone || assetBundleCreateRequest.assetBundle == null)
			{
				loadedCallback(this, AvatarLoadResult.Failed);
				return;
			}

			assetBundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(GameObjectName);
			assetBundleRequest.completed += asyncOperation => AssetLoaded(loadedCallback);
		}

		private void AssetLoaded(Action<CustomAvatar, AvatarLoadResult> loadedCallback)
		{
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
