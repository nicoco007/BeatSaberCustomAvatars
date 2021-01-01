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

using System.Collections;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace CustomAvatar.Utilities
{
    internal class ShaderLoader : IInitializable
    {
        public bool hasErrors { get; private set; }

        public Shader stereoMirrorShader;
        public Shader unlitShader;

        private readonly ILogger<ShaderLoader> _logger;

        public ShaderLoader(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger<ShaderLoader>();
        }

        public void Initialize()
        {
            SharedCoroutineStarter.instance.StartCoroutine(GetShaders());
        }

        private IEnumerator GetShaders()
        {
            AssetBundleCreateRequest shadersBundleCreateRequest = AssetBundle.LoadFromFileAsync("CustomAvatars/Shaders/customavatars.assetbundle");
            yield return shadersBundleCreateRequest;

            if (!shadersBundleCreateRequest.isDone || !shadersBundleCreateRequest.assetBundle)
            {
                hasErrors = true;
                _logger.Error("Failed to load shaders");
                yield break;
            }

            AssetBundleRequest assetBundleRequest = shadersBundleCreateRequest.assetBundle.LoadAllAssetsAsync<Shader>();
            yield return assetBundleRequest;

            if (!assetBundleRequest.isDone || assetBundleRequest.allAssets.Length == 0)
            {
                hasErrors = true;
                _logger.Error("Failed to load shaders");
                yield break;
            }

            foreach (Object asset in assetBundleRequest.allAssets)
            {
                switch (asset.name)
                {
                    case "BeatSaber/Unlit Glow":
                        unlitShader = asset as Shader;
                        break;

                    case "BeatSaberCustomAvatars/StereoRenderShader":
                        stereoMirrorShader = asset as Shader;
                        break;
                }
            }

            CheckShaderLoaded(unlitShader, "Unlit");
            CheckShaderLoaded(stereoMirrorShader, "Stereo Renderer");

            shadersBundleCreateRequest.assetBundle.Unload(false);
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
                hasErrors = true;
            }
        }
    }
}
