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
using UnityEngine.Animations;

namespace CustomAvatar.Utilities
{
    /// <summary>
    /// <see cref="ScaleConstraint"/> tends to be slightly off from the source transform scale when its components aren't integers.
    /// We don't expect the player space to be scaled/skewed non-uniformly so using <see cref="Transform.lossyScale"/> instead is good enough.
    /// </summary>
    internal class LossyScaleConstraint : MonoBehaviour
    {
        [SerializeField]
        private Transform _sourceTransform;

        public Transform sourceTransform
        {
            get => _sourceTransform;
            set
            {
                _sourceTransform = value;
                enabled = value != null;
            }

        }

        protected void OnEnable()
        {
            if (_sourceTransform == null)
            {
                enabled = false;
            }
        }

        protected void Update()
        {
            transform.localScale = sourceTransform.lossyScale;
        }
    }
}
