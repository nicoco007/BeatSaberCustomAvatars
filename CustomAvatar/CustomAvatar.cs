using System;
using UnityEngine;

namespace CustomAvatar
{
	public class CustomAvatar
	{
		public string Name
		{
			get
			{
				if (_assetBundle == null || _assetBundle.AvatarPrefab == null) return null;
				return _assetBundle.AvatarPrefab.AvatarName;
			}
		}

		public string AuthorName
		{
			get
			{
				if (_assetBundle == null || _assetBundle.AvatarPrefab == null) return null;
				return _assetBundle.AvatarPrefab.AuthorName;
			}
		}

		public bool IsLoaded
		{
			get { return _assetBundle.AssetBundle != null; }
		}

		public string FullPath { get; }

		public float Height
		{
			get
			{
				if (_assetBundle == null || _assetBundle.AvatarPrefab == null) return Plugin.DefaultPlayerHeight;
				return _assetBundle.AvatarPrefab.Height;
			}
		}

		public bool AllowHeightCalibration
		{
			get
			{
				if (_assetBundle == null || _assetBundle.AvatarPrefab == null) return true;
				return _assetBundle.AvatarPrefab.AllowHeightCalibration;
			}
		}

		public GameObject GameObject
		{
			get
			{
				return _assetBundle.AvatarPrefab?.Prefab;
			}
		}

		private readonly AvatarAssetBundle _assetBundle;

		internal CustomAvatar(string fullPath)
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