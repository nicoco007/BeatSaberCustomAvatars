//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CustomAvatar.Logging;
using UnityEngine;
using UnityEngine.U2D;
using Zenject;
using Object = UnityEngine.Object;

namespace CustomAvatar.Utilities
{
    internal class AssetLoader : IInitializable, IDisposable
    {
        private struct VoidResult { }

        private readonly ILogger<AssetLoader> _logger;
        private readonly TaskCompletionSource<VoidResult> _taskCompletionSource = new();

        internal AssetLoader(ILogger<AssetLoader> logger)
        {
            _logger = logger;
        }

        internal Shader stereoMirrorShader { get; private set; }

        internal Shader unlitShader { get; private set; }

        internal SpriteAtlas uiSpriteAtlas { get; private set; }

        public Task WaitForAssetsLoadedAsync() => _taskCompletionSource.Task;

        public async void Initialize()
        {
            try
            {
                await LoadAssetsAsync();
                _taskCompletionSource.SetResult(default);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load assets\n{ex}");
                _taskCompletionSource.SetException(ex);
            }
        }

        public void Dispose()
        {
            Object.Destroy(stereoMirrorShader);
            Object.Destroy(unlitShader);
            Object.Destroy(uiSpriteAtlas);
        }

        private async Task LoadAssetsAsync()
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CustomAvatar.Resources.Assets");
            AssetBundleCreateRequest assetBundleCreateRequest = await AssetBundle.LoadFromStreamAsync(stream);
            AssetBundle assetBundle = assetBundleCreateRequest.assetBundle;

            if (assetBundle == null)
            {
                _logger.LogError("Failed to load asset bundle");
                return;
            }

            AssetBundleRequest assetsRequest = await assetBundle.LoadAllAssetsAsync();
            Object[] assets = assetsRequest.allAssets;
            string[] assetNames = assetBundle.GetAllAssetNames();

            for (int i = 0; i < assets.Length; i++)
            {
                Object asset = assets[i];
                string name = assetNames[i];

                switch (name)
                {
                    case "assets/shaders/stereorender.shader":
                        stereoMirrorShader = (Shader)asset;
                        break;

                    case "assets/shaders/unlitoverlay.shader":
                        unlitShader = (Shader)asset;
                        break;

                    case "assets/sprites/ui.spriteatlasv2":
                        uiSpriteAtlas = (SpriteAtlas)asset;
                        break;

                    default:
                        _logger.LogError($"Unexpected asset '{name}'");
                        break;
                }
            }

            await assetBundle.UnloadAsync(false);
        }
    }
}
