//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomAvatar.Editor
{
    [CustomEditor(typeof(PoseManager))]
    public class PoseManagerEditor : UnityEditor.Editor
    {
        private float _sliderValue;

        public override void OnInspectorGUI()
        {
            var richLabel = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };

            var poseManager = (PoseManager)target;

            if (!poseManager.animator.isHuman)
            {
                GUILayout.Label($"{nameof(PoseManager)} is only compatible with human animators.");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            if (poseManager.openHandIsValid)
            {
                GUILayout.Label(new GUIContent("<color='green'>\u2713</color> Open Hands", "Finger poses for open hands are set"), richLabel);
            }
            else
            {
                GUILayout.Label(new GUIContent("<color='red'>\u2715</color> Open Hands", "Finger poses for open hands are not set"), richLabel);
            }

            if (GUILayout.Button("Save Open Hands Pose"))
            {
                Undo.RegisterCompleteObjectUndo(poseManager, "Save Open Hand Poses");
                poseManager.SaveOpenHandPoses();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            if (GUILayout.Button("Clear Open Hands Pose"))
            {
                Undo.RegisterCompleteObjectUndo(poseManager, "Clear Open Hand Poses");
                poseManager.ClearOpenHandPoses();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            GUILayout.EndVertical();
            GUILayout.BeginVertical();

            if (poseManager.closedHandIsValid)
            {
                GUILayout.Label(new GUIContent("<color='green'>\u2713</color> Closed Hands", "Finger poses for closed hands are set"), richLabel);
            }
            else
            {
                GUILayout.Label(new GUIContent("<color='red'>\u2715</color> Closed Hands", "Finger poses for closed hands are not set"), richLabel);
            }

            if (GUILayout.Button("Save Closed Hands Pose"))
            {
                Undo.RegisterCompleteObjectUndo(poseManager, "Save Closed Hand Poses");
                poseManager.SaveClosedHandPoses();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            if (GUILayout.Button("Clear Closed Hands Pose"))
            {
                Undo.RegisterCompleteObjectUndo(poseManager, "Clear Closed Hand Poses");
                poseManager.ClearClosedHandPoses();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            float sliderValue = EditorGUILayout.Slider(new GUIContent("Animate Hands", "Interpolate between open and closed hands poses"), _sliderValue, 0, 1);

            if (sliderValue != _sliderValue)
            {
                Undo.RegisterFullObjectHierarchyUndo(poseManager.gameObject, "Animate Hands");
                poseManager.InterpolateHandPoses(sliderValue);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                _sliderValue = sliderValue;
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Mirror Left Hand Pose"))
            {
                Undo.RegisterFullObjectHierarchyUndo(poseManager.gameObject, "Mirror Left Hand Poses");
                MirrorLeftHand(poseManager.animator);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            if (GUILayout.Button("Mirror Right Hand Pose"))
            {
                Undo.RegisterFullObjectHierarchyUndo(poseManager.gameObject, "Mirror Right Hand Poses");
                MirrorRightHand(poseManager.animator);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Reset Hands"))
            {
                ResetHands(poseManager.animator);
            }
        }

        private void MirrorLeftHand(Animator animator)
        {
            Copy(animator, HumanBodyBones.LeftThumbProximal, HumanBodyBones.RightThumbProximal);
            Copy(animator, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.RightThumbIntermediate);
            Copy(animator, HumanBodyBones.LeftThumbDistal, HumanBodyBones.RightThumbDistal);

            Copy(animator, HumanBodyBones.LeftIndexProximal, HumanBodyBones.RightIndexProximal);
            Copy(animator, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.RightIndexIntermediate);
            Copy(animator, HumanBodyBones.LeftIndexDistal, HumanBodyBones.RightIndexDistal);

            Copy(animator, HumanBodyBones.LeftMiddleProximal, HumanBodyBones.RightMiddleProximal);
            Copy(animator, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.RightMiddleIntermediate);
            Copy(animator, HumanBodyBones.LeftMiddleDistal, HumanBodyBones.RightMiddleDistal);

            Copy(animator, HumanBodyBones.LeftRingProximal, HumanBodyBones.RightRingProximal);
            Copy(animator, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.RightRingIntermediate);
            Copy(animator, HumanBodyBones.LeftRingDistal, HumanBodyBones.RightRingDistal);

            Copy(animator, HumanBodyBones.LeftLittleProximal, HumanBodyBones.RightLittleProximal);
            Copy(animator, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.RightLittleIntermediate);
            Copy(animator, HumanBodyBones.LeftLittleDistal, HumanBodyBones.RightLittleDistal);
        }

        private void MirrorRightHand(Animator animator)
        {
            Copy(animator, HumanBodyBones.RightThumbProximal, HumanBodyBones.LeftThumbProximal);
            Copy(animator, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.LeftThumbIntermediate);
            Copy(animator, HumanBodyBones.RightThumbDistal, HumanBodyBones.LeftThumbDistal);

            Copy(animator, HumanBodyBones.RightIndexProximal, HumanBodyBones.LeftIndexProximal);
            Copy(animator, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.LeftIndexIntermediate);
            Copy(animator, HumanBodyBones.RightIndexDistal, HumanBodyBones.LeftIndexDistal);

            Copy(animator, HumanBodyBones.RightMiddleProximal, HumanBodyBones.LeftMiddleProximal);
            Copy(animator, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.LeftMiddleIntermediate);
            Copy(animator, HumanBodyBones.RightMiddleDistal, HumanBodyBones.LeftMiddleDistal);

            Copy(animator, HumanBodyBones.RightRingProximal, HumanBodyBones.LeftRingProximal);
            Copy(animator, HumanBodyBones.RightRingIntermediate, HumanBodyBones.LeftRingIntermediate);
            Copy(animator, HumanBodyBones.RightRingDistal, HumanBodyBones.LeftRingDistal);

            Copy(animator, HumanBodyBones.RightLittleProximal, HumanBodyBones.LeftLittleProximal);
            Copy(animator, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.LeftLittleIntermediate);
            Copy(animator, HumanBodyBones.RightLittleDistal, HumanBodyBones.LeftLittleDistal);
        }

        private void Copy(Animator animator, HumanBodyBones fromBone, HumanBodyBones toBone)
        {
            Transform fromTransform = animator.GetBoneTransform(fromBone);
            Transform toTransform = animator.GetBoneTransform(toBone);

            fromTransform.rotation.ToAngleAxis(out float angle, out Vector3 axis); // get angle and axis

            // mirror the axis about the plane YZ
            axis.y *= -1;
            axis.z *= -1;

            toTransform.rotation = Quaternion.AngleAxis(angle, axis); // assign it back
        }

        private void ResetHands(Animator animator)
        {
            var fingers = new HumanBodyBones[]
            {
                HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal,
                HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal,
                HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal,
                HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal,
                HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal,
                HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal,
                HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal,
                HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
                HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal,
                HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal
            };

            foreach (HumanBodyBones finger in fingers)
            {
                PrefabUtility.RevertObjectOverride(animator.GetBoneTransform(finger), InteractionMode.UserAction);
            }
        }
    }
}
