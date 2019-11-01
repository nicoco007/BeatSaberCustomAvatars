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

		internal static TrackedDeviceType GetTrackedDeviceType(uint deviceIndex)
		{
			string name = GetStringTrackedDeviceProperty(deviceIndex, ETrackedDeviceProperty.Prop_ControllerType_String);

			FieldInfo field = typeof(TrackedDeviceType).GetFields().FirstOrDefault(f => f.GetCustomAttribute<TrackedDeviceTypeAttribute>()?.Name == name);

			if (field == null)
			{
				return TrackedDeviceType.Unknown;
			}

			return (TrackedDeviceType)field.GetValue(null);
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
