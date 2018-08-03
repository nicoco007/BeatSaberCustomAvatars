using System;
using System.Linq;
using UnityEngine;

namespace CustomAvatar
{
	public static class BeatSaberUtil
	{	
		private static MainSettingsModel _mainSettingsModel;
		private static VRCenterAdjust _vrCenterAdjust;

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
		
		public static VRCenterAdjust VRCenterAdjust
		{
			get
			{
				if (_vrCenterAdjust == null)
				{
					_vrCenterAdjust = Resources.FindObjectsOfTypeAll<VRCenterAdjust>().FirstOrDefault();
				}

				return _vrCenterAdjust;
			}
		}
		
		public static float GetPlayerHeight()
		{
			return MainSettingsModel == null ? MainSettingsModel.kDefaultPlayerHeight : MainSettingsModel.playerHeight;
		}

		public static Vector3 GetRoomCenter()
		{
			return VRCenterAdjust == null
				? MainSettingsModel == null ? Vector3.zero : MainSettingsModel.roomCenter
				: VRCenterAdjust.transform.position;
		}

		public static Quaternion GetRoomRotation()
		{
			return VRCenterAdjust == null
				? MainSettingsModel == null ? Quaternion.identity :
				Quaternion.Euler(0, MainSettingsModel.roomRotation, 0)
				: VRCenterAdjust.transform.rotation;
		}
	}
}