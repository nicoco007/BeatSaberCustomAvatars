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

using CustomAvatar.Tracking;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    [DisallowMultipleComponent]
    public class AvatarFingerTracking : MonoBehaviour
    {
        private PoseManager _poseManager;
        private IAvatarInput _input;

        [Inject]
        [UsedImplicitly]
        private void Construct(IAvatarInput input)
        {
            _input = input;
        }

        protected void Start()
        {
            _poseManager = GetComponentInChildren<PoseManager>();
        }

        protected void LateUpdate()
        {
            ApplyFingerTracking();
        }

        private void ApplyFingerTracking()
        {
            if (_input.TryGetFingerCurl(DeviceUse.LeftHand, out FingerCurl leftFingerCurl))
            {
                _poseManager.ApplyLeftHandFingerPoses(leftFingerCurl.thumb, leftFingerCurl.index, leftFingerCurl.middle, leftFingerCurl.ring, leftFingerCurl.little);
            }
            else
            {
                _poseManager.ApplyLeftHandFingerPoses(1, 1, 1, 1, 1);
            }

            if (_input.TryGetFingerCurl(DeviceUse.RightHand, out FingerCurl rightFingerCurl))
            {
                _poseManager.ApplyRightHandFingerPoses(rightFingerCurl.thumb, rightFingerCurl.index, rightFingerCurl.middle, rightFingerCurl.ring, rightFingerCurl.little);
            }
            else
            {
                _poseManager.ApplyRightHandFingerPoses(1, 1, 1, 1, 1);
            }
        }
    }
}
