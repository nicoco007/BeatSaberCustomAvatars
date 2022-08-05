//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CustomAvatar.Editor
{
    [CustomEditor(typeof(VRIKManager))]
    public class VRIKManagerEditor : UnityEditor.Editor
    {
        private static readonly Regex kRegex = new Regex("(?<!^)(?=[A-Z])");

        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

        public override void OnInspectorGUI()
        {
            string previousSection = null;

            foreach (FieldInfo field in typeof(VRIKManager).GetFields())
            {
                int lastSeparatorIndex = field.Name.LastIndexOf('_');
                string section;
                string fieldName;

                if (lastSeparatorIndex >= 0)
                {
                    fieldName = field.Name.Substring(field.Name.LastIndexOf('_') + 1);
                    section = field.Name.Remove(field.Name.LastIndexOf('_'));
                }
                else
                {
                    fieldName = field.Name;
                    section = null;
                }

                string propertyName = CamelCaseToNatural(fieldName);

                if (previousSection != section && section != null)
                {
                    string[] subSections = section.Split('_');
                    string[] previousSubSections = previousSection?.Split('_') ?? Array.Empty<string>();

                    for (int i = 0; i < subSections.Length; i++)
                    {
                        string subSection = subSections[i];

                        if (previousSubSections.Contains(subSection)) continue;

                        if (!_foldouts.ContainsKey(subSection))
                        {
                            _foldouts.Add(subSection, true);
                        }

                        if (i == 0 || _foldouts[subSections[i - 1]])
                        {
                            EditorGUI.indentLevel = i;
                            _foldouts[subSection] = EditorGUILayout.Foldout(_foldouts[subSection], CamelCaseToNatural(subSection), true);
                        }
                    }
                }

                EditorGUI.indentLevel = section?.Split('_').Length ?? 0;

                if (section == null || AreAllSubSectionsOpen(section))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(field.Name), new GUIContent(propertyName));
                }

                previousSection = section;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private string CamelCaseToNatural(string text)
        {
            return string.Join(" ", kRegex.Split(text).Select(p => p[0].ToString().ToUpper() + p.Substring(1)));
        }

        private bool AreAllSubSectionsOpen(string section)
        {
            foreach (string subSection in section.Split('_'))
            {
                if (!_foldouts[subSection]) return false;
            }

            return true;
        }
    }
}
