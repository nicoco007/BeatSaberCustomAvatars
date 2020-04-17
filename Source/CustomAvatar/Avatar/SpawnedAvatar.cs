using System;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar.Avatar
{
	internal class SpawnedAvatar : MonoBehaviour
	{
		public LoadedAvatar avatar { get; private set; }
		public AvatarTracking tracking { get; private set; }
        public AvatarIK ik { get; private set; }
        public AvatarFingerTracking fingerTracking { get; private set; }
		public AvatarEventsPlayer eventsPlayer { get; private set; }

        private Settings _settings;
        private ILogger _logger;
        private FirstPersonExclusion[] _firstPersonExclusions;
        private Renderer[] _renderers;

        private bool _isCalibrationModeEnabled;

        [Inject]
        public void Inject(DiContainer container, ILoggerFactory loggerFactory, LoadedAvatar avatar, AvatarInput input, Settings settings)
        {
            this.avatar = avatar ?? throw new ArgumentNullException(nameof(avatar));
            
            if (input == null) throw new ArgumentNullException(nameof(input));

            _logger = loggerFactory.CreateLogger<SpawnedAvatar>(avatar.descriptor.name);
            _settings = settings;

            _firstPersonExclusions = GetComponentsInChildren<FirstPersonExclusion>();
            _renderers = GetComponentsInChildren<Renderer>();

            eventsPlayer   = container.InstantiateComponent<AvatarEventsPlayer>(gameObject);
            tracking       = container.InstantiateComponent<AvatarTracking>(gameObject, new object[] { avatar, input });

            if (avatar.isIKAvatar)
            {
                ik = container.InstantiateComponent<AvatarIK>(gameObject, new object[] { avatar, input });
            }

            if (avatar.supportsFingerTracking)
            {
                fingerTracking = container.InstantiateComponent<AvatarFingerTracking>(gameObject);
            }

            DontDestroyOnLoad(this);
        }

        private void OnDestroy()
        {
            Destroy(gameObject);
        }

        public void EnableCalibrationMode()
        {
            if (_isCalibrationModeEnabled || !ik) return;

            _isCalibrationModeEnabled = true;

            tracking.isCalibrationModeEnabled = true;
            ik.EnableCalibrationMode();
        }

        public void DisableCalibrationMode()
        {
            if (!_isCalibrationModeEnabled || !ik) return;

            tracking.isCalibrationModeEnabled = false;
            ik.DisableCalibrationMode();

            _isCalibrationModeEnabled = false;
        }

        // TODO make this class subscribe to an event rather than calling externally
        public void OnFirstPersonEnabledChanged()
        {
	        SetChildrenToLayer(_settings.isAvatarVisibleInFirstPerson ? AvatarLayers.AlwaysVisible : AvatarLayers.OnlyInThirdPerson);

            foreach (FirstPersonExclusion firstPersonExclusion in _firstPersonExclusions)
            {
                foreach (GameObject gameObj in firstPersonExclusion.exclude)
                {
                    if (!gameObj) continue;

                    _logger.Debug($"Excluding '{gameObj.name}' from first person view");
                    gameObj.layer = AvatarLayers.OnlyInThirdPerson;
                }
            }
        }

        private void SetChildrenToLayer(int layer)
        {
	        foreach (Renderer renderer in _renderers)
            {
                renderer.gameObject.layer = layer;
	        }
        }
    }
}
