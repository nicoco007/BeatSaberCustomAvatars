//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace CustomAvatar.StereoRendering
{
	[RequireComponent(typeof(Camera))]
	[DisallowMultipleComponent]
	public class VRRenderEventDetector : MonoBehaviour
	{
		public Camera unityCamera;
		public int eye;
		private bool initialized = false;

		public void Initialize(int e)
		{
			unityCamera = GetComponent<Camera>();
			eye = e;
			initialized = true;
		}

		private void OnPreRender()
		{
			if (initialized)
			{
				StereoRenderManager.Instance.InvokeStereoRenderers(this);
			}
		}
	}
}
