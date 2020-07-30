//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;
using Zenject;

namespace CustomAvatar.StereoRendering
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    internal class VRRenderEventDetector : MonoBehaviour
    {
        public Guid id { get; private set; }
        public Camera camera { get; private set; }
        public StereoRenderManager manager { get; private set; }

        [Inject]
        private void Inject(StereoRenderManager manager)
        {
            this.id = Guid.NewGuid();
            this.manager = manager;
            this.camera = GetComponent<Camera>();
        }

        private void OnPreRender()
        {
            if (manager != null)
            {
                manager.InvokeStereoRenderers(this);
            }
            else
            {
                Destroy(this);
            }
        }
    }
}
