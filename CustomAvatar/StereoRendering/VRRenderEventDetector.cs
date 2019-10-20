//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace CustomAvatar.StereoRendering
{
	[RequireComponent(typeof(Camera))]
	[DisallowMultipleComponent]
	public class VRRenderEventDetector : MonoBehaviour
	{
		public Camera Camera { get; private set; }

		public void Start()
		{
			Camera = GetComponent<Camera>();
		}

		private void OnPreRender()
		{
			StereoRenderManager.Instance.InvokeStereoRenderers(this);
		}
	}
}
