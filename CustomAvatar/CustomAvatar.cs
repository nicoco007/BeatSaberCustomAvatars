using System;
using UnityEngine;

namespace CustomAvatar
{
	public class CustomAvatar
	{
		private float? _height;
		private readonly AvatarAssetBundle _assetBundle;

		internal CustomAvatar(string fullPath)
		{
			FullPath = fullPath;
			_assetBundle = new AvatarAssetBundle(FullPath);
		}

		public string FullPath { get; }
		
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

		public Transform ViewPoint
		{
			get
			{
				if (_assetBundle == null || _assetBundle.AvatarPrefab == null) return null;
				return _assetBundle.AvatarPrefab.ViewPoint;
			}
		}

		public float Height
		{
			get
			{
				if (GameObject == null) return AvatarMeasurement.DefaultPlayerHeight;
				if (_height == null)
				{
					_height = AvatarMeasurement.MeasureHeight(GameObject, _assetBundle.AvatarPrefab.ViewPoint);
				}

				return _height.Value;
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

		public bool IsLoaded
		{
			get { return _assetBundle.AssetBundle != null; }
		}

		public GameObject GameObject
		{
			get
			{
				return _assetBundle.AvatarPrefab?.Prefab;
			}
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