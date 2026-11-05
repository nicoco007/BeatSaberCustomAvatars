//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Rendering;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class CalibratableNodeVisuals : MonoBehaviour
    {
        [SerializeField] private CalibratableNode _calibratableNode;

        [SerializeField] private Transform _grabSphere;

        [field: SerializeField]
        internal GameObject grabSphere { get; private set; }

        protected void LateUpdate()
        {
            transform.CopyPositionAndRotationFrom(_calibratableNode.transform);
            _grabSphere.CopyPositionAndRotationFrom(_calibratableNode.manualOffset);
        }

        internal class Mirrored : MonoBehaviour
        {
            [SerializeField] private MirroredCalibratableNode _calibratableNode;

            [SerializeField] private Transform _grabSphere;

            [field: SerializeField] internal GameObject grabSphere { get; private set; }

            private CalibratableNodeVisuals _calibratableNodeVisuals;

            internal void Init(CalibratableNodeVisuals calibratableNodeVisuals)
            {
                _calibratableNodeVisuals = calibratableNodeVisuals;
                _calibratableNode.Init(calibratableNodeVisuals._calibratableNode);
            }

            internal void OnBeforeRender()
            {
                transform.CopyLocalPositionAndRotationFrom(_calibratableNodeVisuals.transform);
                _grabSphere.CopyLocalPositionAndRotationFrom(_calibratableNodeVisuals._grabSphere);
            }
        }
    }
}
