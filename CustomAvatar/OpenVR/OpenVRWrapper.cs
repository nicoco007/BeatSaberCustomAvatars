using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace CustomAvatar.OpenVR
{
	public static class OpenVRWrapper
	{
		public static void SetActionManifestPath(string manifestPath)
		{
			EVRInputError error = OpenVR.Input.SetActionManifestPath(manifestPath);

			if (error != EVRInputError.None)
			{
				throw new Exception(error.ToString());
			}
		}

		public static ulong GetActionSetHandle(string actionSetName)
		{
			ulong handle = default;

			EVRInputError error = OpenVR.Input.GetActionSetHandle(actionSetName, ref handle);

			if (error != EVRInputError.None)
			{
				throw new Exception(error.ToString());
			}

			return handle;
		}

		public static ulong GetActionHandle(string actionName)
		{
			ulong handle = default;

			EVRInputError error = OpenVR.Input.GetActionHandle(actionName, ref handle);

			if (error != EVRInputError.None)
			{
				throw new Exception(error.ToString());
			}

			return handle;
		}

		public static void UpdateActionState(ulong handle)
		{
			VRActiveActionSet_t[] activeActionSets = new VRActiveActionSet_t[1];

			activeActionSets[0] = new VRActiveActionSet_t
			{
				ulActionSet = handle,
				ulRestrictedToDevice = OpenVR.k_ulInvalidInputValueHandle
			};

			EVRInputError error = OpenVR.Input.UpdateActionState(activeActionSets, (uint)Marshal.SizeOf(typeof(VRActiveActionSet_t)));

			if (error != EVRInputError.None)
			{
				throw new Exception(error.ToString());
			}
		}

		public static InputAnalogActionData_t GetAnalogActionData(ulong actionHandle)
		{
			InputAnalogActionData_t actionData = default;

			EVRInputError error = OpenVR.Input.GetAnalogActionData(actionHandle, ref actionData, (uint)Marshal.SizeOf(typeof(InputAnalogActionData_t)), OpenVR.k_ulInvalidInputValueHandle);

			if (error != EVRInputError.None)
			{
				throw new Exception(error.ToString());
			}

			return actionData;
		}

		public static InputDigitalActionData_t GetDigitalActionData(ulong actionHandle)
		{
			InputDigitalActionData_t actionData = default;

			EVRInputError error = OpenVR.Input.GetDigitalActionData(actionHandle, ref actionData, (uint)Marshal.SizeOf(typeof(InputDigitalActionData_t)), OpenVR.k_ulInvalidInputValueHandle);

			if (error != EVRInputError.None)
			{
				throw new Exception(error.ToString());
			}

			return actionData;
		}

		public static VRSkeletalSummaryData_t GetSkeletalSummaryData(ulong actionHandle, EVRSummaryType summaryType = EVRSummaryType.FromDevice)
		{
			VRSkeletalSummaryData_t summaryData = default;

			EVRInputError error = OpenVR.Input.GetSkeletalSummaryData(actionHandle, summaryType, ref summaryData);

			if (error != EVRInputError.None)
			{
				throw new Exception(error.ToString());
			}

			return summaryData;
		}

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

		public static void TriggerHapticVibrationAction(ulong actionHandle, float startSecondsFromNow, float durationSeconds, float frequency, float amplitude)
		{
			EVRInputError error = OpenVR.Input.TriggerHapticVibrationAction(actionHandle, startSecondsFromNow, durationSeconds, frequency, amplitude, OpenVR.k_ulInvalidInputValueHandle);

			if (error != EVRInputError.None)
			{
				throw new Exception(error.ToString());
			}
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
