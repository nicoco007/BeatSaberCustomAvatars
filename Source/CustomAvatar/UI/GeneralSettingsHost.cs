//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Avatar;
using CustomAvatar.Player;
using System.Collections.Generic;
using UnityEngine;
using CustomAvatar.Configuration;
using CustomAvatar.Tracking;

namespace CustomAvatar.UI
{
    internal class GeneralSettingsHost : ViewControllerHost
    {
        #region Values

        internal readonly List<object> resizeModeOptions = new List<object> { AvatarResizeMode.None, AvatarResizeMode.Height, AvatarResizeMode.ArmSpan };
        internal readonly List<object> floorHeightAdjustOptions = new List<object> { FloorHeightAdjustMode.Off, FloorHeightAdjustMode.PlayersPlaceOnly, FloorHeightAdjustMode.EntireEnvironment };

        #endregion

        private readonly VRPlayerInputInternal _playerInput;
        private readonly Settings _settings;
        private readonly PlayerDataModel _playerDataModel;
        private readonly ArmSpanMeasurer _armSpanMeasurer;

        private float _armSpan;

        internal GeneralSettingsHost(VRPlayerInputInternal playerInput, Settings settings, PlayerDataModel playerDataModel, ArmSpanMeasurer armSpanMeasurer)
        {
            _playerInput = playerInput;
            _settings = settings;
            _playerDataModel = playerDataModel;
            _armSpanMeasurer = armSpanMeasurer;
        }

        public bool visibleInFirstPerson
        {
            get => _settings.isAvatarVisibleInFirstPerson;
            set
            {
                _settings.isAvatarVisibleInFirstPerson.value = value;
                NotifyPropertyChanged();
            }
        }

        public bool showAvatarInSmoothCamera
        {
            get => _settings.showAvatarInSmoothCamera;
            set
            {
                _settings.showAvatarInSmoothCamera.value = value;
                NotifyPropertyChanged();
            }
        }

        public bool showAvatarInMirrors
        {
            get => _settings.showAvatarInMirrors;
            set
            {
                _settings.showAvatarInMirrors = value;
                NotifyPropertyChanged();
            }
        }

        public AvatarResizeMode resizeMode
        {
            get => _settings.resizeMode;
            set
            {
                _settings.resizeMode.value = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(showHeightAdjustWarning));
            }
        }

        public bool enableLocomotion
        {
            get => _settings.enableLocomotion;
            set
            {
                _settings.enableLocomotion.value = value;
                NotifyPropertyChanged();
            }
        }

        public FloorHeightAdjustMode floorHeightAdjustMode
        {
            get => _settings.floorHeightAdjust;
            set
            {
                _settings.floorHeightAdjust.value = value;
                NotifyPropertyChanged();
            }
        }

        public bool moveFloorWithRoomAdjust
        {
            get => _settings.moveFloorWithRoomAdjust;
            set
            {
                _settings.moveFloorWithRoomAdjust.value = value;
                NotifyPropertyChanged();
            }
        }

        public float cameraNearClipPlane
        {
            get => _settings.cameraNearClipPlane;
            set
            {
                _settings.cameraNearClipPlane.value = value;
                NotifyPropertyChanged();
            }
        }

        public bool isMeasureButtonEnabled => _playerInput.TryGetUncalibratedPose(DeviceUse.LeftHand, out Pose _) && _playerInput.TryGetUncalibratedPose(DeviceUse.RightHand, out Pose _);

        public string measureButtonText => _armSpanMeasurer.isMeasuring ? "Cancel" : "Measure";

        public string measureButtonHoverHintText => isMeasureButtonEnabled
            ? "For optimal results, hold your arms out to either side of your body and point the ends of the controllers outwards as far as possible (turn your hands if necessary)."
            : "Both controllers must be turned on to measure arm span.";

        public bool isHeightAdjustInteractable => !_armSpanMeasurer.isMeasuring;

        public bool showHeightAdjustWarning => _settings.resizeMode != AvatarResizeMode.None && _playerDataModel.playerData.playerSpecificSettings.automaticPlayerHeight;

        public float armSpan
        {
            get => _armSpan;
            set
            {
                _armSpan = value;
                _settings.playerArmSpan.value = value;
                NotifyPropertyChanged();

                resizeMode = AvatarResizeMode.ArmSpan;
            }
        }

        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _armSpanMeasurer.updated += OnArmSpanMeasurementChanged;
            _armSpanMeasurer.completed += OnArmSpanMeasurementCompleted;
            _playerInput.inputChanged += OnPlayerInputChanged;

            _armSpan = _settings.playerArmSpan;
            NotifyPropertyChanged(nameof(armSpan));

            NotifyPropertyChanged(nameof(showHeightAdjustWarning));
            OnPlayerInputChanged();
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _armSpanMeasurer.updated -= OnArmSpanMeasurementChanged;
            _armSpanMeasurer.completed -= OnArmSpanMeasurementCompleted;
            _playerInput.inputChanged -= OnPlayerInputChanged;
        }

        private void OnPlayerInputChanged()
        {
            NotifyPropertyChanged(nameof(isMeasureButtonEnabled));
            NotifyPropertyChanged(nameof(measureButtonHoverHintText));
        }

        private void OnArmSpanMeasurementChanged(float armSpan)
        {
            // update UI but not config
            _armSpan = armSpan;
            NotifyPropertyChanged(nameof(armSpan));
        }

        private void OnArmSpanMeasurementCompleted(float armSpan)
        {
            this.armSpan = armSpan;
            NotifyPropertyChanged(nameof(isHeightAdjustInteractable));
            NotifyPropertyChanged(nameof(measureButtonText));
        }

        #region Actions
#pragma warning disable IDE0051

        private string ResizeModeFormatter(object value)
        {
            if (!(value is AvatarResizeMode avatarResizeMode)) return null;

            switch (avatarResizeMode)
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

        private string FloorHeightAdjustFormatter(object value)
        {
            if (!(value is FloorHeightAdjustMode floorHeightAdjustMode)) return null;

            switch (floorHeightAdjustMode)
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

        private string ArmSpanFormatter(float value)
        {
            if (_armSpanMeasurer.isMeasuring)
            {
                return $"Measuring... {value:0.00} m";
            }
            else
            {
                return $"{value:0.00} m";
            }
        }

        private void OnMeasureArmSpanButtonClicked()
        {
            if (_armSpanMeasurer.isMeasuring)
            {
                _armSpanMeasurer.Cancel();
                this.armSpan = _settings.playerArmSpan;
            }
            else
            {
                _armSpanMeasurer.MeasureArmSpan();
            }

            NotifyPropertyChanged(nameof(isHeightAdjustInteractable));
            NotifyPropertyChanged(nameof(measureButtonText));
        }

#pragma warning restore IDE0051
        #endregion
    }
}
