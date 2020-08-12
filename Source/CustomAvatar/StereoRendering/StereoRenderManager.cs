//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

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
