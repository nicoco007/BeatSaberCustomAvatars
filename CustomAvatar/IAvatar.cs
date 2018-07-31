namespace CustomAvatar
{
	public interface IAvatar
	{
		string Name { get; }
		string AuthorName { get; }
		string FullPath { get; }
		float Height { get; }
		bool AllowHeightCalibration { get; }
	}
}