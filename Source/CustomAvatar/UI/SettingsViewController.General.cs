//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Avatar;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HMUI;

namespace CustomAvatar.UI
{
    internal partial class SettingsViewController
    {
        #region Components
        #pragma warning disable 649
        #pragma warning disable IDE0044

        [UIComponent("visible-in-first-person")] private ToggleSetting _visibleInFirstPerson;
        [UIComponent("resize-mode")] private DropDownListSetting _resizeMode;
        [UIComponent("enable-locomotion")] private ToggleSetting _enableLocomotion;
        [UIComponent("floor-height-adjust")] private DropDownListSetting _floorHeightAdjust;
        [UIComponent("move-floor-with-room-adjust")] private ToggleSetting _moveFloorWithRoomAdjust;
        [UIComponent("camera-clip-plane")] private IncrementSetting _cameraNearClipPlane;
        [UIComponent("measure-button")] private Button _measureButton;
        [UIComponent("measure-button")] private HoverHint _measureButtonHoverHint;

#pragma warning restore 649
#pragma warning restore IDE0044
        #endregion

        #region Values

        [UIValue("resize-mode-options")] private readonly List<object> _resizeModeOptions = new List<object> { AvatarResizeMode.None, AvatarResizeMode.Height, AvatarResizeMode.ArmSpan };
        [UIValue("floor-height-adjust-options")] private readonly List<object> _floorHeightAdjustOptions = new List<object> { FloorHeightAdjust.Off, FloorHeightAdjust.PlayersPlaceOnly, FloorHeightAdjust.EntireEnvironment };
        #endregion

        #region Actions

        [UIAction("visible-in-first-person-change")]
        private void OnVisibleInFirstPersonChanged(bool value)
        {
            _settings.isAvatarVisibleInFirstPerson.value = value;
        }

        [UIAction("resize-mode-change")]
        private void OnResizeModeChanged(AvatarResizeMode value)
        {
            _settings.resizeMode.value = value;
        }

        [UIAction("resize-mode-formatter")]
        private string ResizeModeFormatter(object value)
        {
            if (!(value is AvatarResizeMode)) return null;

            switch ((AvatarResizeMode)value)
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

        [UIAction("enable-locomotion-change")]
        private void OnEnableLocomotionChanged(bool value)
        {
            _settings.enableLocomotion.value = value;
        }

        [UIAction("floor-height-adjust-formatter")]
        private string FloorHeightAdjustFormatter(object value)
        {
            if (!(value is FloorHeightAdjust)) return null;

            switch ((FloorHeightAdjust)value)
            {
                case FloorHeightAdjust.Off:
                    return "Off";
                case FloorHeightAdjust.PlayersPlaceOnly:
                    return "Player's Place Only";
                case FloorHeightAdjust.EntireEnvironment:
                    return "Entire Environment";
                default:
                    return null;
            }
        }

        [UIAction("floor-height-adjust-change")]
        private void OnFloorHeightAdjustChanged(FloorHeightAdjust value)
        {
            _settings.floorHeightAdjust.value = value;
        }

        [UIAction("camera-clip-plane-change")]
        private void OnCameraClipPlaneChanged(float value)
        {
            _settings.cameraNearClipPlane = value;

            // TODO logic in view controller is not ideal
            Camera mainCamera = Camera.main;

            if (mainCamera)
            {
                mainCamera.nearClipPlane = value;
            }
            else
            {
                _logger.Error("Could not find main camera!");
            }
        }

        [UIAction("measure-arm-span-click")]
        private void OnMeasureArmSpanButtonClicked()
        {
            MeasureArmSpan();
        }

        [UIAction("move-floor-with-room-adjust-change")]
        private void OnMoveFloorWithRoomAdjustChanged(bool value)
        {
            _settings.moveFloorWithRoomAdjust.value = value;
        }

        #endregion

        #region Arm Span Measurement

        private const float kMinArmSpan = 0.5f;
        private const float kStableMeasurementTimeout = 3f;
        private const float kMinDifferenceToReset = 0.02f;

        private bool _isMeasuring;
        private float _lastUpdateTime;
        private float _lastMeasuredArmSpan;

        private void MeasureArmSpan()
        {
            if (_isMeasuring) return;
            if (!_playerInput.TryGetPose(DeviceUse.LeftHand, out Pose _) || !_playerInput.TryGetPose(DeviceUse.RightHand, out Pose _)) return;

            _isMeasuring = true;
            _lastMeasuredArmSpan = kMinArmSpan;
            _lastUpdateTime = Time.timeSinceLevelLoad;

            InvokeRepeating(nameof(ScanArmSpan), 0.0f, 0.1f);
        }

        private void ScanArmSpan()
        {
            if (Time.timeSinceLevelLoad - _lastUpdateTime < kStableMeasurementTimeout && _playerInput.TryGetPose(DeviceUse.LeftHand, out Pose leftHand) && _playerInput.TryGetPose(DeviceUse.RightHand, out Pose rightHand))
            {
                float armSpan = Vector3.Distance(leftHand.position, rightHand.position);

                if (Mathf.Abs(armSpan - _lastMeasuredArmSpan) >= kMinDifferenceToReset)
                {
                    _lastUpdateTime = Time.timeSinceLevelLoad;
                }

                _lastMeasuredArmSpan = Mathf.Max(kMinArmSpan, (_lastMeasuredArmSpan + armSpan) / 2);
                _armSpanLabel.SetText($"Measuring... {_lastMeasuredArmSpan:0.00} m");
            }
            else
            {
                CancelInvoke(nameof(ScanArmSpan));

                _armSpanLabel.SetText($"{_lastMeasuredArmSpan:0.00} m");
                _settings.playerArmSpan.value = _lastMeasuredArmSpan;
                _isMeasuring = false;
            }
        }

        #endregion
    }
}
