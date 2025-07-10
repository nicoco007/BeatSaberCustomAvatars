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
    internal class ControllerNode : MonoBehaviour, ITrackedNode
    {
        internal static ControllerNode Create(string name, Transform parent)
        {
            GameObject gameObject = new(name);

            Transform transform = gameObject.transform;
            transform.SetParent(parent, false);

            ControllerNode controllerNode = gameObject.AddComponent<ControllerNode>();

            controllerNode.viewTransform = new GameObject("View Transform").transform;
            controllerNode.viewTransform.SetParent(transform, false);

            controllerNode.offset = new GameObject("Offset").transform;
            controllerNode.offset.SetParent(controllerNode.viewTransform, false);

            return controllerNode;
        }

        public Transform offset { get; private set; }

        public Transform viewTransform { get; private set; }

        public VRController controller { get; set; }

        public bool isTracking { get; set; }

        // We don't have to calibrate controllers
        public bool isCalibrated => true;
    }
}
