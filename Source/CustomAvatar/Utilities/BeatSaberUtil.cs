using System.Linq;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class BeatSaberUtil
    {
        private static MainSettingsModelSO _mainSettingsModel;
        private static PlayerData _playerData;

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

        private static PlayerData playerData
        {
            get
            {
                if (_playerData == null)
                {
                    _playerData = Object.FindObjectOfType<PlayerDataModel>()?.playerData;
                }

                return _playerData;
            }
        }

        public static float GetPlayerHeight()
        {
            return playerData?.playerSpecificSettings.playerHeight ?? MainSettingsModelSO.kDefaultPlayerHeight;
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
