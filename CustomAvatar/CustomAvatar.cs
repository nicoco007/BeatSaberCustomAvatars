using System;
using CustomAvatar.Exceptions;
using System.Linq;
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

		public static CustomAvatar FromFile(string filePath)
		{
			AssetBundle assetBundle = AssetBundle.LoadFromFile(filePath);

			if (assetBundle == null)
			{
				throw new AvatarLoadException("Could not load asset bundle");
			}

			GameObject[] gameObjects = assetBundle.LoadAssetWithSubAssets<GameObject>(GameObjectName);
			var avatarGameObject = gameObjects.FirstOrDefault();

			if (avatarGameObject == null)
			{
				throw new AvatarLoadException("Avatar game object not found");
			}

			return new CustomAvatar(filePath, avatarGameObject);
		}
	}
}
