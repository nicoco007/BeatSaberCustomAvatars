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
