using System;
using UnityEngine;

namespace CustomAvatar
{
	public class AvatarAssetBundle
	{
		private AssetBundleCreateRequest _assetBundleRequest;

		public string FullPath { get; }
		public AvatarGameObject AvatarGameObject { get; private set; }
		public AssetBundle AssetBundle { get; private set; }
		
		public bool IsLoaded
		{
			get { return AssetBundle != null; }
		}
		
		public AvatarAssetBundle(string fullPath)
		{
			FullPath = fullPath;
		}
		
		public void LoadAssetBundle(Action<AvatarLoadResult> loadedCallback)
		{
			if (IsLoaded)
			{
				loadedCallback(AvatarLoadResult.Completed);
				return;
			}
			
			void Completed(AsyncOperation asyncOperation)
			{
				AssetBundleLoaded(loadedCallback);
			}

			Console.WriteLine("Loading the asset bundle for " + FullPath);
			_assetBundleRequest = AssetBundle.LoadFromFileAsync(FullPath);
			_assetBundleRequest.completed += Completed;
		}

		/*public void UnloadAssetBundle()
		{
			if (!IsLoaded) return;
			AssetBundle.Unload(true);
		}*/

		private void AssetBundleLoaded(Action<AvatarLoadResult> loadedCallback)
		{
			Console.WriteLine("Loaded");
			if (!_assetBundleRequest.isDone)
			{
				loadedCallback(AvatarLoadResult.Failed);
				return;
			}
			
			Console.WriteLine("Everything is cool so far");
			AssetBundle = _assetBundleRequest.assetBundle;
			AvatarGameObject = new AvatarGameObject(AssetBundle);
			
			if (AvatarGameObject.GameObject == null)
			{
				loadedCallback(AvatarLoadResult.Invalid);
			}
			
			Console.WriteLine("It should have worked...");
			loadedCallback(AvatarLoadResult.Completed);
		}
	}
}