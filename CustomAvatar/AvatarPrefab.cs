using System;
using UnityEngine;

namespace CustomAvatar
{
	public class AvatarPrefab
	{
		private const string GameObjectName = "_CustomAvatar";
		private AvatarDescriptor _descriptor;
		private Transform _viewPoint;

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

		public GameObject Prefab { get; private set; }

		public string AvatarName
		{
			get
			{
				return _descriptor == null ? null : _descriptor.Name;
			}
		}

		public string AuthorName
		{
			get
			{
				return _descriptor == null ? null : _descriptor.Author;
			}
		}

		public Sprite CoverImage
		{
			get
			{
				return _descriptor == null ? null : _descriptor.Cover;
			}
		}

		public Transform ViewPoint
		{
			get
			{
				if (_viewPoint != null) return _viewPoint;
				
				//_viewPoint = _descriptor == null ? null : _descriptor.ViewPoint;
				//if (_viewPoint == null)
				//{
					_viewPoint = Prefab.transform.Find("Head");
				//}

				return _viewPoint;
			}
		}

		public bool AllowHeightCalibration
		{
			get { return _descriptor == null || _descriptor.AllowHeightCalibration; }
		}
	}
}