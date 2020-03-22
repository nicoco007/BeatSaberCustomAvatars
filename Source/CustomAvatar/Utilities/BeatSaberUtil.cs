using System.Linq;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class BeatSaberUtil
    {
        private static MainSettingsModelSO _mainSettingsModel;
        private static PlayerDataModel _playerDataModel;

        public const float kDefaultPlayerEyeHeight = MainSettingsModelSO.kDefaultPlayerHeight - MainSettingsModelSO.kHeadPosToPlayerHeightOffset;

        private static MainSettingsModelSO mainSettingsModel
        {
            get
            {
                if (!_mainSettingsModel)
                {
                    _mainSettingsModel = Resources.FindObjectsOfTypeAll<MainSettingsModelSO>().FirstOrDefault();
                }

                return _mainSettingsModel;
            }
        }

        private static PlayerDataModel playerDataModel
        {
            get
            {
                if (!_playerDataModel)
                {
                    _playerDataModel = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault(m => m.playerData != null);
                }

                return _playerDataModel;
            }
        }

        public static float GetPlayerHeight()
        {
            return !playerDataModel ? MainSettingsModelSO.kDefaultPlayerHeight : playerDataModel.playerData.playerSpecificSettings.playerHeight;
        }

        public static float GetPlayerEyeHeight()
        {
            return GetPlayerHeight() - MainSettingsModelSO.kHeadPosToPlayerHeightOffset;
        }

        public static Vector3 GetRoomCenter()
        {
            return !mainSettingsModel ? Vector3.zero : mainSettingsModel.roomCenter;
        }

        public static Quaternion GetRoomRotation()
        {
            return !mainSettingsModel ? Quaternion.identity : Quaternion.Euler(0, mainSettingsModel.roomRotation, 0);
        }

        public static Vector3 GetControllerPositionOffset()
        {
            return !mainSettingsModel ? Vector3.zero : mainSettingsModel.controllerPosition;
        }

        public static Vector3 GetControllerRotationOffset()
        {
            return !mainSettingsModel ? Vector3.zero : mainSettingsModel.controllerRotation;
        }
    }
}
