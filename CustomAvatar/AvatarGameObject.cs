using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
	public class AvatarGameObject
	{
		private const string GameObjectName = "_CustomAvatar";
		private AvatarDescriptor _descriptor;

		public GameObject GameObject { get; private set; }

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

		public AvatarGameObject(AssetBundle assetBundle, Action<GameObject> loadedCallback)
		{
			if (assetBundle == null) return;

			var assetBundleRequest = assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(GameObjectName);
			assetBundleRequest.completed += LoadAssetCompleted;
			
			void LoadAssetCompleted(AsyncOperation asyncOperation)
			{
				GameObject = (GameObject) assetBundleRequest.asset;
				if (GameObject != null)
				{
					_descriptor = GameObject.GetComponent<AvatarDescriptor>();
				}
				
				loadedCallback(GameObject);
			}
		}
	}
}