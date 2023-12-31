//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using Zenject;
using Object = UnityEngine.Object;

namespace CustomAvatar.Utilities
{
    internal class AssetLoader : IInitializable, IDisposable
    {
        private readonly ILogger<AssetLoader> _logger;

        internal AssetLoader(ILogger<AssetLoader> logger)
        {
            _logger = logger;
        }

        internal Shader stereoMirrorShader { get; private set; }

        internal Shader unlitShader { get; private set; }

        public void Initialize()
        {
            LoadAssetsAsync().ContinueWith((task) => _logger.LogCritical(task.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Dispose()
        {
            Object.Destroy(stereoMirrorShader);
            Object.Destroy(unlitShader);
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

                    default:
                        _logger.LogError($"Unexpected asset '{name}'");
                        break;
                }
            }

            await assetBundle.UnloadAsync(false);
        }
    }
}
