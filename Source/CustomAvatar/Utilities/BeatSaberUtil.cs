using System.Linq;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    public static class BeatSaberUtil
    {
        private static MainSettingsModel _mainSettingsModel;
        private static float _lastPlayerHeight = MainSettingsModel.kDefaultPlayerHeight;
        private static PlayerDataModelSO _playerDataModel;

        private static MainSettingsModel mainSettingsModel
        {
            get
            {
                if (_mainSettingsModel == null)
                {
                    _mainSettingsModel = Resources.FindObjectsOfTypeAll<MainSettingsModel>().FirstOrDefault();
                }

                return _mainSettingsModel;
            }
        }

        public static float GetPlayerHeight()
        {
            if (!_playerDataModel)
                _playerDataModel = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().FirstOrDefault();

            var playerHeight = !_playerDataModel ? _lastPlayerHeight : _playerDataModel.playerData.playerSpecificSettings.playerHeight;
            
            _lastPlayerHeight = playerHeight;
            return playerHeight;
        }

        public static float GetPlayerEyeHeight()
        {
            return GetPlayerHeight() - MainSettingsModel.kHeadPosToPlayerHeightOffset;
        }

        public static Vector3 GetRoomCenter()
        {
            return !mainSettingsModel ? Vector3.zero : mainSettingsModel.roomCenter;
        }

        public static Quaternion GetRoomRotation()
        {
            return !mainSettingsModel ? Quaternion.identity : Quaternion.Euler(0, mainSettingsModel.roomRotation, 0);
        }
    }
}
