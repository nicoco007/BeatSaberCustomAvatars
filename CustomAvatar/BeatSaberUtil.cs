using System.Linq;
using UnityEngine;

namespace CustomAvatar
{
	public static class BeatSaberUtil
	{	
		private static MainSettingsModel _mainSettingsModel;

		public static MainSettingsModel MainSettingsModel
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
			return MainSettingsModel == null ? MainSettingsModel.kDefaultPlayerHeight : MainSettingsModel.playerHeight;
		}

		public static Vector3 GetRoomCenter()
		{
			return MainSettingsModel == null ? Vector3.zero : MainSettingsModel.roomCenter;
		}

		public static Quaternion GetRoomRotation()
		{
			return MainSettingsModel == null
				? Quaternion.identity
				: Quaternion.Euler(0, MainSettingsModel.roomRotation, 0);
		}
	}
}