using System;
using UnityEngine;

#pragma warning disable CS0649
namespace AvatarScriptPack
{
    [Obsolete("Use VRIKManager")]
    internal class IKManager : MonoBehaviour
    {
        public Transform HeadTarget;
        public Transform LeftHandTarget;
        public Transform RightHandTarget;
    }
}
