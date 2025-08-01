﻿//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class TrackedRenderModel : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        internal static TrackedRenderModel Create(Transform parent)
        {
            GameObject gameObject = new("Render Model");
            gameObject.transform.SetParent(parent, false);

            TrackedRenderModel trackedRenderModel = gameObject.AddComponent<TrackedRenderModel>();
            trackedRenderModel._meshFilter = gameObject.AddComponent<MeshFilter>();
            trackedRenderModel._meshRenderer = gameObject.AddComponent<MeshRenderer>();

            return trackedRenderModel;
        }

        public void SetRenderModel(RenderModel renderModel)
        {
            transform.SetLocalPose(renderModel?.localOrigin ?? Pose.identity);
            _meshFilter.sharedMesh = renderModel?.mesh;
            _meshRenderer.sharedMaterial = renderModel?.material;
        }
    }
}
