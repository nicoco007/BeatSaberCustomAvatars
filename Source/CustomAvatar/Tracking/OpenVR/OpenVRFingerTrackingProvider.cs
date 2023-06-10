//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System;
using DynamicOpenVR.IO;
using Zenject;

namespace CustomAvatar.Tracking.OpenVR
{
    internal class OpenVRFingerTrackingProvider : IFingerTrackingProvider, IInitializable, IDisposable
    {
        private SkeletalInput _leftHandAnimAction;
        private SkeletalInput _rightHandAnimAction;

        public void Initialize()
        {
            _leftHandAnimAction = new SkeletalInput("/actions/customavatars/in/lefthandanim");
            _rightHandAnimAction = new SkeletalInput("/actions/customavatars/in/righthandanim");
        }

        public bool TryGetFingerCurl(DeviceUse use, out FingerCurl curl)
        {
            SkeletalInput handAnim = use switch
            {
                DeviceUse.LeftHand => _leftHandAnimAction,
                DeviceUse.RightHand => _rightHandAnimAction,
                _ => throw new InvalidOperationException($"{nameof(TryGetFingerCurl)} only supports {nameof(DeviceUse.LeftHand)} and {nameof(DeviceUse.RightHand)}"),
            };

            if (handAnim == null || !handAnim.isActive || handAnim.summaryData == null)
            {
                curl = null;
                return false;
            }

            curl = new FingerCurl(handAnim.summaryData.thumbCurl, handAnim.summaryData.indexCurl, handAnim.summaryData.middleCurl, handAnim.summaryData.ringCurl, handAnim.summaryData.littleCurl);
            return true;
        }

        public void Dispose()
        {
            _leftHandAnimAction?.Dispose();
            _rightHandAnimAction?.Dispose();
        }
    }
}
