using System;
using UnityEngine;

namespace CustomAvatar.Utilities
{
	internal static class Settings
	{
		private const string kIsAvatarVisibleInFirstPersonKey = "CustomAvatar.IsAvatarVisibleInFirstPerson";
		private const string kResizeModeKey = "CustomAvatar.ResizeMode";
		private const string kEnableFloorAdjustKey = "CustomAvatar.EnableFloorAdjust";
		private const string kPreviousAvatarPathKey = "CustomAvatar.PreviousAvatarPath";
		private const string kPlayerArmSpanKey = "CustomAvatar.PlayerArmSpan";
		private const string kCalibrateFullBodyTrackingOnStartKey = "CustomAvatar.CalibrateFullBodyTrackingOnStart";

		public static bool isAvatarVisibleInFirstPerson
		{
			get => Convert.ToBoolean(PlayerPrefs.GetInt(kIsAvatarVisibleInFirstPersonKey, 1));
			set => PlayerPrefs.SetInt(kIsAvatarVisibleInFirstPersonKey, Convert.ToInt32(value));
		}

		public static AvatarResizeMode resizeMode
		{
			get => (AvatarResizeMode) PlayerPrefs.GetInt(kResizeModeKey, (int) AvatarResizeMode.Height);
			set => PlayerPrefs.SetInt(kResizeModeKey, (int) value);
		}

		public static bool enableFloorAdjust
		{
			get => Convert.ToBoolean(PlayerPrefs.GetInt(kEnableFloorAdjustKey, 0));
			set => PlayerPrefs.SetInt(kEnableFloorAdjustKey, Convert.ToInt32(value));
		}

		public static string previousAvatarPath
		{
			get => PlayerPrefs.GetString(kPreviousAvatarPathKey, null);
			set => PlayerPrefs.SetString(kPreviousAvatarPathKey, value);
		}

		public static float playerArmSpan
		{
			get => PlayerPrefs.GetFloat(kPlayerArmSpanKey, 1.8f);
			set => PlayerPrefs.SetFloat(kPlayerArmSpanKey, value);
		}

		public static bool calibrateFullBodyTrackingOnStart
		{
			get => Convert.ToBoolean(PlayerPrefs.GetInt(kCalibrateFullBodyTrackingOnStartKey, 0));
			set => PlayerPrefs.SetInt(kCalibrateFullBodyTrackingOnStartKey, Convert.ToInt32(value));
		}
	}
}
