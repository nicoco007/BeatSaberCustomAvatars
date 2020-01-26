using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CustomAvatar.Editor
{
    [CustomEditor(typeof(PoseManager))]
    public class PoseManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PoseManager poseManager = (PoseManager)target;

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save Open Hands Pose"))
            {
                Animator animator = poseManager.gameObject.GetComponentInChildren<Animator>();
                poseManager.SaveOpenHand(animator);
            }

            if (GUILayout.Button("Apply Open Hands Pose"))
            {
                Undo.RegisterCompleteObjectUndo(poseManager.gameObject, "Apply Open Hands Pose");
                Animator animator = poseManager.gameObject.GetComponentInChildren<Animator>();
                poseManager.ApplyOpenHand(animator);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save Closed Hands Pose"))
            {
                Animator animator = poseManager.gameObject.GetComponentInChildren<Animator>();
                poseManager.SaveClosedHand(animator);
            }

            if (GUILayout.Button("Apply Closed Hands Pose"))
            {
                Undo.RegisterCompleteObjectUndo(poseManager.gameObject, "Apply Closed Hands Pose");
                Animator animator = poseManager.gameObject.GetComponentInChildren<Animator>();
                poseManager.ApplyClosedHand(animator);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            GUILayout.EndHorizontal();
        }
    }

}