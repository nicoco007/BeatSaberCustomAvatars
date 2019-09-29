using CustomAvatar;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PoseManager))]
public class PoseManagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		PoseManager script = (PoseManager)target;

		if (GUILayout.Button("Save Open Hands Pose"))
		{
			Animator animator = script.gameObject.GetComponentInChildren<Animator>();
			script.SetOpenHand(animator);
			Debug.Log(script.OpenHand_Left_IndexProximal);
		}

		if (GUILayout.Button("Save Closed Hands Pose"))
		{
			Animator animator = script.gameObject.GetComponentInChildren<Animator>();
			script.SetClosedHand(animator);
			Debug.Log(script.ClosedHand_Left_IndexProximal);
		}
	}
}
