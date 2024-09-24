//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
    internal class GenericNode : ITrackedNode
    {
        public GenericNode(string name)
        {
            gameObject = new GameObject(name);
            transform = gameObject.transform;

            calibration = new GameObject($"{name} Calibration").transform;
            calibration.SetParent(transform, false);

            offset = new GameObject($"{name} Offset").transform;
            offset.SetParent(calibration, false);
        }

        public GameObject gameObject { get; protected set; }

        public Transform transform { get; protected set; }

        public Transform offset { get; protected set; }

        public Transform calibration { get; }

        public bool isTracking { get; set; }

        public bool isCalibrated { get; set; }
    }
}
