using CustomAvatar.OpenVR;
using CustomAvatar.OpenVR.Input;
using System;
using System.IO;
using UnityEngine;

namespace CustomAvatar
{
	internal class OpenVRInputManager : MonoBehaviour
	{
		public VectorInput LeftTrigger { get; private set; }
		public VectorInput RightTrigger { get; private set; }
		public ButtonInput Menu { get; private set; }
		public VibrationOutput LeftSlice { get; set; }
		public VibrationOutput RightSlice { get; set; }

		public InputDigitalActionData_t Up => OpenVRWrapper.GetDigitalActionData(upHandle);
		public InputDigitalActionData_t Down => OpenVRWrapper.GetDigitalActionData(downHandle);
		public InputDigitalActionData_t Reset => OpenVRWrapper.GetDigitalActionData(resetHandle);

		public VRSkeletalSummaryData_t LeftHandAnim => OpenVRWrapper.GetSkeletalSummaryData(leftHandAnimHandle);
		public VRSkeletalSummaryData_t RightHandAnim => OpenVRWrapper.GetSkeletalSummaryData(rightHandAnimHandle);

		private ulong actionSetHandle;
		private ulong leftHandAnimHandle;
		private ulong rightHandAnimHandle;

		private ulong upHandle;
		private ulong downHandle;
		private ulong resetHandle;

		public void Awake()
		{
			OpenVRWrapper.SetActionManifestPath(Path.Combine(Environment.CurrentDirectory, "CustomAvatars", "Input", "actions.json"));

			actionSetHandle = OpenVRWrapper.GetActionSetHandle("/actions/main");

			LeftTrigger    = new VectorInput("/actions/main/in/LeftTriggerValue");
			RightTrigger   = new VectorInput("/actions/main/in/RightTriggerValue");
			Menu           = new ButtonInput("/actions/main/in/Menu");
			LeftSlice      = new VibrationOutput("/actions/main/out/LeftSlice");
			RightSlice     = new VibrationOutput("/actions/main/out/RightSlice");

			leftHandAnimHandle = OpenVRWrapper.GetActionHandle("/actions/main/in/LeftHandAnim");
			rightHandAnimHandle = OpenVRWrapper.GetActionHandle("/actions/main/in/RightHandAnim");

			upHandle = OpenVRWrapper.GetActionHandle("/actions/main/in/Up");
			downHandle = OpenVRWrapper.GetActionHandle("/actions/main/in/Down");
			resetHandle = OpenVRWrapper.GetActionHandle("/actions/main/in/Reset");
		}

		public void Update()
		{
			OpenVRWrapper.UpdateActionState(actionSetHandle);
		}
	}
}
