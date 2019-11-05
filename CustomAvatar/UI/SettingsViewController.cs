using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using TMPro;
using UnityEngine;

#pragma warning disable 649 // disable "field is never assigned"
#pragma warning disable IDE0044 // disable "make field readonly"
// ReSharper disable UnusedMember.Local
// ReSharper disable NotAccessedField.Local
namespace CustomAvatar.UI
{
	class SettingsViewController : BSMLResourceViewController
	{
		public override string ResourceName => "CustomAvatar.Views.SettingsViewController.bsml";

		#region Components
        
		[UIComponent("arm-span")] private TextMeshProUGUI _armSpanLabel;

		#endregion

		#region Properties

		[UIValue("resize-options")] private readonly List<object> _resizeModeOptions = new List<object> { AvatarResizeMode.None, AvatarResizeMode.Height, AvatarResizeMode.ArmSpan };

		#endregion

		#region Values

		[UIValue("visible-first-person-value")] private bool _visibleInFirstPerson;
		[UIValue("resize-value")] private AvatarResizeMode _resizeMode;
		[UIValue("floor-adjust-value")] private bool _floorHeightAdjust;
		[UIValue("calibrate-fbt-on-start")] private bool _calibrateFullBodyTrackingOnStart;

		#endregion

		protected override void DidActivate(bool firstActivation, ActivationType type)
		{
			_visibleInFirstPerson = Settings.isAvatarVisibleInFirstPerson;
			_resizeMode = Settings.resizeMode;
			_floorHeightAdjust = Settings.enableFloorAdjust;
			_calibrateFullBodyTrackingOnStart = Settings.calibrateFullBodyTrackingOnStart;

			base.DidActivate(firstActivation, type);

			_armSpanLabel.SetText($"{Settings.playerArmSpan:0.00} m");
		}

		#region Actions

		[UIAction("visible-first-person-change")]
		private void OnVisibleInFirstPersonChanged(bool value)
		{
			Settings.isAvatarVisibleInFirstPerson = value;
            AvatarManager.Instance.OnFirstPersonEnabledChanged();
		}

		[UIAction("resize-change")]
		private void OnResizeModeChanged(AvatarResizeMode value)
		{
			Settings.resizeMode = value;
			AvatarManager.Instance.ResizeCurrentAvatar();
		}

		[UIAction("floor-adjust-change")]
		private void OnFloorHeightAdjustChanged(bool value)
		{
			Settings.enableFloorAdjust = value;
			AvatarManager.Instance.ResizeCurrentAvatar();
		}

		[UIAction("resize-mode-formatter")]
		private string ResizeModeFormatter(object value)
		{
			if (!(value is AvatarResizeMode)) return null;

			switch ((AvatarResizeMode) value)
			{
                case AvatarResizeMode.Height:
					return "Height";
                case AvatarResizeMode.ArmSpan:
	                return "Arm Span";
                case AvatarResizeMode.None:
	                return "Don't Resize";
	            default:
	                return null;
			}
		}

		[UIAction("measure-arm-span-click")]
		private void OnMeasureArmSpanButtonClicked()
		{
			MeasureArmSpan();
		}

		[UIAction("calibrate-fbt-click")]
		private void OnCalibrateFullBodyTrackingClicked()
		{
			AvatarManager.Instance.AvatarTailor.CalibrateFullBodyTracking();
		}

		[UIAction("calibrate-fbt-on-start-change")]
		private void OnCalibrateFullBodyTrackingOnStartChanged(bool value)
		{
			Settings.calibrateFullBodyTrackingOnStart = value;
		}

		#endregion

		#region Arm Span Measurement
		
		private const float kMinArmSpan = 0.5f;

		private TrackedDeviceManager _playerInput = PersistentSingleton<TrackedDeviceManager>.instance;
		private bool _isMeasuring;
		private float _maxMeasuredArmSpan;
		private float _lastUpdateTime;

		private void MeasureArmSpan()
		{
			if (_isMeasuring) return;

			_isMeasuring = true;
			_maxMeasuredArmSpan = kMinArmSpan;
			_lastUpdateTime = Time.timeSinceLevelLoad;

			InvokeRepeating(nameof(ScanArmSpan), 0.0f, 0.1f);
		}

		private void ScanArmSpan()
		{
			var armSpan = Vector3.Distance(_playerInput.LeftHand.Position, _playerInput.RightHand.Position);

			if (armSpan > _maxMeasuredArmSpan)
			{
				_maxMeasuredArmSpan = armSpan;
				_lastUpdateTime = Time.timeSinceLevelLoad;
			}

			if (Time.timeSinceLevelLoad - _lastUpdateTime < 2.0f)
			{
				_armSpanLabel.SetText($"Measuring... {_maxMeasuredArmSpan:0.00} m");
			}
			else
			{
				CancelInvoke(nameof(ScanArmSpan));
				_armSpanLabel.SetText($"{_maxMeasuredArmSpan:0.00} m");
				Settings.playerArmSpan = _maxMeasuredArmSpan;
				_isMeasuring = false;
			}
		}

		#endregion
	}
}
