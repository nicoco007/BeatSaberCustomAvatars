using System;
using System.Collections.Generic;
using System.IO;

namespace CustomAvatar
{
#if BS_0_11_2
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
#elif BS_0_11_1
	public class AvatarLoader
	{	
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
					if (Directory.Exists(item.fullPath)) continue;
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
			AvatarFileBrowserModel.Instance.GetContentOfDirectory(path, callback);
		}
		
		public class AvatarFileBrowserModel : FileBrowserModel
		{
			public static readonly AvatarFileBrowserModel Instance = new AvatarFileBrowserModel();
	
			protected override FileBrowserItem[] GetContentOfDirectory(string path)
			{
				var list = new List<FileBrowserItem>();
				var path2 = path + "\\..";
				if (Path.GetFullPath(path2) != Path.GetFullPath(path))
				{
					list.Add(new DirectoryBrowserItem("..", Path.GetFullPath(path2)));
				}
				if (!CanOpenDirectory(path))
				{
					return list.ToArray();
				}
				path = Path.GetFullPath(path);
				var directories = Directory.GetDirectories(path);
				foreach (var path3 in directories)
				{
					list.Add(new DirectoryBrowserItem(Path.GetFileName(path3), Path.GetFullPath(path3)));
				}
				var files = Directory.GetFiles(path);
				foreach (var path4 in files)
				{
					var a = Path.GetExtension(path4).ToLower();
					if (a == ".avatar")
					{
						list.Add(new FileBrowserItem(Path.GetFileName(path4), Path.GetFullPath(path4)));
					}
				}
				return list.ToArray();
			}

			private static bool CanOpenDirectory(string path)
			{
				bool result;
				try
				{
					Directory.GetDirectories(path);
					result = true;
				}
				catch
				{
					result = false;
				}
				return result;
			}
		}
	}
#endif
}