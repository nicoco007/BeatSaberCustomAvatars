//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

extern alias BeatSaberFinalIK;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using CustomAvatar.Rendering.Cameras;
using CustomAvatar.Utilities;
using JetBrains.Annotations;
using SiraUtil.Tools.FPFC;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR;
using VRUIControls;
using Zenject;

namespace CustomAvatar.Tracking
{
    [DisallowMultipleComponent]
    internal class TrackingRig : MonoBehaviour
    {
        private const float kMinPressValue = VRInputModule.kMinPressValue;

        private static readonly List<ConstraintSource> kEmptyConstraintSources = new(0);

        private ILogger<TrackingRig> _logger;
        private DiContainer _container;
        private IDeviceProvider _deviceProvider;
        private IRenderModelProvider _renderModelProvider;
        private PlayerAvatarManager _playerAvatarManager;
        private ActiveCameraManager _activeCameraManager;
        private Settings _settings;
        private CalibrationData _calibrationData;
        private VRControllerVisualsManager _vrControllerVisualsManager;
        private BeatSaberUtilities _beatSaberUtilities;
        private IFPFCSettings _fpfcSettings;
        private HumanoidCalibrator _humanoidCalibrator;

        private ParentConstraint _parentConstraint;
        private LossyScaleConstraint _scaleConstraint;
        private CalibrationMode _activeCalibrationMode;

        private bool _showRenderModels;
        private TrackedRenderModel _leftHandRenderModel;
        private TrackedRenderModel _rightHandRenderModel;
        private TrackedRenderModel _pelvisRenderModel;
        private TrackedRenderModel _leftFootRenderModel;
        private TrackedRenderModel _rightFootRenderModel;

        internal event Action trackingChanged;

        internal event Action<CalibrationMode> activeCalibrationModeChanged;

        internal event Action<CalibrationMode> calibrationModeChanged;

        internal bool areBothHandsTracking { get; private set; }

        internal bool areAnyFullBodyTrackersTracking { get; private set; }

        // local to the current active origin (parent of VRCenterAdjust or world if no parent)
        internal float eyeHeight => (_activeCameraManager.current != null && _activeCameraManager.current.origin != null ? _activeCameraManager.current.origin.InverseTransformPoint(head.transform.position).y : head.transform.position.y) - (_settings.moveFloorWithRoomAdjust ? _beatSaberUtilities.roomCenter.y : 0);

        internal GenericNode head { get; private set; }

        internal ControllerNode leftHand { get; private set; }

        internal ControllerNode rightHand { get; private set; }

        internal GenericNode pelvis { get; private set; }

        internal GenericNode leftFoot { get; private set; }

        internal GenericNode rightFoot { get; private set; }

        internal Transform fullBodyTracking { get; private set; }

        internal CalibrationMode activeCalibrationMode
        {
            get => _activeCalibrationMode;
            private set
            {
                _activeCalibrationMode = value;
                activeCalibrationModeChanged?.Invoke(value);
            }
        }

        internal CalibrationMode calibrationMode
        {
            get => _playerAvatarManager != null && _playerAvatarManager.currentAvatarSettings != null ? _playerAvatarManager.currentAvatarSettings.calibrationMode : CalibrationMode.None;
            set
            {
                if (_playerAvatarManager != null && _playerAvatarManager.currentAvatarSettings != null)
                {
                    _playerAvatarManager.currentAvatarSettings.calibrationMode = value;
                }

                UpdateOffsets();
                UpdateNodeStates();
                calibrationModeChanged?.Invoke(value);
            }
        }

        internal bool areRenderModelsAvailable => _renderModelProvider != null;

        internal bool showRenderModels
        {
            get => _showRenderModels;
            set
            {
                _showRenderModels = value;
                UpdateRenderModelsVisibility();
            }
        }

        internal void BeginCalibration(CalibrationMode calibrationMode)
        {
            if (calibrationMode == CalibrationMode.None)
            {
                return;
            }

            SetIKEnabled(false);

            activeCalibrationMode = calibrationMode;
        }

        internal void EndCalibration()
        {
            SetIKEnabled(true);

            activeCalibrationMode = CalibrationMode.None;

            _playerAvatarManager.ResizeCurrentAvatar();

            trackingChanged?.Invoke();
        }

        internal void ClearCalibrationData(CalibrationMode calibrationMode)
        {
            switch (calibrationMode)
            {
                case CalibrationMode.Automatic:
                    _humanoidCalibrator.ClearAutomaticFullBodyTrackingData();
                    break;

                case CalibrationMode.Manual:
                    if (_playerAvatarManager.currentlySpawnedAvatar != null)
                    {
                        _humanoidCalibrator.ClearManualFullBodyTrackingData();
                    }

                    break;
            }

            UpdateOffsets();
            UpdateNodeStates();
        }

        protected void Awake()
        {
            _parentConstraint = gameObject.AddComponent<ParentConstraint>();
            _parentConstraint.weight = 1;
            _parentConstraint.constraintActive = true;

            _scaleConstraint = gameObject.AddComponent<LossyScaleConstraint>();

            head = GenericNode.Create("Head", transform);
            leftHand = ControllerNode.Create("Left Hand", transform);
            rightHand = ControllerNode.Create("Right Hand", transform);

            fullBodyTracking = new GameObject("Full Body Tracking").transform;
            fullBodyTracking.SetParent(transform, false);

            pelvis = GenericNode.Create("Pelvis", fullBodyTracking);
            leftFoot = GenericNode.Create("Left Foot", fullBodyTracking);
            rightFoot = GenericNode.Create("Right Foot", fullBodyTracking);

            if (_renderModelProvider != null)
            {
                _leftHandRenderModel = CreateRenderModelObject("Left Hand Render Model");
                _rightHandRenderModel = CreateRenderModelObject("Right Hand Render Model");
                _pelvisRenderModel = CreateRenderModelObject("Pelvis Render Model");
                _leftFootRenderModel = CreateRenderModelObject("Left Foot Render Model");
                _rightFootRenderModel = CreateRenderModelObject("Right Foot Render Model");

                _leftHandRenderModel.transform.SetParent(leftHand.transform, false);
                _rightHandRenderModel.transform.SetParent(rightHand.transform, false);
                _pelvisRenderModel.transform.SetParent(pelvis.transform, false);
                _leftFootRenderModel.transform.SetParent(leftFoot.transform, false);
                _rightFootRenderModel.transform.SetParent(rightFoot.transform, false);
            }
        }

        protected void OnEnable()
        {
            if (_activeCameraManager != null)
            {
                _activeCameraManager.changed += OnActiveCameraChanged;
                OnActiveCameraChanged(_activeCameraManager.current);
            }

            if (_playerAvatarManager != null)
            {
                _playerAvatarManager.avatarChanged += OnAvatarChanged;
                _playerAvatarManager.avatarScaleChanged += OnAvatarScaleChanged;
            }

            if (_settings != null)
            {
                _settings.playerEyeHeight.changed += OnPlayerEyeHeightChanged;
                _settings.showRenderModels.changed += OnShowRenderModelsChanged;
            }

            if (_beatSaberUtilities != null)
            {
                _beatSaberUtilities.roomAdjustChanged += OnRoomAdjustChanged;
            }

            if (_deviceProvider != null)
            {
                _deviceProvider.devicesChanged += OnDevicesChanged;
            }

            UpdateOffsets();
            UpdateNodeStates();
            UpdateRenderModels();
            UpdateRenderModelsVisibility();
        }

        protected void Start()
        {
            VRControllersValueSettingsOffsets localPlayerControllerOffset = _container.InstantiateComponent<VRControllersValueSettingsOffsets>(gameObject);

            // UnityXRHelper sets things up in Start(), so we have to wait until then for VRController since it calls IVRPlatformHelper stuff in OnEnable().
            SetUpVRController(leftHand, XRNode.LeftHand, localPlayerControllerOffset);
            SetUpVRController(rightHand, XRNode.RightHand, localPlayerControllerOffset);

            if (_fpfcSettings != null)
            {
                _fpfcSettings.Changed += OnFpfcSettingsChanged;
            }

            if (_beatSaberUtilities != null)
            {
                _beatSaberUtilities.focusChanged += OnFocusChanged;
            }

            UpdateBehaviourEnabled();
            UpdateOffsets();
        }

        [Inject]
        [UsedImplicitly]
        private void Construct(
            ILogger<TrackingRig> logger,
            DiContainer container,
            IDeviceProvider deviceProvider,
            [InjectOptional] IRenderModelProvider renderModelProvider,
            PlayerAvatarManager playerAvatarManager,
            ActiveCameraManager activeCameraManager,
            Settings settings,
            CalibrationData calibrationData,
            VRControllerVisualsManager vrControllerVisualsManager,
            BeatSaberUtilities beatSaberUtilities,
            IFPFCSettings fpfcSettings)
        {
            _container = container;
            _logger = logger;
            _deviceProvider = deviceProvider;
            _renderModelProvider = renderModelProvider;
            _playerAvatarManager = playerAvatarManager;
            _activeCameraManager = activeCameraManager;
            _settings = settings;
            _calibrationData = calibrationData;
            _vrControllerVisualsManager = vrControllerVisualsManager;
            _beatSaberUtilities = beatSaberUtilities;
            _fpfcSettings = fpfcSettings;
            _humanoidCalibrator = new HumanoidCalibrator(this, calibrationData, settings, activeCameraManager, beatSaberUtilities, playerAvatarManager);
        }

        protected void Update()
        {
            UpdateTransform(DeviceUse.Head, head);
            UpdateTransform(DeviceUse.Waist, pelvis);
            UpdateTransform(DeviceUse.LeftFoot, leftFoot);
            UpdateTransform(DeviceUse.RightFoot, rightFoot);

            if (activeCalibrationMode != CalibrationMode.None)
            {
                _playerAvatarManager.ResizeCurrentAvatar(eyeHeight);

                // need to use trigger rather than triggerButton since the latter is not bound on all controller types with SteamVR
                if (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.trigger, out float leftTriggerValue) &&
                    InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue) &&
                    leftTriggerValue >= kMinPressValue && rightTriggerValue >= kMinPressValue)
                {
                    switch (activeCalibrationMode)
                    {
                        case CalibrationMode.Automatic:
                            _humanoidCalibrator.CalibrateAutomatically();
                            break;

                        case CalibrationMode.Manual:
                            _humanoidCalibrator.CalibrateManually();
                            break;
                    }

                    calibrationMode = activeCalibrationMode;

                    EndCalibration();
                }
            }
        }

        protected void OnDisable()
        {
            if (_activeCameraManager != null)
            {
                _activeCameraManager.changed -= OnActiveCameraChanged;
                OnActiveCameraChanged(null);
            }

            if (_playerAvatarManager != null)
            {
                _playerAvatarManager.avatarChanged -= OnAvatarChanged;
                _playerAvatarManager.avatarScaleChanged -= OnAvatarScaleChanged;
            }

            if (_settings != null)
            {
                _settings.playerEyeHeight.changed -= OnPlayerEyeHeightChanged;
                _settings.showRenderModels.changed -= OnShowRenderModelsChanged;
            }

            if (_beatSaberUtilities != null)
            {
                _beatSaberUtilities.roomAdjustChanged -= OnRoomAdjustChanged;
            }

            if (_deviceProvider != null)
            {
                _deviceProvider.devicesChanged -= OnDevicesChanged;
            }
        }

        protected void OnDestroy()
        {
            if (_fpfcSettings != null)
            {
                _fpfcSettings.Changed -= OnFpfcSettingsChanged;
            }

            if (_beatSaberUtilities != null)
            {
                _beatSaberUtilities.focusChanged -= OnFocusChanged;
            }
        }

        private VRController SetUpVRController(ControllerNode trackedNode, XRNode node, VRControllerTransformOffset transformOffset)
        {
            trackedNode.gameObject.SetActive(false);

            VRController vrController = _container.InstantiateComponent<VRController>(trackedNode.gameObject);
            vrController._node = node;
            vrController._viewAnchorTransform = trackedNode.viewTransform;
            vrController._transformOffset = transformOffset;

            trackedNode.controller = vrController;
            trackedNode.gameObject.SetActive(true);

            return vrController;
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            EndCalibration();
            UpdateOffsets();
            UpdateNodeStates();
            calibrationModeChanged?.Invoke(calibrationMode);
        }

        private void OnAvatarScaleChanged(float scale)
        {
            UpdateOffsets();
        }

        private void OnPlayerEyeHeightChanged(float eyeHeight)
        {
            UpdateOffsets();
        }

        private void OnShowRenderModelsChanged(bool show)
        {
            UpdateRenderModelsVisibility();
        }

        private void OnRoomAdjustChanged(Vector3 position, Quaternion rotation)
        {
            UpdateOffsets();
        }

        private void OnDevicesChanged()
        {
            UpdateNodeStates();
            UpdateRenderModels();
            trackingChanged?.Invoke();
        }

        private void OnFpfcSettingsChanged(IFPFCSettings fpfcSettings)
        {
            UpdateBehaviourEnabled();
        }

        private void OnFocusChanged(bool hasFocus)
        {
            UpdateBehaviourEnabled();
            UpdateRenderModelsVisibility();
        }

        private void UpdateBehaviourEnabled()
        {
            bool enabled = _beatSaberUtilities.hasFocus && !_fpfcSettings.Enabled;
            this.enabled = enabled;
            this.leftHand.controller.enabled = enabled;
            this.rightHand.controller.enabled = enabled;
        }

        private void UpdateOffsets()
        {
            if (_playerAvatarManager == null)
            {
                return;
            }

            SpawnedAvatar spawnedAvatar = _playerAvatarManager.currentlySpawnedAvatar;

            if (spawnedAvatar == null)
            {
                return;
            }

            _logger.LogTrace("Updating offsets");

            UpdateOffset(leftHand.offset, spawnedAvatar.prefab.leftHandOffset, _playerAvatarManager.scale);
            UpdateOffset(rightHand.offset, spawnedAvatar.prefab.rightHandOffset, _playerAvatarManager.scale);

            float fullBodyScale = _playerAvatarManager.scaledEyeHeight / _settings.playerEyeHeight;
            float trackerScale = _playerAvatarManager.scale / fullBodyScale;

            CalibrationData.FullBodyCalibration automaticCalibration = _calibrationData.automaticCalibration;
            CalibrationData.FullBodyCalibration manualCalibration = _playerAvatarManager.currentManualCalibration;

            UpdateOffset(head.offset, spawnedAvatar.prefab.headOffset, spawnedAvatar.prefab.headCalibrationOffset, _playerAvatarManager.scale, manualCalibration.head, automaticCalibration.head);
            UpdateOffset(pelvis.offset, spawnedAvatar.prefab.pelvisOffset, spawnedAvatar.prefab.pelvisCalibrationOffset, trackerScale, manualCalibration.waist, automaticCalibration.waist);
            UpdateOffset(leftFoot.offset, spawnedAvatar.prefab.leftLegOffset, spawnedAvatar.prefab.leftFootCalibrationOffset, trackerScale, manualCalibration.leftFoot, automaticCalibration.leftFoot);
            UpdateOffset(rightFoot.offset, spawnedAvatar.prefab.rightLegOffset, spawnedAvatar.prefab.rightFootCalibrationOffset, trackerScale, manualCalibration.rightFoot, automaticCalibration.rightFoot);

            switch (calibrationMode)
            {
                case CalibrationMode.None:
                    _humanoidCalibrator.ApplyNoCalibration();
                    break;

                case CalibrationMode.Manual:
                    _humanoidCalibrator.ApplyManualCalibration();
                    break;

                case CalibrationMode.Automatic:
                    _humanoidCalibrator.ApplyAutomaticCalibration();
                    break;
            }

            fullBodyTracking.localPosition = new Vector3(0, _settings.playerEyeHeight - _playerAvatarManager.scaledEyeHeight, 0);
            fullBodyTracking.localScale = new Vector3(1, fullBodyScale, 1);
        }

        private void UpdateOffset(Transform transform, Pose manualOffset, Pose automaticOffset, float scale, Pose manualCalibration, Pose automaticCalibration)
        {
            switch (calibrationMode)
            {
                case CalibrationMode.None:
                    UpdateOffset(transform, manualOffset, scale);
                    break;

                case CalibrationMode.Manual:
                    if (!manualCalibration.Equals(Pose.identity))
                    {
                        UpdateOffset(transform, Pose.identity, 1);
                    }
                    else
                    {
                        UpdateOffset(transform, manualOffset, scale);
                    }

                    break;

                case CalibrationMode.Automatic:
                    if (!automaticCalibration.Equals(Pose.identity))
                    {
                        UpdateOffset(transform, automaticOffset, scale);
                    }
                    else
                    {
                        UpdateOffset(transform, manualOffset, scale);
                    }

                    break;
            }
        }

        private void UpdateOffset(Transform transform, Pose offset, float scale)
        {
            transform.SetLocalPositionAndRotation(offset.position * scale, offset.rotation);
        }

        private void OnActiveCameraChanged(CameraTracker activeCamera)
        {
            _logger.LogTrace("Updating constraints");

            if (activeCamera != null && activeCamera.playerSpace != null)
            {
                _parentConstraint.SetSources([new ConstraintSource { sourceTransform = activeCamera.playerSpace, weight = 1 }]);
                _scaleConstraint.sourceTransform = activeCamera.playerSpace;
            }
            else
            {
                _parentConstraint.SetSources(kEmptyConstraintSources);
                _scaleConstraint.sourceTransform = null;
            }
        }

        private void UpdateTransform(DeviceUse deviceUse, GenericNode trackedNode)
        {
            if (_deviceProvider.TryGetDeviceState(deviceUse, out DeviceState deviceState) && deviceState.isTracking)
            {
                trackedNode.transform.SetLocalPositionAndRotation(deviceState.position, deviceState.rotation);
            }
            else
            {
                trackedNode.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        private void SetIKEnabled(bool enabled)
        {
            if (_playerAvatarManager != null && _playerAvatarManager.currentlySpawnedAvatar != null && _playerAvatarManager.currentlySpawnedAvatar.ik != null)
            {
                _playerAvatarManager.currentlySpawnedAvatar.ik.enabled = enabled;
            }
        }

        private void UpdateNodeStates()
        {
            if (_deviceProvider == null)
            {
                return;
            }

            _logger.LogTrace("Updating active transforms");

            UpdateNodeState(head, DeviceUse.Head);
            UpdateNodeState(leftHand, DeviceUse.LeftHand);
            UpdateNodeState(rightHand, DeviceUse.RightHand);
            UpdateNodeState(pelvis, DeviceUse.Waist);
            UpdateNodeState(leftFoot, DeviceUse.LeftFoot);
            UpdateNodeState(rightFoot, DeviceUse.RightFoot);

            areBothHandsTracking = leftHand.isTracking && rightHand.isTracking;
            areAnyFullBodyTrackersTracking = pelvis.isTracking || leftFoot.isTracking || rightFoot.isTracking;

            trackingChanged?.Invoke();
        }

        private void UpdateNodeState(GenericNode trackedNode, DeviceUse deviceUse)
        {
            trackedNode.isTracking = _deviceProvider.TryGetDeviceState(deviceUse, out DeviceState deviceState) && deviceState.isTracking;
            trackedNode.isCalibrated = IsCurrentlyCalibrated(deviceUse);
        }

        private void UpdateNodeState(ControllerNode trackedController, DeviceUse deviceUse)
        {
            // To mimic what VRController does, we only check if the device is connected.
            trackedController.isTracking = _deviceProvider.TryGetDeviceState(deviceUse, out DeviceState deviceState) && deviceState.isConnected;
        }

        private bool IsCurrentlyCalibrated(DeviceUse deviceUse)
        {
            if (deviceUse is not DeviceUse.Waist and not DeviceUse.LeftFoot and not DeviceUse.RightFoot)
            {
                return true;
            }

            CalibrationData.FullBodyCalibration calibration;

            switch (calibrationMode)
            {
                case CalibrationMode.Automatic:
                    calibration = _calibrationData.automaticCalibration;
                    break;

                case CalibrationMode.Manual:
                    calibration = _playerAvatarManager.currentManualCalibration;
                    break;

                default:
                    return true;
            }

            Pose pose = deviceUse switch
            {
                DeviceUse.Waist => calibration.waist,
                DeviceUse.LeftFoot => calibration.leftFoot,
                DeviceUse.RightFoot => calibration.rightFoot,
                _ => Pose.identity,
            };

            return !pose.Equals(Pose.identity);
        }

        private TrackedRenderModel CreateRenderModelObject(string name)
        {
            GameObject renderModelGo = new(name);
            renderModelGo.SetActive(false);
            return new TrackedRenderModel(renderModelGo, renderModelGo.AddComponent<MeshFilter>(), renderModelGo.AddComponent<MeshRenderer>());
        }

        private void UpdateRenderModelsVisibility()
        {
            if (_renderModelProvider == null)
            {
                return;
            }

            bool show = _showRenderModels && _settings.showRenderModels && _beatSaberUtilities.hasFocus;
            _leftHandRenderModel?.SetActive(show);
            _rightHandRenderModel?.SetActive(show);
            _pelvisRenderModel?.SetActive(show);
            _leftFootRenderModel?.SetActive(show);
            _rightFootRenderModel?.SetActive(show);
            _vrControllerVisualsManager.SetHandlesActive(!show);
        }

        private void UpdateRenderModels()
        {
            if (_renderModelProvider == null)
            {
                return;
            }

            UpdateRenderModelsAsync().ContinueWith((task) => _logger.LogError(task.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        private Task UpdateRenderModelsAsync()
        {
            _logger.LogTrace("Updating render models");

            return Task.WhenAll(
                UpdateRenderModelAsync(DeviceUse.LeftHand, leftHand, _leftHandRenderModel),
                UpdateRenderModelAsync(DeviceUse.RightHand, rightHand, _rightHandRenderModel),
                UpdateRenderModelAsync(DeviceUse.Waist, pelvis, _pelvisRenderModel),
                UpdateRenderModelAsync(DeviceUse.LeftFoot, leftFoot, _leftFootRenderModel),
                UpdateRenderModelAsync(DeviceUse.RightFoot, rightFoot, _rightFootRenderModel));
        }

        private async Task UpdateRenderModelAsync(DeviceUse deviceUse, ITrackedNode trackedNode, TrackedRenderModel trackedRenderModel)
        {
            RenderModel renderModel;

            if (trackedNode.isTracking && (renderModel = await _renderModelProvider.GetRenderModelAsync(deviceUse)) != null)
            {
                trackedRenderModel.transform.SetLocalPositionAndRotation(renderModel.localOrigin.position, renderModel.localOrigin.rotation);
                trackedRenderModel.meshFilter.sharedMesh = renderModel.mesh;
                trackedRenderModel.meshRenderer.sharedMaterial = renderModel.material;
            }
            else
            {
                trackedRenderModel.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                trackedRenderModel.meshFilter.sharedMesh = null;
                trackedRenderModel.meshRenderer.sharedMaterial = null;
            }
        }
    }
}
