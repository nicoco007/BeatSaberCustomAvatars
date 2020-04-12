//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using Zenject;

namespace CustomAvatar.StereoRendering
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    internal class VRRenderEventDetector : MonoBehaviour
    {
        public Camera Camera { get; private set; }

        private StereoRenderManager _manager { get; set; }

        [Inject]
        private void Inject(StereoRenderManager manager)
        {
            _manager = manager;
        }

        public void Start()
        {
            // check instantiated properly
            if (_manager == null)
            {
                Destroy(this);
            }

            Camera = GetComponent<Camera>();
        }

        private void OnPreRender()
        {
            _manager.InvokeStereoRenderers(this);
        }
    }
}
