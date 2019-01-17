namespace CustomAvatar
{
	public interface IAvatarInput
	{
		PosRot HeadPosRot { get; }
		PosRot LeftPosRot { get; }
		PosRot RightPosRot { get; }
	}
}
