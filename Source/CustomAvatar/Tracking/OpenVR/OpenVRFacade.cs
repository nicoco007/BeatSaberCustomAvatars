//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Text;
using Valve.VR;

namespace CustomAvatar.Tracking.OpenVR
{
    using OpenVR = Valve.VR.OpenVR;

    internal class OpenVRFacade
    {
        public string[] GetTrackedDeviceSerialNumbers()
        {
            string[] serialNumbers = new string[OpenVR.k_unMaxTrackedDeviceCount];

            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                serialNumbers[i] = GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_SerialNumber_String);
            }

            return serialNumbers;
        }

        public int GetInt32TrackedDeviceProperty(uint deviceIndex, ETrackedDeviceProperty property)
        {
            ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
            int result = OpenVR.System.GetInt32TrackedDeviceProperty(deviceIndex, property, ref error);

            if (error != ETrackedPropertyError.TrackedProp_Success)
            {
                throw new OpenVRException($"Failed to get property '{property}' for device at index {deviceIndex}", property, error);
            }

            return result;
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

            if (error != ETrackedPropertyError.TrackedProp_Success && error != ETrackedPropertyError.TrackedProp_BufferTooSmall)
            {
                throw new OpenVRException($"Failed to get property '{property}' for device at index {deviceIndex}: {error}", property, error);
            }

            if (length > 0)
            {
                StringBuilder stringBuilder = new StringBuilder((int)length);
                OpenVR.System.GetStringTrackedDeviceProperty(deviceIndex, property, stringBuilder, length, ref error);

                if (error != ETrackedPropertyError.TrackedProp_Success)
                {
                    throw new OpenVRException($"Failed to get property '{property}' for device at index {deviceIndex}: {error}", property, error);
                }

                return stringBuilder.ToString();
            }

            return null;
        }
    }
}
