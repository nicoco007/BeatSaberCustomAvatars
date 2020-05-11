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
        private DiContainer _container;

        [Inject]
        private void Inject(ILoggerProvider loggerProvider, DiContainer container)
        {
            _logger = loggerProvider.CreateLogger<IKManager>();
            _container = container;
        }

        public virtual void Start()
        {
            _logger.Warning("Avatar is still using the legacy IKManager; please migrate to VRIKManager");

            var vrikManager = _container.InstantiateComponent<VRIKManager>(gameObject);

            vrikManager.solver_spine_headTarget = HeadTarget;
            vrikManager.solver_leftArm_target = LeftHandTarget;
            vrikManager.solver_rightArm_target = RightHandTarget;
        }
    }
}
