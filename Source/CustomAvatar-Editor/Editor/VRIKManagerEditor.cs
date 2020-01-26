using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CustomAvatar.Editor
{
    [CustomEditor(typeof(VRIKManager))]
    public class VRIKManagerEditor : UnityEditor.Editor
    {
        private static readonly Regex kRegex = new Regex("(?<!^)(?=[A-Z])");

        public override void OnInspectorGUI()
        {
            foreach (var field in typeof(VRIKManager).GetFields())
            {
                string[] parts = field.Name.Split('_');
                string propertyName = string.Join(" ", kRegex.Split(parts.Last()).Select(p => p.First().ToString().ToUpper() + p.Substring(1)));

                GUILayout.BeginHorizontal();

                GUILayout.Space((parts.Length - 1) * 10);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field.Name), new GUIContent(propertyName));

                GUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
