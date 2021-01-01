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
