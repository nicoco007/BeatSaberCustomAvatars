using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomAvatar
{
    internal class ShaderLoader : MonoBehaviour
    {
        public static Shader stereoMirrorShader;
        public static Shader unlitShader;

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
                Plugin.logger.Error("Failed to load shaders");
                yield break;
            }

            AssetBundleRequest assetBundleRequest = shadersBundleCreateRequest.assetBundle.LoadAllAssetsAsync<Shader>();
            yield return assetBundleRequest;

            if (!assetBundleRequest.isDone || assetBundleRequest.allAssets.Length == 0)
            {
                Plugin.logger.Error("Failed to load shaders");
                yield break;
            }

            foreach (Object asset in assetBundleRequest.allAssets)
            {
                switch (asset.name)
                {
                    case "BeatSaber/Unlit Glow":
                        unlitShader = asset as Shader;
                        Plugin.logger.Info("Loaded unlit shader");
                        break;
                    
                    case "Custom/StereoRenderShader-Unlit":
                        stereoMirrorShader = asset as Shader;
                        Plugin.logger.Info("Loaded stereo render shader");
                        break;
                }
            }

            shadersBundleCreateRequest.assetBundle.Unload(false);
        }
    }
}
