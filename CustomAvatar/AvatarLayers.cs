using UnityEngine;

namespace CustomAvatar
{
	public static class AvatarLayers
	{
		public const int OnlyInThirdPerson = 30;
		public const int Global = 31;

		public static void SetChildrenToLayer(GameObject gameObject, int layer)
		{
			foreach (var child in gameObject.GetComponentsInChildren<Transform>())
			{
				child.gameObject.layer = layer;
			}
		}
	}
}
