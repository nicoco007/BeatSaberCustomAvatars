using System;
using UnityEngine;

namespace CustomAvatar
{
	public class CustomAvatar
	{
		public AvatarAssetBundle AssetBundle { get; }
		private float? _height;

		internal CustomAvatar(string fullPath)
		{
			FullPath = fullPath;
			AssetBundle = new AvatarAssetBundle(FullPath);
		}

		public string FullPath { get; }
		
		public string Name
		{
			get
			{
				if (AssetBundle == null || AssetBundle.AvatarPrefab == null) return null;
				return AssetBundle.AvatarPrefab.AvatarName;
			}
		}

		public string AuthorName
		{
			get
			{
				if (AssetBundle == null || AssetBundle.AvatarPrefab == null) return null;
				return AssetBundle.AvatarPrefab.AuthorName;
			}
		}

		public Sprite CoverImage
		{
			get
			{
				if (AssetBundle == null || AssetBundle.AvatarPrefab == null) return null;
				return AssetBundle.AvatarPrefab.CoverImage;
			}
		}

		public Transform ViewPoint
		{
			get
			{
				if (AssetBundle == null || AssetBundle.AvatarPrefab == null) return null;
				return AssetBundle.AvatarPrefab.ViewPoint;
			}
		}

		public float EyeHeight
		{
			get
			{
				if (GameObject == null) return MainSettingsModel.kDefaultPlayerHeight - MainSettingsModel.kHeadPosToPlayerHeightOffset;
				if (_height == null)
				{
					_height = AvatarMeasurement.MeasureHeight(GameObject, AssetBundle.AvatarPrefab.ViewPoint);
				}

				return _height.Value;
			}
		}

		public bool AllowHeightCalibration
		{
			get
			{
				if (AssetBundle == null || AssetBundle.AvatarPrefab == null) return true;
				return AssetBundle.AvatarPrefab.AllowHeightCalibration;
			}
		}

		public bool IsLoaded
		{
			get { return AssetBundle.AssetBundle != null; }
		}

		public GameObject GameObject
		{
			get
			{
				return AssetBundle.AvatarPrefab?.Prefab;
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

			AssetBundle.LoadAssetBundle(Loaded);
		}
	}
}
