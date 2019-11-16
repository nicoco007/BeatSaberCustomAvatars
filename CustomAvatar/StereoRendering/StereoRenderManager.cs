//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;

namespace CustomAvatar.StereoRendering
{
    [DisallowMultipleComponent]
    public class StereoRenderManager : MonoBehaviour
    {
        // singleton
        private static StereoRenderManager instance = null;

        // all current stereo renderers
        public List<StereoRenderer> stereoRenderers = new List<StereoRenderer>();

        /////////////////////////////////////////////////////////////////////////////////
        // initialization

        // whehter we have initialized the singleton
        public static bool Active { get { return instance != null; } }

        // singleton interface
        public static StereoRenderManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("StereoRenderManager").AddComponent<StereoRenderManager>();
                    Plugin.Logger.Info("Initialized StereoRenderManager");
                }

                return instance;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        // render related

        public void InvokeStereoRenderers(VRRenderEventDetector detector)
        {
            // render registored stereo cameras
            for (int renderIter = 0; renderIter < stereoRenderers.Count; renderIter++)
            {
                StereoRenderer stereoRenderer = stereoRenderers[renderIter];

                if (stereoRenderer.shouldRender)
                {
                    stereoRenderer.Render(detector);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        // callbacks

        public void AddToManager(StereoRenderer stereoRenderer)
        {
            stereoRenderers.Add(stereoRenderer);
        }

        public void RemoveFromManager(StereoRenderer stereoRenderer)
        {
            stereoRenderers.Remove(stereoRenderer);
        }
    }
}
