using System;
using CustomAvatar;
using UnityEngine;

namespace AvatarScriptPack
{
    [Obsolete("Use VRIKManager")]
    [RequireComponent(typeof(VRIKManager))]
    class IKManager : MonoBehaviour
    {
        public Transform HeadTarget;
        public Transform LeftHandTarget;
        public Transform RightHandTarget;

        public void Start()
        {
            var vrikManager = this.gameObject.GetComponent<VRIKManager>();

            if (vrikManager != null)
            {
                vrikManager.solver_spine_headTarget = HeadTarget;
                vrikManager.solver_leftArm_target = LeftHandTarget;
                vrikManager.solver_rightArm_target = RightHandTarget;
            }
        }
    }
}
