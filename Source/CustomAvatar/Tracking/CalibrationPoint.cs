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

using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Tracking
{
    internal class CalibrationPoint : MonoBehaviour
    {
        [field: SerializeField]
        internal CalibratableNode node { get; set; }

        [SerializeField]
        private Transform _target;

        [SerializeField]
        private Transform _sphere;

        [SerializeField]
        private Transform _cone;

        [SerializeField]
        private Transform _endCap;

        protected void LateUpdate()
        {
            Vector3 target = _target.position;
            float distance = transform.InverseTransformPoint(target).magnitude;
            _cone.localPosition = new Vector3(0, 0, distance / 2);
            _cone.localScale = new Vector3(1, 1, distance);
            _endCap.localPosition = new Vector3(0, 0, distance);
            transform.LookAt(target);
            _sphere.localScale = (2 * distance + 1) * Vector3.one;
        }

        internal class Mirrored : MonoBehaviour
        {
            [SerializeField]
            private Transform _sphere;

            [SerializeField]
            private Transform _cone;

            [SerializeField]
            private Transform _endCap;

            internal void CopyFrom(CalibrationPoint calibrationPoint)
            {
                transform.CopyLocalPositionAndRotationFrom(calibrationPoint.transform);
                _cone.localPosition = calibrationPoint._cone.localPosition;
                _cone.localScale = calibrationPoint._cone.localScale;
                _endCap.localPosition = calibrationPoint._endCap.localPosition;
                _sphere.localScale = calibrationPoint._sphere.localScale;
            }
        }
    }
}
