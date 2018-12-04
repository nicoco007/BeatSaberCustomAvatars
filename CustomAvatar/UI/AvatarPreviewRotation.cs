using UnityEngine;

namespace CustomAvatar
{
	public class AvatarPreviewRotation : MonoBehaviour
	{
		public static bool rotatePreview = false;
		public float rotationSpeed = 20f;
		private Vector3 defaultRotation = new Vector3 (0, -120, 0);

		void Awake()
		{
			Plugin.Log("AvatarPreviewRotation is awake");
		}

		public void Reset()
		{
			transform.Rotate(defaultRotation);
		}

		void Update()
		{
			if (rotatePreview)
			{
				transform.Rotate(Vector3.up * Time.deltaTime * rotationSpeed);
			}
		}
	}
}
