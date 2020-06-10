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
        [UIComponent("automatic-calibration")] private HoverHint _automaticCalibrationHoverHint;

        [UIComponent("automatic-calibration")] private BoolSetting _automaticCalibrationSetting;

        [UIComponent("calibrate-button")] private Button _calibrateButton;
        [UIComponent("clear-button")] private Button _clearButton;

        #pragma warning restore IDE0044
        #pragma warning restore 649
        #endregion

        private GameObject _waistSphere;
        private GameObject _leftFootSphere;
        private GameObject _rightFootSphere;

        #region Actions
        // ReSharper disable UnusedMember.Local

        [UIAction("automatic-calibration-change")]
        private void OnAutomaticCalibrationChanged(bool value)
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
        
        // ReSharper restore UnusedMember.Local
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
                }
                
                _avatarManager.currentlySpawnedAvatar.DisableCalibrationMode();
            }

            Destroy(_waistSphere);
            Destroy(_leftFootSphere);
            Destroy(_rightFootSphere);

            _calibrating = false;
            _calibrateButtonText.text = "Calibrate";
            _clearButtonText.text = "Clear";
            _clearButton.interactable = _currentAvatarSettings?.fullBodyCalibration.isCalibrated ?? false;
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
                    _waistSphere.transform.position = _trackedDeviceManager.waist.position;
                    _waistSphere.transform.rotation = _trackedDeviceManager.waist.rotation;
                }
                else
                {
                    _waistSphere.SetActive(false);
                }

                if (_trackedDeviceManager.leftFoot.tracked)
                {
                    _leftFootSphere.SetActive(true);
                    _leftFootSphere.transform.position = _trackedDeviceManager.leftFoot.position;
                    _leftFootSphere.transform.rotation = _trackedDeviceManager.leftFoot.rotation;
                }
                else
                {
                    _leftFootSphere.SetActive(false);
                }

                if (_trackedDeviceManager.rightFoot.tracked)
                {
                    _rightFootSphere.SetActive(true);
                    _rightFootSphere.transform.position = _trackedDeviceManager.rightFoot.position;
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
