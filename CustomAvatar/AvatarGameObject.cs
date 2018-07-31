using UnityEngine;

namespace CustomAvatar
{
	public class AvatarGameObject
	{
		private const string GameObjectName = "_customavatar";
		private readonly AvatarDescriptor _descriptor;
		
		public GameObject GameObject { get; }

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

		public AvatarGameObject(AssetBundle assetBundle)
		{
			if (assetBundle == null) return;

			GameObject = assetBundle.LoadAsset<GameObject>(GameObjectName);
			if (GameObject == null) return;
			_descriptor = GameObject.GetComponent<AvatarDescriptor>();
		}
	}
}