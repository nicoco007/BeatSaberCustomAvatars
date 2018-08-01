using System;
using UnityEngine;

namespace CustomAvatar
{
	public class AvatarPrefab
	{
		private const string GameObjectName = "_CustomAvatar";
		private AvatarDescriptor _descriptor;

		public GameObject Prefab { get; private set; }

		public string AvatarName
		{
			get
			{
				return _descriptor == null ? null : _descriptor.AvatarName;
			}
		}

		public string AuthorName
		{
			get
			{
				return _descriptor == null ? null : _descriptor.AuthorName;
			}
		}

		public float Height
		{
			get { return _descriptor == null ? Plugin.DefaultPlayerHeight : _descriptor.Height; }
		}

		public bool AllowHeightCalibration
		{
			get { return _descriptor == null || _descriptor.AllowHeightCalibration; }
		}

		public AvatarPrefab(AssetBundle assetBundle, Action<GameObject> loadedCallback)
		{
			if (assetBundle == null) return;

			var assetBundleRequest = assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(GameObjectName);
			assetBundleRequest.completed += LoadAssetCompleted;
			
			void LoadAssetCompleted(AsyncOperation asyncOperation)
			{
				Prefab = (GameObject) assetBundleRequest.asset;
				if (Prefab != null)
				{
					_descriptor = Prefab.GetComponent<AvatarDescriptor>();
				}
				
				loadedCallback(Prefab);
			}
		}
	}
}