using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AvatarScriptPack
{
    class IKManager : MonoBehaviour
    {
        public Transform HeadTarget;
        public Transform LeftHandTarget;
        public Transform RightHandTarget;

        public void Start()
        {
            var vrik = this.gameObject.GetComponent<VRIK>();

            if (vrik != null)
            {
                vrik.solver.spine.headTarget = HeadTarget;
                vrik.solver.leftArm.target = LeftHandTarget;
                vrik.solver.rightArm.target = RightHandTarget;
            }
        }
    }
}
