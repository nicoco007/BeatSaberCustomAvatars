using System;
using UnityEngine;

namespace CustomAvatar
{
	public class AvatarAssetBundle
	{
		private AssetBundleCreateRequest _assetBundleRequest;

		public string FullPath { get; }
		public AvatarPrefab AvatarPrefab { get; private set; }
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
			if (!_assetBundleRequest.isDone)
			{
				loadedCallback(AvatarLoadResult.Failed);
				return;
			}
			
			AssetBundle = _assetBundleRequest.assetBundle;
			AvatarPrefab = new AvatarPrefab(AssetBundle, GameObjectLoaded);

			void GameObjectLoaded(GameObject gameObject)
			{
				if (gameObject == null)
				{
					loadedCallback(AvatarLoadResult.Invalid);
				}
				
				loadedCallback(AvatarLoadResult.Completed);
			}
		}
	}
}
