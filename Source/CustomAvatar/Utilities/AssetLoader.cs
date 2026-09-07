//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using Object = UnityEngine.Object;

namespace CustomAvatar.Utilities
{
    internal class AssetLoader : IDisposable
    {
        private struct VoidResult { }

        private readonly ILogger<AssetLoader> _logger;

        protected AssetLoader(ILogger<AssetLoader> logger)
        {
            _logger = logger;
        }

        internal GameObject playerAvatarManager { get; private set; }

        internal GameObject trackingRig { get; private set; }

        internal Shader stereoMirrorShader { get; private set; }

        internal Shader unlitShader { get; private set; }

        internal SpriteAtlas uiSpriteAtlas { get; private set; }

        public void Dispose()
        {
            Object.Destroy(stereoMirrorShader);
            Object.Destroy(unlitShader);
            Object.Destroy(uiSpriteAtlas);
        }

        internal async Task LoadAssetsAsync()
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CustomAvatar.Resources.Assets");
            AssetBundleCreateRequest assetBundleCreateRequest = await AssetBundle.LoadFromStreamAsync(stream);
            AssetBundle assetBundle = assetBundleCreateRequest.assetBundle;

            if (assetBundle == null)
            {
                _logger.LogError("Failed to load asset bundle");
                return;
            }

            try
            {
                AssetBundleRequest assetsRequest = await assetBundle.LoadAllAssetsAsync();

                if (assetsRequest.allAssets == null || assetsRequest.allAssets.Length == 0)
                {
                    _logger.LogError("Failed to load assets");
                    return;
                }

                // since we called LoadAllAssetsAsync these are nearly instant lookups
                playerAvatarManager = assetBundle.LoadAsset<GameObject>("Assets/Prefabs/PlayerAvatarManager.prefab");
                trackingRig = assetBundle.LoadAsset<GameObject>("Assets/Prefabs/TrackingRig.prefab");
                stereoMirrorShader = assetBundle.LoadAsset<Shader>("Assets/Shaders/StereoRender.shader");
                unlitShader = assetBundle.LoadAsset<Shader>("Assets/Shaders/UnlitOverlay.shader");
                uiSpriteAtlas = assetBundle.LoadAsset<SpriteAtlas>("Assets/Sprites/UI.spriteatlasv2");
            }
            finally
            {
                await assetBundle.UnloadAsync(false);
            }
        }
    }
}
