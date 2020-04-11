using System;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace CustomAvatar.Avatar
{
	internal class SpawnedAvatar
	{
		public LoadedAvatar avatar { get; }
		public AvatarTracking tracking { get; }
        public AvatarIK ik { get; }
		public AvatarEventsPlayer eventsPlayer { get; }

        private readonly GameObject _gameObject;
        private readonly FirstPersonExclusion[] _firstPersonExclusions;

        private bool _isCalibrationModeEnabled;

        public SpawnedAvatar(DiContainer container, LoadedAvatar avatar, AvatarInput input)
        {
            this.avatar = avatar ?? throw new ArgumentNullException(nameof(avatar));
            
            if (input == null) throw new ArgumentNullException(nameof(input));

            _gameObject            = Object.Instantiate(avatar.gameObject);
            _firstPersonExclusions = _gameObject.GetComponentsInChildren<FirstPersonExclusion>();

            eventsPlayer   = container.InstantiateComponent<AvatarEventsPlayer>(_gameObject);
            tracking       = container.InstantiateComponent<AvatarTracking>(_gameObject);

            tracking.avatar = avatar;
            tracking.input  = input;
            
            if (avatar.isIKAvatar)
            {
                ik = container.InstantiateComponent<AvatarIK>(_gameObject);
                ik.input = input;
                ik.avatar = avatar;
            }

            if (avatar.supportsFingerTracking)
            {
                container.InstantiateComponent<AvatarFingerTracking>(_gameObject);
            }

            Object.DontDestroyOnLoad(_gameObject);
        }

        public void Destroy()
        {
	        Object.Destroy(_gameObject);
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
	        SetChildrenToLayer(SettingsManager.settings.isAvatarVisibleInFirstPerson ? AvatarLayers.AlwaysVisible : AvatarLayers.OnlyInThirdPerson);

            foreach (FirstPersonExclusion firstPersonExclusion in _firstPersonExclusions)
            {
                foreach (GameObject gameObj in firstPersonExclusion.exclude)
                {
                    if (!gameObj) continue;

                    Plugin.logger.Debug($"Excluding '{gameObj.name}' from first person view");
                    gameObj.layer = AvatarLayers.OnlyInThirdPerson;
                }
            }
        }

        private void SetChildrenToLayer(int layer)
        {
	        foreach (Transform child in _gameObject.GetComponentsInChildren<Transform>())
	        {
		        child.gameObject.layer = layer;
	        }
        }
    }
}
