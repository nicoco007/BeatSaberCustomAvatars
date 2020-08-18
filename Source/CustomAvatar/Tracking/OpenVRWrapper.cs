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

using System.Linq;
using System.Reflection;
using System.Text;
using Valve.VR;

namespace CustomAvatar.Tracking
{
    internal static class OpenVRWrapper
    {
        internal static string[] GetTrackedDeviceSerialNumbers()
        {
            string[] serialNumbers = new string[OpenVR.k_unMaxTrackedDeviceCount];

            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                serialNumbers[i] = GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_SerialNumber_String);
            }

            return serialNumbers;
        }

        internal static TrackedDeviceRole GetTrackedDeviceRole(uint deviceIndex)
        {
            string name = GetStringTrackedDeviceProperty(deviceIndex, ETrackedDeviceProperty.Prop_ControllerType_String);

            if (name == null)
            {
                return TrackedDeviceRole.Unknown;
            }

            FieldInfo field = typeof(TrackedDeviceRole).GetFields().FirstOrDefault(f => f.GetCustomAttribute<TrackedDeviceTypeAttribute>()?.Name == name);

            if (field == null)
            {
                return TrackedDeviceRole.Unknown;
            }

            return (TrackedDeviceRole)field.GetValue(null);
        }

        internal static string GetStringTrackedDeviceProperty(uint deviceIndex, ETrackedDeviceProperty property)
        {
            ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
            uint length = OpenVR.System.GetStringTrackedDeviceProperty(deviceIndex, property, null, 0, ref error);

            if (length > 0)
            {
                StringBuilder stringBuilder = new StringBuilder((int)length);
                OpenVR.System.GetStringTrackedDeviceProperty(deviceIndex, property, stringBuilder, length, ref error);

                return stringBuilder.ToString();
            }

            return null;
        }
    }
}
