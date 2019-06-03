using UnityEngine;
using UnityEngine.XR;

namespace CustomAvatar.StereoRendering
{
	class UnityXRParamFactory : IDeviceParamFactory
	{
		private const float IPD = 0.06567926f;

		public int GetRenderWidth()
		{
			return XRSettings.eyeTextureWidth;
		}

		public int GetRenderHeight()
		{
			return XRSettings.eyeTextureHeight;
		}

		public Vector3 GetEyeSeperation(int eye)
		{
			if (eye == 0)
			{
				return new Vector3(-IPD / 2f, 0, 0);
			}
			else if (eye == 1)
			{
				return new Vector3(IPD / 2f, 0, 0);
			}

			return Vector3.zero;
		}

		public Quaternion GetEyeLocalRotation(int eye)
		{
			return Quaternion.identity;
		}

		public Matrix4x4 GetProjectionMatrix(int eye, float nearPlane, float farPlane)
		{
			if (eye == 0)
			{
				return Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
			}
			else if (eye == 1)
			{
				return Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
			}

			return Matrix4x4.identity;
		}
	}
}
