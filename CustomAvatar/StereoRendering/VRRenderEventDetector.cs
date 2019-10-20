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

		public void Initialize(int eye)
		{
			this.unityCamera = GetComponent<Camera>();
			this.eye = eye;
			this.initialized = true;
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
