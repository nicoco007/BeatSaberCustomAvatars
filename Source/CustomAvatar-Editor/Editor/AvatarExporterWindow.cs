using System.IO;
using UnityEditor;
using UnityEngine;

namespace CustomAvatar.Editor
{
    public class AvatarExporterWindow : EditorWindow
    {
        private AvatarDescriptor[] avatars;
    
        [MenuItem("Window/Avatar Exporter")]
        public static void ShowWindow()
        {
		    GetWindow(typeof(AvatarExporterWindow), false, "Avatar Exporter");
        }
        
        private void OnFocus()
        {
		    avatars = FindObjectsOfType<AvatarDescriptor>();
            Repaint();
        }

        void OnGUI()
        {
            GUIStyle titleLabelStyle = new GUIStyle(EditorStyles.largeLabel);
            titleLabelStyle.fontSize = 16;

            GUIStyle textureStyle = new GUIStyle(EditorStyles.label);
            textureStyle.alignment = TextAnchor.UpperRight;
        
		    foreach (AvatarDescriptor avatar in avatars)
            {
                if (!avatar || !avatar.gameObject) continue;

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();

			    GUILayout.Label(avatar.name, titleLabelStyle);

                GUILayout.Label("Properties", EditorStyles.largeLabel);
                
			    EditorGUILayout.LabelField("Game Object: ", avatar.gameObject.name);
			    EditorGUILayout.LabelField("Author: ", avatar.author);

                GUILayout.EndVertical();

                var texture = AssetPreview.GetAssetPreview(avatar.cover);
                GUILayout.Label(texture, textureStyle, GUILayout.MaxWidth(80), GUILayout.MaxHeight(80));

                GUILayout.EndHorizontal();

                if (GUILayout.Button("Export " + avatar.name))
                {
                    SaveAvatar(avatar);
                }

			    GUILayout.Space(20);
		    }
        }

        private void SaveAvatar(AvatarDescriptor avatar)
        {
            string destinationPath = EditorUtility.SaveFilePanel("Save avatar file", null, avatar.name + ".avatar", "avatar");

            if (string.IsNullOrEmpty(destinationPath)) return;

            string destinationFileName = Path.GetFileName(destinationPath);
            string tempFolder = Application.temporaryCachePath;
            string tempAssetBundlePath = Path.Combine(Application.temporaryCachePath, destinationFileName);
            string prefabPath = Path.Combine("Assets", "_CustomAvatar.prefab");

            PrefabUtility.SaveAsPrefabAsset(avatar.gameObject, prefabPath);

            AssetBundleBuild assetBundleBuild = new AssetBundleBuild
            {
                assetBundleName = destinationFileName,
                assetNames = new[] { prefabPath }
            };

            assetBundleBuild.assetBundleName = destinationFileName;

            BuildTargetGroup selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            BuildPipeline.BuildAssetBundles(tempFolder, new[] { assetBundleBuild }, 0, EditorUserBuildSettings.activeBuildTarget);

            EditorUserBuildSettings.SwitchActiveBuildTarget(selectedBuildTargetGroup, activeBuildTarget);

            File.Copy(tempAssetBundlePath, destinationPath, true);
            
            AssetDatabase.DeleteAsset(prefabPath);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Export Successful!", $"{avatar.name} was exported successfully!", "OK");
        }
    }
}
