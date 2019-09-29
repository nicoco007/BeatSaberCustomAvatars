namespace CustomAvatar.OpenVR.Input
{
	class ButtonInput : OVRAction
	{
		private InputDigitalActionData_t Input => OpenVRWrapper.GetDigitalActionData(Handle);

		public ButtonInput(string actionName) : base(actionName) { }

		public bool GetButton()
		{
			return Input.bState;
		}

		public bool GetButtonDown()
		{
			return Input.bState && Input.bChanged;
		}

		public bool GetButtonUp()
		{
			return !Input.bState && Input.bChanged;
		}
	}
}
