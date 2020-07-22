using System.Collections;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
    internal class ShaderLoader : MonoBehaviour
    {
        public Shader stereoMirrorShader;
        public Shader unlitShader;

        private ILogger<ShaderLoader> _logger;

        [Inject]
        private void Inject(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger<ShaderLoader>();
        }

        private void Start()
        {
            StartCoroutine(GetShaders());
        }

        private IEnumerator GetShaders()
        {
            AssetBundleCreateRequest shadersBundleCreateRequest = AssetBundle.LoadFromFileAsync("CustomAvatars/Shaders/customavatars.assetbundle");
            yield return shadersBundleCreateRequest;

            if (!shadersBundleCreateRequest.isDone || !shadersBundleCreateRequest.assetBundle)
            {
                _logger.Error("Failed to load shaders");
                yield break;
            }

            AssetBundleRequest assetBundleRequest = shadersBundleCreateRequest.assetBundle.LoadAllAssetsAsync<Shader>();
            yield return assetBundleRequest;

            if (!assetBundleRequest.isDone || assetBundleRequest.allAssets.Length == 0)
            {
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
                    
                    case "Custom/StereoRenderShader-Unlit":
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
            }
        }
    }
}
