//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Reflection;
using System.Threading.Tasks;
using CustomAvatar.Exceptions;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace CustomAvatar.Utilities
{
    internal class ShaderLoader : IInitializable
    {
        public Shader stereoMirrorShader { get; private set; }
        public Shader unlitShader { get; private set; }

        private readonly ILogger<ShaderLoader> _logger;

        public ShaderLoader(ILogger<ShaderLoader> logger)
        {
            _logger = logger;
        }

        public async void Initialize()
        {
            try
            {
                await LoadShaders();

                CheckShaderLoaded(unlitShader, "Glow Overlay");
                CheckShaderLoaded(stereoMirrorShader, "Stereo Render");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load shaders");
                _logger.Error(ex);
            }
        }

        private async Task LoadShaders()
        {
            AssetBundleCreateRequest shadersBundleCreateRequest = await AssetBundle.LoadFromStreamAsync(Assembly.GetExecutingAssembly().GetManifestResourceStream("CustomAvatar.Resources.shaders.assets"));
            AssetBundle assetBundle = shadersBundleCreateRequest.assetBundle;

            if (!assetBundle)
            {
                throw new ShaderLoadException("Failed to load asset bundle");
            }

            AssetBundleRequest assetBundleRequest = await assetBundle.LoadAllAssetsAsync<Shader>();

            if (assetBundleRequest.allAssets.Length == 0)
            {
                assetBundle.Unload(true);
                throw new ShaderLoadException("No assets found");
            }

            foreach (Object asset in assetBundleRequest.allAssets)
            {
                switch (asset.name)
                {
                    case "Beat Saber Custom Avatars/Glow Overlay":
                        unlitShader = (Shader)asset;
                        break;

                    case "Beat Saber Custom Avatars/Stereo Render":
                        stereoMirrorShader = (Shader)asset;
                        break;
                }
            }

            assetBundle.Unload(false);
        }

        private void CheckShaderLoaded(Shader shader, string name)
        {
            if (shader)
            {
                _logger.Info($"{name} shader loaded");
            }
            else
            {
                _logger.Error($"{name} shader not found");
            }
        }
    }
}
