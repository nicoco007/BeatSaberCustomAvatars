using System;
using CustomAvatar;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

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

        private ILogger _logger;

        [Inject]
        private void Inject(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<IKManager>();
        }

        public virtual void Start()
        {
            _logger.Warning("Avatar is still using the legacy IKManager; please migrate to VRIKManager");

            var vrikManager = gameObject.AddComponent<VRIKManager>();

            vrikManager.solver_spine_headTarget = HeadTarget;
            vrikManager.solver_leftArm_target = LeftHandTarget;
            vrikManager.solver_rightArm_target = RightHandTarget;
        }
    }
}
