namespace CustomAvatar.OpenVR.Input
{
	class OVRAction
	{
		protected ulong Handle { get; }

		public OVRAction(string actionName)
		{
			Handle = OpenVRWrapper.GetActionHandle(actionName);
		}
	}
}
