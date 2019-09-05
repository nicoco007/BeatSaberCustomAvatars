using System.Linq;
using System.Reflection;
using System.Text;

namespace CustomAvatar.OpenVR
{
	public static class OpenVRHelper
	{
		public static string[] GetTrackedDeviceSerialNumbers()
		{
			string[] serialNumbers = new string[OpenVR.k_unMaxTrackedDeviceCount];

			for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
			{
				serialNumbers[i] = GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_SerialNumber_String);
			}

			return serialNumbers;
		}

		public static TrackedDeviceType GetTrackedDeviceType(uint deviceIndex)
		{
			string name = GetStringTrackedDeviceProperty(deviceIndex, ETrackedDeviceProperty.Prop_ControllerType_String);

			FieldInfo field = typeof(TrackedDeviceType).GetFields().FirstOrDefault(f => f.GetCustomAttribute<TrackedDeviceTypeAttribute>()?.Name == name);

			if (field == null)
			{
				Plugin.Logger.Warn($"Could not get enum value for " + name);
			}

			return (TrackedDeviceType)field.GetValue(null);
		}

		private static string GetStringTrackedDeviceProperty(uint deviceIndex, ETrackedDeviceProperty property)
		{
			ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
			uint length = OpenVR.System.GetStringTrackedDeviceProperty(deviceIndex, property, null, 0, ref error);

			if (length > 0)
			{
				StringBuilder stringBuilder = new StringBuilder((int)length);
				OpenVR.System.GetStringTrackedDeviceProperty(deviceIndex, property, stringBuilder, length, ref error);

				return stringBuilder.ToString();
			}

			if (error != ETrackedPropertyError.TrackedProp_Success)
			{
				Plugin.Logger.Warn($"Failed to get {property} for tracked device {deviceIndex}: {error}");
			}

			return null;
		}
	}
}
