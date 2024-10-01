//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;

namespace CustomAvatar.UI
{
    internal class GeneralSettingsHost : ViewControllerHost
    {
        #region Values

        protected readonly AvatarResizeMode[] resizeModeOptions = [AvatarResizeMode.None, AvatarResizeMode.Height, AvatarResizeMode.ArmSpan];
        protected readonly FloorHeightAdjustMode[] floorHeightAdjustOptions = [FloorHeightAdjustMode.Off, FloorHeightAdjustMode.PlayersPlaceOnly, FloorHeightAdjustMode.EntireEnvironment];
        protected readonly float[] nearClipPlaneValues = [0.001f, 0.01f, 0.02f, 0.03f, 0.04f, 0.05f, 0.06f, 0.07f, 0.08f, 0.09f, 0.1f];

        #endregion

        private readonly Settings _settings;
        private readonly TrackingRig _trackingRig;
        private readonly ArmSpanMeasurer _armSpanMeasurer;

        private float _armSpan;

        internal GeneralSettingsHost(Settings settings, TrackingRig trackingRig, ArmSpanMeasurer armSpanMeasurer)
        {
            _settings = settings;
            _trackingRig = trackingRig;
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

        public bool showRenderModelsOption => _trackingRig.areRenderModelsAvailable;

        public bool showRenderModels
        {
            get => _settings.showRenderModels;
            set
            {
                _settings.showRenderModels.value = value;
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

        public bool isMeasureButtonEnabled => _trackingRig.areBothHandsTracking;

        // TODO: find or create a better cancel icon
        public string measureButtonIcon => _armSpanMeasurer.isMeasuring ? "#ResetIcon" : "#MeasureIcon";

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
            _trackingRig.trackingChanged += OnTrackingChanged;
            _settings.playerEyeHeight.changed += OnPlayerEyeHeightChanged;

            _armSpan = _settings.playerArmSpan;
            NotifyPropertyChanged(nameof(armSpan));

            OnTrackingChanged();
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _armSpanMeasurer.updated -= OnArmSpanMeasurementChanged;
            _armSpanMeasurer.completed -= OnArmSpanMeasurementCompleted;
            _trackingRig.trackingChanged -= OnTrackingChanged;
            _settings.playerEyeHeight.changed -= OnPlayerEyeHeightChanged;

            _trackingRig.EndCalibration();
        }

        private void OnTrackingChanged()
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
            NotifyPropertyChanged(nameof(measureButtonIcon));
        }

        private void OnPlayerEyeHeightChanged(float eyeHeight)
        {
            NotifyPropertyChanged(nameof(height));
        }

        #region Actions
#pragma warning disable IDE0051

        private string ResizeModeFormatter(object value)
        {
            if (value is not AvatarResizeMode avatarResizeMode)
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
            if (value is not FloorHeightAdjustMode floorHeightAdjustMode)
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

        private string CentimeterFormatter(float value)
        {
            return $"{value * 100:0.#} cm";
        }

        private string HeightFormatter(float value) => CentimeterFormatter(value + BeatSaberUtilities.kHeadPosToPlayerHeightOffset);

        private string ArmSpanFormatter(float value) => _armSpanMeasurer.isMeasuring ? $"Measuring... {CentimeterFormatter(value)}" : CentimeterFormatter(value);

        private void OnMeasureHeightButtonClicked()
        {
            this.height = _trackingRig.eyeHeight;
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
            NotifyPropertyChanged(nameof(measureButtonIcon));
        }

#pragma warning restore IDE0051
        #endregion
    }
}
