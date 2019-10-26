using System;
using System.Collections.Generic;
using CustomAvatar.Exceptions;
using UnityEngine;

namespace CustomAvatar
{
	public class CustomAvatar
	{
		private const float kMinIKAvatarHeight = 1.4f;
		private const float kMaxIKAvatarHeight = 2.5f;
		private const string kGameObjectName = "_CustomAvatar";
		private float? _eyeHeight;

		public string fullPath { get; }
		public GameObject gameObject { get; }
		public AvatarDescriptor descriptor { get; }
		public Transform viewPoint { get; }

		public float eyeHeight
		{
			get
			{
				if (gameObject == null) return BeatSaberUtil.GetPlayerEyeHeight();
				if (_eyeHeight == null)
				{
					var localPosition = gameObject.transform.InverseTransformPoint(viewPoint.position);
					_eyeHeight = localPosition.y;
			
					//This is to handle cases where the head might be at 0,0,0, like in a non-IK avatar.
					if (_eyeHeight < kMinIKAvatarHeight || _eyeHeight > kMaxIKAvatarHeight)
					{
						_eyeHeight = MainSettingsModel.kDefaultPlayerHeight;
					}
				}

				return _eyeHeight.Value;
			}
		}

		public CustomAvatar(string fullPath, GameObject avatarGameObject)
		{
			this.fullPath = fullPath ?? throw new ArgumentNullException(nameof(avatarGameObject));
			gameObject = avatarGameObject ?? throw new ArgumentNullException(nameof(avatarGameObject));
			descriptor = avatarGameObject.GetComponent<AvatarDescriptor>() ?? throw new AvatarLoadException($"Avatar at '{fullPath}' does not have an AvatarDescriptor");
			viewPoint = avatarGameObject.transform.Find("Head") ?? throw new AvatarLoadException($"Avatar '{descriptor.Name}' does not have a Head transform");
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

			AssetBundleRequest assetBundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetWithSubAssetsAsync<GameObject>(kGameObjectName);
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
