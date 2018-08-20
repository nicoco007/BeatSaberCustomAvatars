using System;
using System.Collections.Generic;
using System.IO;

namespace CustomAvatar
{
	public class AvatarLoader
	{
		private static readonly string[] AvatarExtensions =
		{
			".avatar"
		};
		
		private readonly List<CustomAvatar> _avatars = new List<CustomAvatar>();

		public IReadOnlyList<CustomAvatar> Avatars
		{
			get { return _avatars; }
		}
		
		public AvatarLoader(string customAvatarsPath, Action<IReadOnlyList<CustomAvatar>> loadedCallback)
		{
			void AvatarPathsLoaded(FileBrowserItem[] items)
			{
				foreach (var item in items)
				{
					if (item.isDirectory) continue;
					var newAvatar = new CustomAvatar(item.fullPath);
					_avatars.Add(newAvatar);
				}

				loadedCallback(_avatars.ToArray());
			}

			GetAvatarsInPath(customAvatarsPath, AvatarPathsLoaded);
		}

		public int IndexOf(CustomAvatar customAvatar)
		{
			return _avatars.IndexOf(customAvatar);
		}
		
		private static void GetAvatarsInPath(string path, Action<FileBrowserItem[]> callback)
		{
			FileBrowserModel.GetContentOfDirectory(path, AvatarExtensions, callback);
		}
	}
}