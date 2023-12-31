using System.IO;
using UnityEditor;
using UnityEngine;

public class ExportCustomAvatarsAssetBundle
{
    [MenuItem("Assets/Export Custom Avatars Asset Bundle", priority = 1100)]
    public static void BuildAssetBundle()
    {
        string resourcesPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "Source", "CustomAvatar", "Resources"));
        string targetPath = EditorUtility.SaveFilePanel("Export Custom Avatars Asset Bundle", resourcesPath, "Assets", string.Empty);

        if (string.IsNullOrEmpty(targetPath))
        {
            return;
        }

        AssetBundleBuild assetBundleBuild = new()
        {
            assetBundleName = Path.GetFileName(targetPath),
            assetNames = new[] {
                "Assets/Shaders/StereoRender.shader",
                "Assets/Shaders/UnlitOverlay.shader",
                "Assets/Sprites/UI.spriteatlasv2",
            },
        };

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new AssetBundleBuild[] { assetBundleBuild }, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);

        if (manifest == null)
        {
            EditorUtility.DisplayDialog("Failed to build asset bundle!", "Failed to build asset bundle! Check the console for details.", "OK");
            return;
        }

        string fileName = manifest.GetAllAssetBundles()[0];
        File.Copy(Path.Combine(Application.temporaryCachePath, fileName), targetPath, true);

        EditorUtility.DisplayDialog("Export Successful!", "Asset bundle exported successfully!", "OK");
    }
}
