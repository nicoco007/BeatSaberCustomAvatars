using UnityEngine;

namespace CustomAvatar
{
	public static class AvatarLayers
	{
		public const int OnlyInThirdPerson = 3;
		public const int OnlyInFirstPerson = 4;

		public static void SetChildrenToLayer(GameObject gameObject, int layer)
		{
			foreach (var child in gameObject.GetComponentsInChildren<Transform>())
			{
				child.gameObject.layer = layer;
			}
		}
	}
}