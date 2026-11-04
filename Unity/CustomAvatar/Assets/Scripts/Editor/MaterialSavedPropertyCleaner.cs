//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MaterialSavedPropertyCleaner
{
    private const string AssetMenuPath = "Assets/Remove Unused Material Properties";
    private const string ContextMenuPath = "CONTEXT/Material/Remove Unused Material Properties";

    [MenuItem(AssetMenuPath, true)]
    private static bool ValidateRemoveUnusedMaterialProperties()
    {
        return Selection.GetFiltered<Material>(SelectionMode.Assets).Length > 0;
    }

    [MenuItem(AssetMenuPath, false, 2000)]
    private static void RemoveUnusedMaterialProperties()
    {
        RemoveUnusedMaterialProperties(Selection.GetFiltered<Material>(SelectionMode.Assets));
    }

    [MenuItem(ContextMenuPath, true)]
    private static bool ValidateRemoveUnusedMaterialPropertiesFromContext(MenuCommand command)
    {
        return command.context is Material material && material.shader != null;
    }

    [MenuItem(ContextMenuPath)]
    private static void RemoveUnusedMaterialPropertiesFromContext(MenuCommand command)
    {
        RemoveUnusedMaterialProperties(new[] { command.context as Material });
    }

    private static void RemoveUnusedMaterialProperties(Material[] materials)
    {
        int changedCount = 0;
        int removedPropertyCount = 0;

        foreach (Material material in materials)
        {
            if (material == null || material.shader == null)
            {
                continue;
            }

            int removedFromMaterial = RemoveUnusedSavedProperties(material);
            if (removedFromMaterial <= 0)
            {
                continue;
            }

            changedCount++;
            removedPropertyCount += removedFromMaterial;
        }

        string message = changedCount > 0
            ? $"Removed {removedPropertyCount} unused saved propert{(removedPropertyCount == 1 ? "y" : "ies")} from {changedCount} material{(changedCount == 1 ? string.Empty : "s")}."
            : "No unused saved material properties were found.";

        EditorUtility.DisplayDialog("Remove Unused Material Properties", message, "OK");
    }

    private static int RemoveUnusedSavedProperties(Material material)
    {
        HashSet<string> shaderPropertyNames = GetShaderPropertyNames(material.shader);
        SerializedObject serializedMaterial = new SerializedObject(material);
        SerializedProperty savedProperties = serializedMaterial.FindProperty("m_SavedProperties");

        if (savedProperties == null)
        {
            return 0;
        }

        int removedCount = 0;
        removedCount += RemoveUnusedEntries(savedProperties.FindPropertyRelative("m_TexEnvs"), shaderPropertyNames);
        removedCount += RemoveUnusedEntries(savedProperties.FindPropertyRelative("m_Floats"), shaderPropertyNames);
        removedCount += RemoveUnusedEntries(savedProperties.FindPropertyRelative("m_Colors"), shaderPropertyNames);

        SerializedProperty ints = savedProperties.FindPropertyRelative("m_Ints");
        if (ints != null)
        {
            removedCount += RemoveUnusedEntries(ints, shaderPropertyNames);
        }

        if (removedCount <= 0)
        {
            return 0;
        }

        Undo.RegisterCompleteObjectUndo(material, "Remove Unused Material Properties");
        serializedMaterial.ApplyModifiedProperties();
        EditorUtility.SetDirty(material);
        return removedCount;
    }

    private static HashSet<string> GetShaderPropertyNames(Shader shader)
    {
        int propertyCount = ShaderUtil.GetPropertyCount(shader);
        HashSet<string> shaderPropertyNames = new HashSet<string>();

        for (int index = 0; index < propertyCount; index++)
        {
            shaderPropertyNames.Add(ShaderUtil.GetPropertyName(shader, index));
        }

        return shaderPropertyNames;
    }

    private static int RemoveUnusedEntries(SerializedProperty propertyArray, HashSet<string> shaderPropertyNames)
    {
        if (propertyArray == null || !propertyArray.isArray)
        {
            return 0;
        }

        int removedCount = 0;
        for (int index = propertyArray.arraySize - 1; index >= 0; index--)
        {
            SerializedProperty entry = propertyArray.GetArrayElementAtIndex(index);
            SerializedProperty propertyName = entry.FindPropertyRelative("first");

            if (propertyName == null || shaderPropertyNames.Contains(propertyName.stringValue))
            {
                continue;
            }

            propertyArray.DeleteArrayElementAtIndex(index);
            removedCount++;
        }

        return removedCount;
    }
}