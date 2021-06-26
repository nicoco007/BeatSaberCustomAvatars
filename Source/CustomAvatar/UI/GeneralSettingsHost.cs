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
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using CustomAvatar.Configuration;
using CustomAvatar.Tracking;

namespace CustomAvatar.UI
{
    internal class GeneralSettingsHost : IViewControllerHost
    {
        #region Components
#pragma warning disable CS0649, IDE0044

        [UIComponent("arm-span")] private CurvedTextMeshPro _armSpanLabel;
        [UIComponent("visible-in-first-person")] private ToggleSetting _visibleInFirstPerson;
        [UIComponent("resize-mode")] private DropDownListSetting _resizeMode;
        [UIComponent("enable-locomotion")] private ToggleSetting _enableLocomotion;
        [UIComponent("floor-height-adjust")] private DropDownListSetting _floorHeightAdjust;
        [UIComponent("move-floor-with-room-adjust")] private ToggleSetting _moveFloorWithRoomAdjust;
        [UIComponent("camera-clip-plane")] private IncrementSetting _cameraNearClipPlane;
        [UIComponent("measure-button")] private Button _measureButton;
        [UIComponent("measure-button")] private HoverHint _measureButtonHoverHint;
        [UIComponent("height-adjust-warning-text")] private RectTransform _heightAdjustWarningText;

#pragma warning restore CS0649, IDE0044
        #endregion

        #region Values

        internal readonly List<object> resizeModeOptions = new List<object> { AvatarResizeMode.None, AvatarResizeMode.Height, AvatarResizeMode.ArmSpan };
        internal readonly List<object> floorHeightAdjustOptions = new List<object> { FloorHeightAdjustMode.Off, FloorHeightAdjustMode.PlayersPlaceOnly, FloorHeightAdjustMode.EntireEnvironment };

        #endregion

        private readonly VRPlayerInputInternal _playerInput;
        private readonly Settings _settings;
        private readonly PlayerDataModel _playerDataModel;
        private readonly ArmSpanMeasurer _armSpanMeasurer;

        internal GeneralSettingsHost(VRPlayerInputInternal playerInput, Settings settings, PlayerDataModel playerDataModel, ArmSpanMeasurer armSpanMeasurer)
        {
            _playerInput = playerInput;
            _settings = settings;
            _playerDataModel = playerDataModel;
            _armSpanMeasurer = armSpanMeasurer;
        }

        public void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _visibleInFirstPerson.Value = _settings.isAvatarVisibleInFirstPerson;
            _resizeMode.Value = _settings.resizeMode.value;
            _enableLocomotion.Value = _settings.enableLocomotion;
            _floorHeightAdjust.Value = _settings.floorHeightAdjust.value;
            _moveFloorWithRoomAdjust.Value = _settings.moveFloorWithRoomAdjust;
            _cameraNearClipPlane.Value = _settings.cameraNearClipPlane;

            _armSpanLabel.SetText($"{_settings.playerArmSpan.value:0.00} m");

            _settings.resizeMode.changed += OnSettingsResizeModeChanged;
            _armSpanMeasurer.updated += OnArmSpanMeasurementChanged;
            _armSpanMeasurer.completed += OnArmSpanMeasurementCompleted;

            OnSettingsResizeModeChanged(_settings.resizeMode);
        }

        public void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _settings.resizeMode.changed -= OnSettingsResizeModeChanged;
            _armSpanMeasurer.updated -= OnArmSpanMeasurementChanged;
            _armSpanMeasurer.completed -= OnArmSpanMeasurementCompleted;
        }

        public void UpdateUI(SpawnedAvatar avatar)
        {
            if (_playerInput.TryGetUncalibratedPose(DeviceUse.LeftHand, out Pose _) && _playerInput.TryGetUncalibratedPose(DeviceUse.RightHand, out Pose _))
            {
                _measureButton.interactable = true;
                _measureButtonHoverHint.text = "For optimal results, hold your arms out to either side of your body and point the ends of the controllers outwards as far as possible (turn your hands if necessary).";
            }
            else
            {
                _measureButton.interactable = false;
                _measureButtonHoverHint.text = "Controllers not detected";
            }
        }

        private void OnSettingsResizeModeChanged(AvatarResizeMode resizeMode)
        {
            _heightAdjustWarningText.gameObject.SetActive(resizeMode != AvatarResizeMode.None && _playerDataModel.playerData.playerSpecificSettings.automaticPlayerHeight);
        }

        private void OnArmSpanMeasurementChanged(float armSpan)
        {
            _armSpanLabel.SetText($"Measuring... {armSpan:0.00} m");
        }

        private void OnArmSpanMeasurementCompleted(float armSpan)
        {
            _armSpanLabel.SetText($"{armSpan:0.00} m");
            _settings.playerArmSpan.value = armSpan;
        }

        #region Actions
#pragma warning disable IDE0051

        private void OnVisibleInFirstPersonChanged(bool value)
        {
            _settings.isAvatarVisibleInFirstPerson.value = value;
        }

        private void OnResizeModeChanged(AvatarResizeMode value)
        {
            _settings.resizeMode.value = value;
        }

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

        private void OnEnableLocomotionChanged(bool value)
        {
            _settings.enableLocomotion.value = value;
        }

        private string FloorHeightAdjustFormatter(object value)
        {
            if (!(value is FloorHeightAdjustMode)) return null;

            switch ((FloorHeightAdjustMode)value)
            {
                case FloorHeightAdjustMode.Off:
                    return "Off";
                case FloorHeightAdjustMode.PlayersPlaceOnly:
                    return "Player's Place Only";
                case FloorHeightAdjustMode.EntireEnvironment:
                    return "Entire Environment";
                default:
                    return null;
            }
        }

        private void OnFloorHeightAdjustChanged(FloorHeightAdjustMode value)
        {
            _settings.floorHeightAdjust.value = value;
        }

        private void OnCameraClipPlaneChanged(float value)
        {
            _settings.cameraNearClipPlane.value = value;
        }

        private void OnMeasureArmSpanButtonClicked()
        {
            _armSpanMeasurer.MeasureArmSpan();
        }

        private void OnMoveFloorWithRoomAdjustChanged(bool value)
        {
            _settings.moveFloorWithRoomAdjust.value = value;
        }

#pragma warning restore IDE0051
        #endregion
    }
}
