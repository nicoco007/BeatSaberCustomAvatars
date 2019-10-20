using System;
using System.Collections.Generic;
using CustomAvatar.Exceptions;
using UnityEngine;

namespace CustomAvatar
{
	public class CustomAvatar
	{
		private const string GameObjectName = "_CustomAvatar";
		private float? eyeHeight;

		public string FullPath { get; }
		public GameObject GameObject { get; }
		public AvatarDescriptor Descriptor { get; }
		public Transform ViewPoint { get; }

		public float EyeHeight
		{
			get
			{
				if (GameObject == null) return BeatSaberUtil.GetPlayerViewPointHeight();
				if (eyeHeight == null)
				{
					eyeHeight = AvatarMeasurement.MeasureEyeHeight(GameObject, ViewPoint);
				}

				return eyeHeight.Value;
			}
		}

		public CustomAvatar(string fullPath, GameObject avatarGameObject)
		{
			FullPath = fullPath ?? throw new ArgumentNullException(nameof(avatarGameObject));
			GameObject = avatarGameObject ?? throw new ArgumentNullException(nameof(avatarGameObject));
			Descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");
			ViewPoint = avatarGameObject.transform.Find("Head") ?? throw new AvatarLoadException($"Avatar '{Descriptor.Name}' does not have a Head transform");
		}

		public static IEnumerator<AsyncOperation> FromFileCoroutine(string filePath, Action<CustomAvatar> success, Action<Exception> error)
		{
			Plugin.Logger.Info("Loading avatar from " + filePath);

			AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(filePath);
			yield return assetBundleCreateRequest;

			if (!assetBundleCreateRequest.isDone || assetBundleCreateRequest.assetBundle == null)
			{
				error(new AvatarLoadException("Avatar game object not found"));
				yield break;
			}

			AssetBundleRequest assetBundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(GameObjectName);
			yield return assetBundleRequest;
			assetBundleCreateRequest.assetBundle.Unload(false);

			if (!assetBundleRequest.isDone || assetBundleRequest.asset == null)
			{
				error(new AvatarLoadException("Could not load asset bundle"));
				yield break;
			}
				
			try
			{
				success(new CustomAvatar(filePath, assetBundleRequest.asset as GameObject));
			}
			catch (Exception ex)
			{
				error(ex);
			}
		}
	}
}
