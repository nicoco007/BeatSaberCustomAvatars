namespace CustomAvatar.OpenVR.Input
{
	class VibrationOutput : OVRAction
	{
		public VibrationOutput(string actionName) : base(actionName) { }

		public void TriggerHapticPulse(float durationSeconds, float amplitude)
		{
			OpenVRWrapper.TriggerHapticVibrationAction(Handle, 0, durationSeconds, 150f, amplitude);
		}
	}
}
