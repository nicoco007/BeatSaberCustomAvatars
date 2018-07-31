using System;
using System.IO;
using System.Linq;

namespace CustomAvatar
{
	public static class AvatarsLoading
	{
		private static readonly string[] AvatarExtensions =
		{
			".avatar"
		};
		
		public static void GetAvatarsInPath(string path, Action<FileBrowserItem[]> callback)
		{
			FileBrowserModel.GetContentOfDirectory(path, AvatarExtensions, callback);
		}
	}
}