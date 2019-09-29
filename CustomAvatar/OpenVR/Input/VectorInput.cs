namespace CustomAvatar.OpenVR.Input
{
	class VectorInput : OVRAction
	{
		public VectorInput(string actionName) : base(actionName) { }

		public float GetAxisRaw()
		{
			return OpenVRWrapper.GetAnalogActionData(Handle).x;
		}
	}
}
