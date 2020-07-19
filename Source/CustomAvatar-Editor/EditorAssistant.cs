#if UNITY_EDITOR
extern alias BeatSaberDynamicBone;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

public class DBTransfer : MonoBehaviour
{
    static List<string> GetPathTil(GameObject Top, Transform child)
    {
        var result = new List<string>();
        result.Add(child.gameObject.name);
        while (child.parent != null)
        {
            if (child.parent.gameObject == Top)
            {
                result.Reverse();
                return result;
            }
            var gameobj = child.parent.gameObject;
            child = gameobj.transform;
            result.Add(gameobj.name);
        }
        return new List<string>();
    }

    static Transform GetChildrenByPath(GameObject Top, List<string> path)
    {
        var parent = Top.transform;
        foreach (string name in path)
        {
            parent = parent.Find(name);
            if (!parent)
                return null;
        }
        return parent;
    }

    static void CopyClassValues<T>(T srcComp, T destComp, GameObject srcTop, GameObject destTop)
    {
        FieldInfo[] sourceFields = srcComp.GetType().GetFields(
                                                        BindingFlags.Public |
                                                        BindingFlags.NonPublic |
                                                        BindingFlags.Instance
                                                     );
        
        for (int i = 0; i < sourceFields.Length; i++)
        {
            var value = sourceFields[i].GetValue(srcComp);
            if (value is Transform)
            {
                var path = GetPathTil(srcTop, (Transform)value);
                Transform destTransform = GetChildrenByPath(destTop, path);
                if (destTransform)
                {
                    sourceFields[i].SetValue(destComp, destTransform);
                }
                else
                {
                    sourceFields[i].SetValue(destComp, null);
                }
            }
            else if (value is List<Transform>)
            {
                List<Transform> _value = (List<Transform>)value;
                List<Transform> _destVal = new List<Transform>(Enumerable.Repeat<Transform>(null, _value.Count));
                for (int index = 0; index < _value.Count; index++)
                {
                    var path = GetPathTil(srcTop, _value[index]);
                    Transform destTransform = GetChildrenByPath(destTop, path);
                    if (destTransform)
                    {
                        _destVal[index] = destTransform;
                    }
                }
                sourceFields[i].SetValue(destComp, _destVal);
            }
            else if (value is List<BeatSaberDynamicBone::DynamicBoneColliderBase>)
            {
                List<BeatSaberDynamicBone::DynamicBoneColliderBase> _value = (List<BeatSaberDynamicBone::DynamicBoneColliderBase>)value;
                List<BeatSaberDynamicBone::DynamicBoneColliderBase> _destVal = new List<BeatSaberDynamicBone::DynamicBoneColliderBase>(Enumerable.Repeat<BeatSaberDynamicBone::DynamicBoneColliderBase>(null, _value.Count));
                for (int index = 0; index < _value.Count; index++)
                {
                    var path = GetPathTil(srcTop, _value[index].transform);
                    Transform destTransform = GetChildrenByPath(destTop, path);
                    if (destTransform)
                    {
                        _destVal[index] = destTransform.gameObject.GetComponent<BeatSaberDynamicBone::DynamicBoneColliderBase>();
                    }
                }
                sourceFields[i].SetValue(destComp, _destVal);
            }
            else
            {
                sourceFields[i].SetValue(destComp, value);
            }
        }
    }

    static void TransferComponent<T>(GameObject source, GameObject dest, GameObject srcTop, GameObject destTop) where T : Component
    {
        var srcComps = source.GetComponents<T>();
        if (srcComps.Length == 0)
            return;

        var destComps = dest.GetComponents<T>();
        foreach (T destComp in destComps)
        {
            DestroyImmediate(destComp);
        }
        foreach (T srcComp in srcComps)
        {
            var destComp = dest.AddComponent<T>();
            try
            {
                CopyClassValues(srcComp, destComp, srcTop, destTop);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
    }

    static void DescendTransfer(GameObject source, GameObject dest, GameObject srcTop, GameObject destTop)
    {
        foreach (Transform child in source.transform)
        {
            var srcObj = child.gameObject;
            if (srcObj.GetComponent<SkinnedMeshRenderer>())
                continue;

            var destObj = dest.transform.Find(srcObj.name)?.gameObject;
            if (!destObj)
            {
                destObj = new GameObject(srcObj.name);
                destObj.transform.parent = dest.transform;
            }
            destObj.transform.localPosition = srcObj.transform.localPosition;
            destObj.transform.localEulerAngles = srcObj.transform.localEulerAngles;
            destObj.transform.localScale = srcObj.transform.localScale;

            TransferComponent<BeatSaberDynamicBone::DynamicBone>(srcObj, destObj, srcTop, destTop);
            TransferComponent<BeatSaberDynamicBone::DynamicBoneCollider>(srcObj, destObj, srcTop, destTop);
            TransferComponent<BeatSaberDynamicBone::DynamicBonePlaneCollider>(srcObj, destObj, srcTop, destTop);

            DescendTransfer(srcObj, destObj, srcTop, destTop);
        }
    }

    [MenuItem("BeatSaber/Transfer Dynamic Bones")]
    static void Transfer()
    {
        var source = GameObject.Find("Source");
        var destTop = GameObject.Find("_CustomAvatar");
        var destAvatar = destTop.GetComponentInChildren<Animator>()?.gameObject;
        if (!destAvatar)
        {
            throw new NullReferenceException();
        }
        DescendTransfer(source, destAvatar, source, destAvatar);
    }
}


public class GenericGenerate : MonoBehaviour
{
    static bool isChildOf(Transform that, GameObject parent)
    {
        while (that.parent != null)
        {
            if (that.parent == parent.transform)
                return true;

            that = that.parent;
        }
        return false;
    }
    static GameObject FindGameObject(GameObject parent, string name)
    {
        var objects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in objects)
        {
            if (isChildOf(obj.transform, parent) && obj.name == name)
                return obj;
        }
        return null;
    }
    static GameObject GenerateChild(GameObject parent, string name, Transform transform = null)
    {
        var obj = parent.transform.Find(name)?.gameObject;
        if (obj)
            return obj;

        obj = new GameObject(name);
        obj.transform.parent = parent.transform;
        if (transform)
        {
            obj.transform.position = transform.position;
            obj.transform.eulerAngles = transform.eulerAngles;
            obj.transform.localScale = transform.localScale;
        }
        else
        {
            obj.transform.localPosition = new Vector3(0, 0, 0);
            obj.transform.localEulerAngles = new Vector3(0, 0, 0);
            obj.transform.localScale = new Vector3(1, 1, 1);
        }
        return obj;
    }

    class TargetInfo
    {
        public HumanBodyBones bone { get; set; }
        public Transform targetTransform { get; set; }
    }

    [MenuItem("BeatSaber/Generate Common Objects")]
    static void TopGenerate()
    {
        var top = GameObject.Find("_CustomAvatar");
        var animator = top.GetComponentInChildren<Animator>();
        var avatar = animator?.gameObject;
        var armature = avatar.transform.Find("Armature");

        GenerateChild(top, "Body");
        if (armature)
        {
            var targets = new Dictionary<string, TargetInfo>()
            {
                { "Head", new TargetInfo{ bone = HumanBodyBones.Head , targetTransform = null} },
                { "Pelvis", new TargetInfo{ bone = HumanBodyBones.Hips , targetTransform = null} },
                { "LeftHand", new TargetInfo{ bone = HumanBodyBones.LeftHand , targetTransform = null} },
                { "RightHand", new TargetInfo{ bone = HumanBodyBones.RightHand , targetTransform = null} },
                { "LeftLeg", new TargetInfo{ bone = HumanBodyBones.LeftToes , targetTransform = null} },
                { "RightLeg", new TargetInfo{ bone = HumanBodyBones.RightToes , targetTransform = null} },
            };

            foreach (var target in targets)
            {
                var armTransform = animator.GetBoneTransform(target.Value.bone);
                var targetObj = GenerateChild(top, target.Key, armTransform);
                GenerateChild(targetObj, target.Key + "Target");
            }
        }

        var ikMgr = avatar.GetComponent<CustomAvatar.VRIKManager>();
        if (!ikMgr)
        {
            ikMgr = avatar.AddComponent<CustomAvatar.VRIKManager>();
        }

        {
            ikMgr.references_root = avatar.transform;
            ikMgr.references_pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
            ikMgr.references_spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            ikMgr.references_chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            ikMgr.references_neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            ikMgr.references_head = animator.GetBoneTransform(HumanBodyBones.Head);
            ikMgr.references_leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            ikMgr.references_leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            ikMgr.references_leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            ikMgr.references_leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            ikMgr.references_rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            ikMgr.references_rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            ikMgr.references_rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            ikMgr.references_rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            ikMgr.references_leftThigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            ikMgr.references_leftCalf = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            ikMgr.references_leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            ikMgr.references_leftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);
            ikMgr.references_rightThigh = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            ikMgr.references_rightCalf = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            ikMgr.references_rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            ikMgr.references_rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);
        }

        {
            ikMgr.solver_spine_headTarget = top.transform.Find("HeadTarget");
            ikMgr.solver_spine_pelvisTarget = top.transform.Find("PelvisTarget");

            ikMgr.solver_leftArm_target = top.transform.Find("LeftHandTarget");
            ikMgr.solver_rightArm_target = top.transform.Find("RightHandTarget");

            ikMgr.solver_leftLeg_target = top.transform.Find("LeftLegTarget");
            ikMgr.solver_rightLeg_target = top.transform.Find("RightLegTarget");
        }
    }

    static void MirrorTransform(Transform A, Transform B, float angleMul = 1.0f)
    {
        B.transform.localPosition = new Vector3(
            -A.transform.localPosition.x,
            A.transform.localPosition.y,
            A.transform.localPosition.z);
        B.transform.localEulerAngles = new Vector3(
            A.transform.localEulerAngles.x * angleMul,
            -A.transform.localEulerAngles.y * angleMul,
            -A.transform.localEulerAngles.z * angleMul);
    }

    [MenuItem("BeatSaber/Mirror Targets")]
    static void Mirror()
    {
        var top = GameObject.Find("_CustomAvatar");

        {
            var leftHand = top.transform.Find("LeftHand");
            var leftHandTarget = leftHand.transform.Find("LeftHandTarget");

            var rightHand = top.transform.Find("RightHand");
            var rightHandTarget = rightHand.transform.Find("RightHandTarget");

            MirrorTransform(leftHand, rightHand);

            leftHandTarget.transform.parent = top.transform;
            rightHandTarget.transform.parent = top.transform;

            MirrorTransform(leftHandTarget, rightHandTarget, -1.0f);

            leftHandTarget.transform.parent = leftHand.transform;
            rightHandTarget.transform.parent = rightHand.transform;
        }

        {
            var leftLeg = top.transform.Find("LeftLeg");
            var leftLegTarget = leftLeg.transform.Find("LeftLegTarget");

            var rightLeg = top.transform.Find("RightLeg");
            var rightLegTarget = rightLeg.transform.Find("RightLegTarget");

            MirrorTransform(leftLeg, rightLeg);

            leftLegTarget.transform.parent = top.transform;
            rightLegTarget.transform.parent = top.transform;

            MirrorTransform(leftLegTarget, rightLegTarget);

            leftLegTarget.transform.parent = leftLeg.transform;
            rightLegTarget.transform.parent = rightLeg.transform;
        }
    }
}
#endif
