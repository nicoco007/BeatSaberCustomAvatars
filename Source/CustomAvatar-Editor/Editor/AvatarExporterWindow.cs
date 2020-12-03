//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
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

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        private void OnFocus()
        {
		    avatars = FindObjectsOfType<AvatarDescriptor>();
            Repaint();
        }

        void OnGUI()
        {
            GUIStyle titleLabelStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 16
            };

            GUIStyle textureStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.UpperRight
            };
        
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

        #pragma warning restore IDE0051
        #endregion

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

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(tempFolder, new[] { assetBundleBuild }, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

            // switch back to what it was before creating the asset bundle
            EditorUserBuildSettings.SwitchActiveBuildTarget(selectedBuildTargetGroup, activeBuildTarget);

            if (manifest == null)
            {
                EditorUtility.DisplayDialog("Export Failed", "Failed to create asset bundle! Please check the Unity console for more information.", "OK");
                return;
            }

            File.Copy(tempAssetBundlePath, destinationPath, true);
            
            AssetDatabase.DeleteAsset(prefabPath);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Export Successful!", $"{avatar.name} was exported successfully!", "OK");
        }
    }
}
