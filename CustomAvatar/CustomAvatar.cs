using System;
using UnityEngine;

namespace CustomAvatar
{
	public class CustomAvatar : IAvatar
	{
		public string Name
		{
			get
			{
				if (_assetBundle == null || _assetBundle.AvatarGameObject == null) return null;
				return _assetBundle.AvatarGameObject.AvatarName;
			}
		}

		public string AuthorName
		{
			get
			{
				if (_assetBundle == null || _assetBundle.AvatarGameObject == null) return null;
				return _assetBundle.AvatarGameObject.AuthorName;
			}
		}

		public bool IsLoaded
		{
			get { return _assetBundle.AssetBundle != null; }
		}

		public string FullPath { get; }

		public GameObject GameObject
		{
			get
			{
				return _assetBundle.AvatarGameObject?.GameObject;
			}
		}

		private readonly AvatarAssetBundle _assetBundle;

		public CustomAvatar(string fullPath)
		{
			FullPath = fullPath;
			_assetBundle = new AvatarAssetBundle(FullPath);
		}

		public void Load(Action<CustomAvatar, AvatarLoadResult> loadedCallback)
		{
			if (IsLoaded)
			{
				loadedCallback(this, AvatarLoadResult.Completed);
				return;
			}

			void Loaded(AvatarLoadResult result)
			{
				loadedCallback(this, result);
			}

			_assetBundle.LoadAssetBundle(Loaded);
		}
	}
}