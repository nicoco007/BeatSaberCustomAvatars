using System;
using CustomAvatar;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
#pragma warning disable CS0649
namespace AvatarScriptPack
{
    [Obsolete("Use VRIKManager")]
    internal class IKManager : MonoBehaviour
    {
        public Transform HeadTarget;
        public Transform LeftHandTarget;
        public Transform RightHandTarget;

        public virtual void Start()
        {
            Plugin.logger.Warn("Avatar is still using the legacy IKManager; please migrate to VRIKManager");

            var vrikManager = gameObject.AddComponent<VRIKManager>();

            vrikManager.solver_spine_headTarget = HeadTarget;
            vrikManager.solver_leftArm_target = LeftHandTarget;
            vrikManager.solver_rightArm_target = RightHandTarget;
        }
    }
}
