//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System.Collections.Generic;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using UnityEngine.XR;

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
        private readonly ArmSpanMeasurer _armSpanMeasurer;

        private float _armSpan;

        internal GeneralSettingsHost(VRPlayerInputInternal playerInput, Settings settings, ArmSpanMeasurer armSpanMeasurer)
        {
            _playerInput = playerInput;
            _settings = settings;
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

        public float height
        {
            get => _settings.playerEyeHeight;
            set
            {
                _settings.playerEyeHeight.value = value;
                NotifyPropertyChanged();

                resizeMode = AvatarResizeMode.Height;
            }
        }

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
            if (!(value is AvatarResizeMode avatarResizeMode))
            {
                return null;
            }

            return avatarResizeMode switch
            {
                AvatarResizeMode.Height => "Height",
                AvatarResizeMode.ArmSpan => "Arm Span",
                AvatarResizeMode.None => "Don't Resize",
                _ => null,
            };
        }

        private string FloorHeightAdjustFormatter(object value)
        {
            if (!(value is FloorHeightAdjustMode floorHeightAdjustMode))
            {
                return null;
            }

            return floorHeightAdjustMode switch
            {
                FloorHeightAdjustMode.Off => "Off",
                FloorHeightAdjustMode.PlayersPlaceOnly => "Player's Place Only",
                FloorHeightAdjustMode.EntireEnvironment => "Entire Environment",
                _ => null,
            };
        }

        private string HeightFormatter(float value)
        {
            return $"{value + BeatSaberUtilities.kHeadPosToPlayerHeightOffset:0.00} m";
        }

        private void OnMeasureHeightButtonClicked()
        {
            if (InputDevices.GetDeviceAtXRNode(XRNode.Head).TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            {
                this.height = Mathf.Round(position.y * 100) / 100;
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
