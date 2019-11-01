using System.Linq;
using UnityEngine;

namespace CustomAvatar.Utilities
{
	public static class BeatSaberUtil
	{
		private static Transform _originTransform;
		private static MainSettingsModel _mainSettingsModel;
		private static float _lastPlayerHeight = MainSettingsModel.kDefaultPlayerHeight;
		private static PlayerDataModelSO _playerDataModel;

		private static MainSettingsModel MainSettingsModel
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

			var playerHeight = _playerDataModel == null ? _lastPlayerHeight : _playerDataModel.playerData.playerSpecificSettings.playerHeight;
			
			_lastPlayerHeight = playerHeight;
			return playerHeight;
		}

		public static float GetPlayerEyeHeight()
		{
			return GetPlayerHeight() - MainSettingsModel.kHeadPosToPlayerHeightOffset;
		}

		public static Vector3 GetRoomCenter()
		{
			if (_originTransform == null)
			{
				return MainSettingsModel == null ? Vector3.zero : MainSettingsModel.roomCenter;
			}
			
			return _originTransform.position;
		}

		public static Quaternion GetRoomRotation()
		{
			if (_originTransform == null)
			{
				return MainSettingsModel == null ? Quaternion.identity : Quaternion.Euler(0, MainSettingsModel.roomRotation, 0);
			}

			return _originTransform.rotation;
		}
	}
}
