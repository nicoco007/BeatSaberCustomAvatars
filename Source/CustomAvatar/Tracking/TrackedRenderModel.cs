//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
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

using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class TrackedRenderModel
    {
        private readonly GameObject _gameObject;

        public TrackedRenderModel(GameObject gameObject, MeshFilter meshFilter, MeshRenderer meshRenderer)
        {
            _gameObject = gameObject;
            this.transform = gameObject.transform;
            this.meshFilter = meshFilter;
            this.meshRenderer = meshRenderer;
        }

        public Transform transform { get; }

        public MeshFilter meshFilter { get; }

        public MeshRenderer meshRenderer { get; }

        public void SetActive(bool value) => _gameObject.SetActive(value);
    }
}
