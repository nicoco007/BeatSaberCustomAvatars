//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;

namespace CustomAvatar.StereoRendering
{
    [Serializable]
    internal class StereoRenderManager
    {
        // all current stereo renderers
        public List<StereoRenderer> stereoRenderers = new List<StereoRenderer>();

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
