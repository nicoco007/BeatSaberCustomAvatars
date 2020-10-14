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

using CustomAvatar.Avatar;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [UIComponent("bypass-calibration")] private HoverHint _bypassCalibrationHoverHint;
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
            _currentAvatarSettings.ignoreExclusions = value;
            _avatarManager.UpdateFirstPersonVisibility();
        }

        [UIAction("bypass-calibration-change")]
        private void OnEnableBypassCalibrationChanged(bool value)
        {
            _currentAvatarSettings.bypassCalibration = value;
        }

        [UIAction("automatic-calibration-change")]
        private void OnEnableAutomaticCalibrationChanged(bool value)
        {
            DisableCalibrationMode(false);
            _currentAvatarSettings.useAutomaticCalibration = value;
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
                _avatarTailor.ClearManualFullBodyTrackingData(_avatarManager.currentlySpawnedAvatar);
                _clearButton.interactable = false;
            }
        }

        #endregion

        private void EnableCalibrationMode()
        {
            _avatarManager.currentlySpawnedAvatar.EnableCalibrationMode();
            _calibrating = true;
            _calibrateButtonText.text = "Save";
            _clearButtonText.text = "Cancel";
            _clearButton.interactable = true;

            _waistSphere = CreateCalibrationSphere();
            _leftFootSphere = CreateCalibrationSphere();
            _rightFootSphere = CreateCalibrationSphere();
        }

        private void DisableCalibrationMode(bool save)
        {
            if (_avatarManager.currentlySpawnedAvatar != null)
            {
                if (save)
                {
                    _avatarTailor.CalibrateFullBodyTrackingManual(_avatarManager.currentlySpawnedAvatar);

                    _automaticCalibrationSetting.Value = false;
                    OnEnableAutomaticCalibrationChanged(false);
                }

                _avatarManager.currentlySpawnedAvatar.DisableCalibrationMode();
            }

            Destroy(_waistSphere);
            Destroy(_leftFootSphere);
            Destroy(_rightFootSphere);

            _calibrating = false;
            _calibrateButtonText.text = "Calibrate";
            _clearButtonText.text = "Clear";
            _clearButton.interactable = _currentAvatarManualCalibration?.isCalibrated ?? false;
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
                if (_trackedDeviceManager.waist.tracked)
                {
                    _waistSphere.SetActive(true);
                    _waistSphere.transform.position = _avatarTailor.ApplyTrackedPointFloorOffset(_avatarManager.currentlySpawnedAvatar, _trackedDeviceManager.waist.position);
                    _waistSphere.transform.rotation = _trackedDeviceManager.waist.rotation;
                }
                else
                {
                    _waistSphere.SetActive(false);
                }

                if (_trackedDeviceManager.leftFoot.tracked)
                {
                    _leftFootSphere.SetActive(true);
                    _leftFootSphere.transform.position = _avatarTailor.ApplyTrackedPointFloorOffset(_avatarManager.currentlySpawnedAvatar, _trackedDeviceManager.leftFoot.position);
                    _leftFootSphere.transform.rotation = _trackedDeviceManager.leftFoot.rotation;
                }
                else
                {
                    _leftFootSphere.SetActive(false);
                }

                if (_trackedDeviceManager.rightFoot.tracked)
                {
                    _rightFootSphere.SetActive(true);
                    _rightFootSphere.transform.position = _avatarTailor.ApplyTrackedPointFloorOffset(_avatarManager.currentlySpawnedAvatar, _trackedDeviceManager.rightFoot.position);
                    _rightFootSphere.transform.rotation = _trackedDeviceManager.rightFoot.rotation;
                }
                else
                {
                    _rightFootSphere.SetActive(false);
                }
            }
        }
    }
}
