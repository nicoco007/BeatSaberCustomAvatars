using UnityEngine;

namespace CustomAvatar
{
	public class AvatarPreviewRotation : MonoBehaviour
	{
		public static bool rotatePreview;
		public float rotationSpeed = 20f;
		private Vector3 defaultRotation = new Vector3 (0, -120, 0);

		public void Reset()
		{
			transform.Rotate(defaultRotation);
		}

		void Update()
		{
			if (rotatePreview)
			{
				transform.Rotate(Vector3.up, Time.deltaTime * rotationSpeed, Space.World);
			}
		}
	}
}
