using System;
using System.Collections.Generic;

namespace CustomAvatar
{
	public class AvatarsManager
	{
		private readonly List<CustomAvatar> _avatars = new List<CustomAvatar>();
		private readonly PlayerAvatarInput _playerAvatarInput = new PlayerAvatarInput();
		private CustomAvatar _currentAvatar;

		public IEnumerable<IAvatar> Avatars
		{
			get { return _avatars; }
		}

		public CustomAvatarSpawner Spawner { get; }

		private CustomAvatar CurrentAvatar
		{
			get { return _currentAvatar; }
			set
			{
				_currentAvatar = value;
				_currentAvatar.Load(CustomAvatarLoaded);
			}
		}
		
		public AvatarsManager(string customAvatarsPath)
		{
			Spawner = new CustomAvatarSpawner();
			AvatarsLoading.GetAvatarsInPath(customAvatarsPath, AvatarPathsLoaded);
			
			void AvatarPathsLoaded(FileBrowserItem[] items)
			{
				foreach (var item in items)
				{
					if (item.isDirectory) continue;
					var newAvatar = new CustomAvatar(item.fullPath);
					_avatars.Add(newAvatar);
				}

				Console.WriteLine("Found " + _avatars.Count);
				if (_avatars.Count == 0) return;
				CurrentAvatar = _avatars[0];
			}
		}

		public IAvatar GetCurrentAvatar()
		{
			return CurrentAvatar;
		}

		public IAvatar SwitchToNextAvatar()
		{
			var currentIndex = _avatars.IndexOf(CurrentAvatar);
			if (currentIndex < 0) currentIndex = 0;

			var nextIndex = currentIndex + 1;
			if (nextIndex >= _avatars.Count)
			{
				nextIndex = 0;
			}

			var nextAvatar = _avatars[nextIndex];
			CurrentAvatar = nextAvatar;
			return nextAvatar;
		}

		public IAvatar SwitchToPreviousAvatar()
		{
			var currentIndex = _avatars.IndexOf(CurrentAvatar);
			if (currentIndex < 0) currentIndex = 0;

			var nextIndex = currentIndex - 1;
			if (nextIndex < 0)
			{
				nextIndex = _avatars.Count - 1;
			}

			var nextAvatar = _avatars[nextIndex];
			CurrentAvatar = nextAvatar;
			return nextAvatar;
		}

		private void CustomAvatarLoaded(CustomAvatar loadedAvatar, AvatarLoadResult result)
		{
			if (result != AvatarLoadResult.Completed)
			{
				Plugin.Log("Avatar " + loadedAvatar.FullPath + " failed to load");
				return;
			}

			Spawner.SpawnAvatar(_playerAvatarInput, loadedAvatar);
		}
	}
}