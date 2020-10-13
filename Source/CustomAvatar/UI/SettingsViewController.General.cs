//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
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

/*
using CustomAvatar.Avatar;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using System.Collections.Generic;
using UnityEngine;

namespace CustomAvatar.UI
{
    internal partial class SettingsViewController
    {
        #region Components
        #pragma warning disable 649
        #pragma warning disable IDE0044

        [UIComponent("visible-in-first-person")] private CheckboxSetting _visibleInFirstPerson;
        [UIComponent("resize-mode")] private ListSetting _resizeMode;
        [UIComponent("enable-locomotion")] private CheckboxSetting _enableLocomotion;
        [UIComponent("floor-adjust")] private CheckboxSetting _floorHeightAdjust;
        [UIComponent("move-floor-with-room-adjust")] private CheckboxSetting _moveFloorWithRoomAdjust;
        [UIComponent("camera-clip-plane")] private IncrementSetting _cameraNearClipPlane;

        #pragma warning restore 649
        #pragma warning restore IDE0044
        #endregion

        #region Values

        [UIValue("resize-mode-options")] private readonly List<object> _resizeModeOptions = new List<object> { AvatarResizeMode.None, AvatarResizeMode.Height, AvatarResizeMode.ArmSpan };

        #endregion

        #region Actions

        [UIAction("visible-in-first-person-change")]
        private void OnVisibleInFirstPersonChanged(bool value)
        {
            _settings.isAvatarVisibleInFirstPerson = value;
        }

        [UIAction("resize-mode-change")]
        private void OnResizeModeChanged(AvatarResizeMode value)
        {
            _settings.resizeMode = value;
            _avatarManager.ResizeCurrentAvatar();
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
            _settings.enableLocomotion = value;
            _avatarManager.UpdateLocomotionEnabled();
        }

        [UIAction("floor-adjust-change")]
        private void OnFloorHeightAdjustChanged(bool value)
        {
            _settings.enableFloorAdjust = value;
            _avatarManager.ResizeCurrentAvatar();
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
            _settings.moveFloorWithRoomAdjust = value;
        }

        #endregion

        #region Arm Span Measurement

        private const float kMinArmSpan = 0.5f;

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
            var armSpan = Vector3.Distance(_trackedDeviceManager.leftHand.position, _trackedDeviceManager.rightHand.position);

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
                _settings.playerArmSpan = _maxMeasuredArmSpan;
                _isMeasuring = false;

                if (_settings.resizeMode == AvatarResizeMode.ArmSpan)
                {
                    _avatarManager.ResizeCurrentAvatar();
                }
            }
        }

        #endregion
    }
}
*/
