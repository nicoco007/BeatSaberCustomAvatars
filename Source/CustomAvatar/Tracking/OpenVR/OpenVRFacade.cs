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

using System.Text;
using UnityEngine;
using Valve.VR;

namespace CustomAvatar.Tracking.OpenVR
{
#pragma warning disable IDE0065
    using OpenVR = Valve.VR.OpenVR;
#pragma warning restore IDE0065

    internal class OpenVRFacade
    {
        public const uint kMaxTrackedDeviceCount = OpenVR.k_unMaxTrackedDeviceCount;

        public ETrackedDeviceClass GetTrackedDeviceClass(uint deviceIndex)
        {
            return OpenVR.System.GetTrackedDeviceClass(deviceIndex);
        }

        public float GetFloatTrackedDeviceProperty(uint deviceIndex, ETrackedDeviceProperty property)
        {
            ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
            float result = OpenVR.System.GetFloatTrackedDeviceProperty(deviceIndex, property, ref error);

            if (error != ETrackedPropertyError.TrackedProp_Success)
            {
                throw new OpenVRException($"Failed to get property '{property}' for device at index {deviceIndex}", property, error);
            }

            return result;
        }

        public string GetStringTrackedDeviceProperty(uint deviceIndex, ETrackedDeviceProperty property)
        {
            ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
            uint length = OpenVR.System.GetStringTrackedDeviceProperty(deviceIndex, property, null, 0, ref error);

            if (error == ETrackedPropertyError.TrackedProp_UnknownProperty)
            {
                return null;
            }

            if (error != ETrackedPropertyError.TrackedProp_Success && error != ETrackedPropertyError.TrackedProp_BufferTooSmall)
            {
                throw new OpenVRException($"Failed to get property '{property}' for device at index {deviceIndex}: {error}", property, error);
            }

            if (length > 0)
            {
                var stringBuilder = new StringBuilder((int)length);
                OpenVR.System.GetStringTrackedDeviceProperty(deviceIndex, property, stringBuilder, length, ref error);

                if (error != ETrackedPropertyError.TrackedProp_Success)
                {
                    throw new OpenVRException($"Failed to get property '{property}' for device at index {deviceIndex}: {error}", property, error);
                }

                return stringBuilder.ToString();
            }

            return null;
        }

        public ETrackedControllerRole GetControllerRoleForTrackedDeviceIndex(uint deviceIndex)
        {
            return OpenVR.System.GetControllerRoleForTrackedDeviceIndex(deviceIndex);
        }

        public void GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin universeOrigin, TrackedDevicePose_t[] poses)
        {
            OpenVR.System.GetDeviceToAbsoluteTrackingPose(universeOrigin, GetPredictedSecondsToPhotons(), poses);
        }

        public void GetPositionAndRotation(HmdMatrix34_t rawMatrix, out Vector3 position, out Quaternion rotation)
        {
            position = new Vector3(rawMatrix.m3, rawMatrix.m7, -rawMatrix.m11);

            if (IsRotationValid(rawMatrix))
            {
                float w = Mathf.Sqrt(Mathf.Max(0, 1 + rawMatrix.m0 + rawMatrix.m5 + rawMatrix.m10)) / 2;
                float x = Mathf.Sqrt(Mathf.Max(0, 1 + rawMatrix.m0 - rawMatrix.m5 - rawMatrix.m10)) / 2;
                float y = Mathf.Sqrt(Mathf.Max(0, 1 - rawMatrix.m0 + rawMatrix.m5 - rawMatrix.m10)) / 2;
                float z = Mathf.Sqrt(Mathf.Max(0, 1 - rawMatrix.m0 - rawMatrix.m5 + rawMatrix.m10)) / 2;

                CopySign(ref x, rawMatrix.m6 - rawMatrix.m9);
                CopySign(ref y, rawMatrix.m8 - rawMatrix.m2);
                CopySign(ref z, rawMatrix.m4 - rawMatrix.m1);

                rotation = new Quaternion(x, y, z, w);
            }
            else
            {
                rotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Calculates the number of seconds from now to when the next photons will come out of the HMD. See https://github.com/ValveSoftware/openvr/wiki/IVRSystem::GetDeviceToAbsoluteTrackingPose.
        /// </summary>
        /// <returns>The number of seconds from now to when the next photons will come out of the HMD</returns>
        private float GetPredictedSecondsToPhotons()
        {
            float displayFrequency = GetFloatTrackedDeviceProperty(OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_DisplayFrequency_Float);
            float vsyncToPhotons = GetFloatTrackedDeviceProperty(OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_SecondsFromVsyncToPhotons_Float);

            float secondsSinceLastVsync = 0;
            ulong frameCounter = 0;

            OpenVR.System.GetTimeSinceLastVsync(ref secondsSinceLastVsync, ref frameCounter);

            float frameDuration = 1f / displayFrequency;

            // secondsSinceLastVsync just keeps increasing forever on AMD 7900 XTX GPUs.
            // According to the docs, secondsSinceLastVsync should always be <= frameDuration
            // (see https://github.com/ValveSoftware/openvr/wiki/IVRSystem::GetTimeSinceLastVsync).
            // This seems to be a bug with SteamVR. The Mathf.Max below reduces the impact of this unexpected
            // return value since (frameDuration - secondsSinceLastVsync) is usually quite small anyway.
            return Mathf.Max(frameDuration - secondsSinceLastVsync, 0) + vsyncToPhotons;
        }

        private static void CopySign(ref float sizeVal, float signVal)
        {
            if (signVal > 0 != sizeVal > 0) sizeVal = -sizeVal;
        }

        private bool IsRotationValid(HmdMatrix34_t rawMatrix)
        {
            return (rawMatrix.m2 != 0 || rawMatrix.m6 != 0 || rawMatrix.m10 != 0) && (rawMatrix.m1 != 0 || rawMatrix.m5 != 0 || rawMatrix.m9 != 0);
        }
    }
}
