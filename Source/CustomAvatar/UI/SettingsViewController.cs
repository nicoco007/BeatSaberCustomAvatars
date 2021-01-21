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

using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using HMUI;
using Polyglot;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CustomAvatar.UI
{
    internal partial class SettingsViewController : BSMLResourceViewController
    {
        public override string ResourceName => "CustomAvatar.Views.Settings.bsml";

        private static readonly int kColor = Shader.PropertyToID("_Color");

        #region Components
        #pragma warning disable CS0649
        #pragma warning disable IDE0051

        [UIComponent("container")] private readonly RectTransform _container;
        [UIComponent("loader")] private readonly Transform _loader;

        #pragma warning restore IDE0051
        #pragma warning restore CS0649
        #endregion

        private bool _calibrating;
        private Material _sphereMaterial;
        private Material _redMaterial;
        private Material _greenMaterial;
        private Material _blueMaterial;

        private ILogger<SettingsViewController> _logger;
        private PlayerAvatarManager _avatarManager;
        private Settings _settings;
        private CalibrationData _calibrationData;
        private ShaderLoader _shaderLoader;
        private VRPlayerInput _playerInput;
        private PlayerDataModel _playerDataModel;

        private Settings.AvatarSpecificSettings _currentAvatarSettings;
        private CalibrationData.FullBodyCalibration _currentAvatarManualCalibration;

        [Inject]
        private void Inject(ILoggerProvider loggerProvider, PlayerAvatarManager avatarManager, Settings settings, CalibrationData calibrationData, ShaderLoader shaderLoader, VRPlayerInput playerInput, PlayerDataModel playerDataModel)
        {
            _logger = loggerProvider.CreateLogger<SettingsViewController>();
            _avatarManager = avatarManager;
            _settings = settings;
            _calibrationData = calibrationData;
            _shaderLoader = shaderLoader;
            _playerInput = playerInput;
            _playerDataModel = playerDataModel;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            _visibleInFirstPerson.Value = _settings.isAvatarVisibleInFirstPerson;
            _resizeMode.Value = _settings.resizeMode.value;
            _enableLocomotion.Value = _settings.enableLocomotion;
            _floorHeightAdjust.Value = _settings.floorHeightAdjust.value;
            _moveFloorWithRoomAdjust.Value = _settings.moveFloorWithRoomAdjust;
            _calibrateFullBodyTrackingOnStart.Value = _settings.calibrateFullBodyTrackingOnStart;
            _cameraNearClipPlane.Value = _settings.cameraNearClipPlane;

            _armSpanLabel.SetText($"{_settings.playerArmSpan:0.00} m");

            if (firstActivation)
            {
                if (_shaderLoader.unlitShader)
                {
                    _sphereMaterial = new Material(_shaderLoader.unlitShader);
                    _redMaterial = new Material(_shaderLoader.unlitShader);
                    _greenMaterial = new Material(_shaderLoader.unlitShader);
                    _blueMaterial = new Material(_shaderLoader.unlitShader);

                    _redMaterial.SetColor(kColor, new Color(0.8f, 0, 0, 1));
                    _greenMaterial.SetColor(kColor, new Color(0, 0.8f, 0, 1));
                    _blueMaterial.SetColor(kColor, new Color(0, 0.5f, 1, 1));
                }
                else
                {
                    _logger.Error("Unlit shader not loaded; manual calibration points may not be visible");
                }

                Transform header = Instantiate(Resources.FindObjectsOfTypeAll<GameplaySetupViewController>().First().transform.Find("HeaderPanel"), rectTransform, false);

                header.name = "HeaderPanel";

                Destroy(header.GetComponentInChildren<LocalizedTextMeshProUGUI>());
                header.GetComponentInChildren<TextMeshProUGUI>().text = "Settings";

                rectTransform.sizeDelta = new Vector2(120, 0);
                rectTransform.offsetMin = new Vector2(-60, 0);
                rectTransform.offsetMax = new Vector2(60, 0);
            }

            _pelvisOffset.Value = _settings.automaticCalibration.pelvisOffset;
            _footOffset.Value = _settings.automaticCalibration.legOffset;

            _waistTrackerPosition.Value = _settings.automaticCalibration.waistTrackerPosition;

            if (addedToHierarchy)
            {
                _avatarManager.avatarStartedLoading += OnAvatarStartedLoading;
                _avatarManager.avatarChanged += OnAvatarChanged;
                _playerInput.inputChanged += OnInputChanged;
                _settings.resizeMode.changed += OnSettingsResizeModeChanged;
            }

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);
            OnSettingsResizeModeChanged(_settings.resizeMode);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            if (removedFromHierarchy)
            {
                _avatarManager.avatarStartedLoading -= OnAvatarStartedLoading;
                _avatarManager.avatarChanged -= OnAvatarChanged;
                _playerInput.inputChanged -= OnInputChanged;
                _settings.resizeMode.changed -= OnSettingsResizeModeChanged;
            }

            DisableCalibrationMode(false);
        }

        private void OnAvatarStartedLoading(string fileName)
        {
            SetLoading(true);
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            SetLoading(false);
            DisableCalibrationMode(false);
            UpdateUI(spawnedAvatar?.avatar);
        }

        private void SetLoading(bool loading)
        {
            _loader.gameObject.SetActive(loading);
            SetInteractableRecursively(!loading);
        }

        private void SetInteractableRecursively(bool enable)
        {
            foreach (Selectable selectable in _container.GetComponentsInChildren<Selectable>(true))
            {
                selectable.interactable = enable;
                selectable.enabled = enable;
            }

            foreach (Interactable interactable in _container.GetComponentsInChildren<Interactable>(true))
            {
                interactable.interactable = enable;
                interactable.enabled = enable;
            }

            float alpha = enable ? 1 : 0.5f;

            foreach (TextMeshProUGUI textMesh in _container.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                textMesh.alpha = alpha;
            }
        }

        private void OnSettingsResizeModeChanged(AvatarResizeMode resizeMode)
        {
            _heightAdjustWarningText.gameObject.SetActive(resizeMode != AvatarResizeMode.None && _playerDataModel.playerData.playerSpecificSettings.automaticPlayerHeight);
        }

        private void UpdateUI(LoadedAvatar avatar)
        {
            SetInteractableRecursively(avatar != null);
            UpdateCalibrationButtons(avatar);

            if (avatar == null)
            {
                _clearButton.interactable = false;
                _calibrateButton.interactable = false;
                _automaticCalibrationSetting.interactable = false;
                _automaticCalibrationHoverHint.text = "No avatar selected";

                return;
            }

            _currentAvatarSettings = _settings.GetAvatarSettings(avatar.fileName);
            _currentAvatarManualCalibration = _calibrationData.GetAvatarManualCalibration(avatar.fileName);

            _ignoreExclusionsSetting.Value = _currentAvatarSettings.ignoreExclusions;

            _bypassCalibration.Value = _currentAvatarSettings.bypassCalibration;

            _automaticCalibrationSetting.Value = _currentAvatarSettings.useAutomaticCalibration;
            _automaticCalibrationSetting.interactable = avatar.descriptor.supportsAutomaticCalibration;
            _automaticCalibrationHoverHint.text = avatar.descriptor.supportsAutomaticCalibration ? "Use automatic calibration instead of manual calibration." : "Not supported by current avatar";
        }

        private void OnInputChanged()
        {
            if (_avatarManager.currentlySpawnedAvatar) UpdateCalibrationButtons(_avatarManager.currentlySpawnedAvatar.avatar);
        }

        private void UpdateCalibrationButtons(LoadedAvatar avatar)
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

            if (avatar == null)
            {
                _calibrateButton.interactable = false;
                _clearButton.interactable = false;
                _calibrateButtonHoverHint.text = "No avatar selected";
                _calibrateButtonText.text = "Calibrate";
                _clearButtonText.text = "Clear";

                _autoCalibrateButton.interactable = false;
                _autoClearButton.interactable = false;
                _autoCalibrateButtonHoverHint.text = "No avatar selected";

                return;
            }

            if (!_playerInput.TryGetUncalibratedPose(DeviceUse.Waist,     out Pose _) &&
                !_playerInput.TryGetUncalibratedPose(DeviceUse.LeftFoot,  out Pose _) &&
                !_playerInput.TryGetUncalibratedPose(DeviceUse.RightFoot, out Pose _))
            {
                _autoCalibrateButton.interactable = false;
                _autoClearButton.interactable = _calibrationData.automaticCalibration.isCalibrated;
                _autoCalibrateButtonHoverHint.text = "No trackers detected";

                _calibrateButton.interactable = false;
                _clearButton.interactable = _currentAvatarManualCalibration?.isCalibrated == true;
                _calibrateButtonHoverHint.text = "No trackers detected";
                _calibrateButtonText.text = "Calibrate";
                _clearButtonText.text = "Clear";

                return;
            }

            _calibrateButton.interactable = true;
            _clearButton.interactable = _calibrating || _currentAvatarManualCalibration?.isCalibrated == true;
            _calibrateButtonHoverHint.text = "Start manual full body calibration";
            _calibrateButtonText.text = _calibrating ? "Save" : "Calibrate";
            _clearButtonText.text = _calibrating ? "Cancel" : "Clear";

            if (avatar.descriptor.supportsAutomaticCalibration)
            {
                _autoCalibrateButton.interactable = true;
                _autoClearButton.interactable = _calibrationData.automaticCalibration.isCalibrated;
                _autoCalibrateButtonHoverHint.text = "Calibrate full body tracking automatically";
            }
            else
            {
                _autoCalibrateButton.interactable = false;
                _autoClearButton.interactable = false;
                _autoCalibrateButtonHoverHint.text = "Not supported by current avatar";
            }
        }
    }
}
