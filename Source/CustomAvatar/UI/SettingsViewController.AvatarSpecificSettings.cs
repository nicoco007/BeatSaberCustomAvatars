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
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CustomAvatar.Tracking;

namespace CustomAvatar.UI
{
    internal partial class SettingsViewController
    {
        #region Components
        #pragma warning disable 649
        #pragma warning disable IDE0044

        [UIComponent("arm-span")] private TextMeshProUGUI _armSpanLabel;
        [UIComponent("calibrate-button")] private TextMeshProUGUI _calibrateButtonText;
        [UIComponent("clear-button")] private TextMeshProUGUI _clearButtonText;
        [UIComponent("automatic-calibration")] private HoverHint _automaticCalibrationHoverHint;

        [UIComponent("ignore-exclusions")] private ToggleSetting _ignoreExclusionsSetting;
        [UIComponent("bypass-calibration")] private ToggleSetting _bypassCalibration;
        [UIComponent("automatic-calibration")] private ToggleSetting _automaticCalibrationSetting;

        [UIComponent("calibrate-button")] private Button _calibrateButton;
        [UIComponent("clear-button")] private Button _clearButton;

        [UIComponent("calibrate-button")] private HoverHint _calibrateButtonHoverHint;

        #pragma warning restore IDE0044
        #pragma warning restore 649
        #endregion

        private GameObject _waistSphere;
        private GameObject _leftFootSphere;
        private GameObject _rightFootSphere;

        #region Actions

        [UIAction("ignore-exclusions-change")]
        private void OnIgnoreExclusionsChanged(bool value)
        {
            if (_currentAvatarSettings == null) return;

            _currentAvatarSettings.ignoreExclusions = value;
            _avatarManager.UpdateFirstPersonVisibility();
        }

        [UIAction("bypass-calibration-change")]
        private void OnEnableBypassCalibrationChanged(bool value)
        {
            if (_currentAvatarSettings == null) return;

            _currentAvatarSettings.bypassCalibration.value = value;
        }

        [UIAction("automatic-calibration-change")]
        private void OnEnableAutomaticCalibrationChanged(bool value)
        {
            DisableCalibrationMode(false);

            if (_currentAvatarSettings == null) return;

            _currentAvatarSettings.useAutomaticCalibration.value = value;
        }

        [UIAction("calibrate-fbt-click")]
        private void OnCalibrateFullBodyTrackingClicked()
        {
            if (!_calibrating)
            {
                EnableCalibrationMode();
            }
            else
            {
                DisableCalibrationMode(true);
            }
        }

        [UIAction("clear-fbt-calibration-data-click")]
        private void OnClearFullBodyTrackingCalibrationDataClicked()
        {
            if (_calibrating)
            {
                DisableCalibrationMode(false);
            }
            else
            {
                _playerInput.ClearManualFullBodyTrackingData(_avatarManager.currentlySpawnedAvatar);
                _clearButton.interactable = false;
            }
        }

        #endregion

        private void EnableCalibrationMode()
        {
            if (!_avatarManager.currentlySpawnedAvatar) return;

            _avatarManager.currentlySpawnedAvatar.EnableCalibrationMode();
            _calibrating = true;

            UpdateCalibrationButtons(_avatarManager.currentlySpawnedAvatar.avatar);

            _waistSphere = CreateCalibrationSphere();
            _leftFootSphere = CreateCalibrationSphere();
            _rightFootSphere = CreateCalibrationSphere();
        }

        private void DisableCalibrationMode(bool save)
        {
            _calibrating = false;

            Destroy(_waistSphere);
            Destroy(_leftFootSphere);
            Destroy(_rightFootSphere);

            if (!_avatarManager.currentlySpawnedAvatar) return;

            if (save)
            {
                _playerInput.CalibrateFullBodyTrackingManual(_avatarManager.currentlySpawnedAvatar);

                _automaticCalibrationSetting.Value = false;
                OnEnableAutomaticCalibrationChanged(false);
            }

            _avatarManager.currentlySpawnedAvatar.DisableCalibrationMode();

            UpdateCalibrationButtons(_avatarManager.currentlySpawnedAvatar.avatar);
        }

        private GameObject CreateCalibrationSphere()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.layer = AvatarLayers.kAlwaysVisible;
            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.GetComponent<Renderer>().material = _sphereMaterial;

            CreateAxis(new Vector3(0.5f, 0, 0), new Vector3(1f, 0.1f, 0.1f), _redMaterial, sphere.transform);
            CreateAxis(new Vector3(0, 0.5f, 0), new Vector3(0.1f, 1f, 0.1f), _greenMaterial, sphere.transform);
            CreateAxis(new Vector3(0, 0, 0.5f), new Vector3(0.1f, 0.1f, 1f), _blueMaterial, sphere.transform);

            return sphere;
        }

        private void CreateAxis(Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject axis = GameObject.CreatePrimitive(PrimitiveType.Cube);

            axis.layer = AvatarLayers.kAlwaysVisible;
            axis.transform.localPosition = position;
            axis.transform.localScale = scale;
            axis.GetComponent<Renderer>().material = material;
            axis.transform.SetParent(parent, false);
        }

        private void Update()
        {
            if (_calibrating)
            {
                if (_playerInput.TryGetUncalibratedPoseForAvatar(DeviceUse.Waist, _avatarManager.currentlySpawnedAvatar, out Pose waist))
                {
                    _waistSphere.SetActive(true);
                    _waistSphere.transform.position = waist.position;
                    _waistSphere.transform.rotation = waist.rotation;
                }
                else
                {
                    _waistSphere.SetActive(false);
                }

                if (_playerInput.TryGetUncalibratedPoseForAvatar(DeviceUse.LeftFoot, _avatarManager.currentlySpawnedAvatar, out Pose leftFoot))
                {
                    _leftFootSphere.SetActive(true);
                    _leftFootSphere.transform.position = leftFoot.position;
                    _leftFootSphere.transform.rotation = leftFoot.rotation;
                }
                else
                {
                    _leftFootSphere.SetActive(false);
                }

                if (_playerInput.TryGetUncalibratedPoseForAvatar(DeviceUse.RightFoot, _avatarManager.currentlySpawnedAvatar, out Pose rightFoot))
                {
                    _rightFootSphere.SetActive(true);
                    _rightFootSphere.transform.position = rightFoot.position;
                    _rightFootSphere.transform.rotation = rightFoot.rotation;
                }
                else
                {
                    _rightFootSphere.SetActive(false);
                }
            }
        }
    }
}
