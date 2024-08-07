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

extern alias BeatSaberFinalIK;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Rendering;
using CustomAvatar.Utilities;
using SiraUtil.Tools.FPFC;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR;
using Zenject;

namespace CustomAvatar.Tracking
{
    [DisallowMultipleComponent]
    internal class TrackingRig : MonoBehaviour
    {
        private static readonly List<ConstraintSource> kEmptyConstraintSources = new(0);

        private ILogger<TrackingRig> _logger;
        private DiContainer _container;
        private IDeviceProvider _deviceProvider;
        private IRenderModelProvider _renderModelProvider;
        private PlayerAvatarManager _playerAvatarManager;
        private ActivePlayerSpaceManager _activePlayerSpaceManager;
        private ActiveOriginManager _activeOriginManager;
        private Settings _settings;
        private CalibrationData _calibrationData;
        private VRControllerVisualsManager _vrControllerVisualsManager;
        private BeatSaberUtilities _beatSaberUtilities;
        private IFPFCSettings _fpfcSettings;
        private HumanoidCalibrator _humanoidCalibrator;

        private ParentConstraint _parentConstraint;
        private ScaleConstraint _scaleConstraint;
        private CalibrationMode _activeCalibrationMode;

        private VRController _leftController;
        private VRController _rightController;

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
        internal float eyeHeight => (_activeOriginManager.current != null ? _activeOriginManager.current.InverseTransformPoint(head.transform.position).y : head.transform.position.y) - (_settings.moveFloorWithRoomAdjust ? _beatSaberUtilities.roomCenter.y : 0);

        internal TrackedNode head { get; private set; }

        internal TrackedNode leftHand { get; private set; }

        internal TrackedNode rightHand { get; private set; }

        internal TrackedNode pelvis { get; private set; }

        internal TrackedNode leftFoot { get; private set; }

        internal TrackedNode rightFoot { get; private set; }

        internal Transform fullBodyTracking { get; private set; }

        internal Transform headCalibration { get; private set; }

        internal Transform pelvisCalibration { get; private set; }

        internal Transform leftFootCalibration { get; private set; }

        internal Transform rightFootCalibration { get; private set; }

        internal Transform headOffset { get; private set; }

        internal Transform leftHandOffset { get; private set; }

        internal Transform rightHandOffset { get; private set; }

        internal Transform pelvisOffset { get; private set; }

        internal Transform leftFootOffset { get; private set; }

        internal Transform rightFootOffset { get; private set; }

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
                UpdateActiveTransforms();
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
                        _humanoidCalibrator.ClearManualFullBodyTrackingData(_playerAvatarManager.currentlySpawnedAvatar);
                    }

                    break;
            }

            UpdateOffsets();
            UpdateActiveTransforms();
        }

        private void Awake()
        {
            _parentConstraint = gameObject.AddComponent<ParentConstraint>();
            _parentConstraint.weight = 1;
            _parentConstraint.constraintActive = true;

            _scaleConstraint = gameObject.AddComponent<ScaleConstraint>();
            _scaleConstraint.weight = 1;
            _scaleConstraint.constraintActive = true;

            head = new TrackedNode(new GameObject("Head"));
            leftHand = new TrackedNode(new GameObject("Left Hand"));
            rightHand = new TrackedNode(new GameObject("Right Hand"));
            fullBodyTracking = new GameObject("Full Body Tracking").transform;

            head.transform.SetParent(transform, false);
            leftHand.transform.SetParent(transform, false);
            rightHand.transform.SetParent(transform, false);
            fullBodyTracking.SetParent(transform, false);

            pelvis = new TrackedNode(new GameObject("Pelvis"));
            leftFoot = new TrackedNode(new GameObject("Left Foot"));
            rightFoot = new TrackedNode(new GameObject("Right Foot"));

            pelvis.transform.SetParent(fullBodyTracking, false);
            leftFoot.transform.SetParent(fullBodyTracking, false);
            rightFoot.transform.SetParent(fullBodyTracking, false);

            Transform leftHandControllerOffset = new GameObject("Left Hand Controller Offset").transform;
            Transform rightHandControllerOffset = new GameObject("Left Hand Controller Offset").transform;

            leftHandControllerOffset.SetParent(leftHand.transform, false);
            rightHandControllerOffset.SetParent(rightHand.transform, false);

            VRControllersValueSettingsOffsets localPlayerControllerOffset = _container.InstantiateComponent<VRControllersValueSettingsOffsets>(gameObject);

            _leftController = SetUpVRController(leftHand.gameObject, XRNode.LeftHand, leftHandControllerOffset, localPlayerControllerOffset);
            _rightController = SetUpVRController(rightHand.gameObject, XRNode.RightHand, rightHandControllerOffset, localPlayerControllerOffset);

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

            headCalibration = new GameObject("Head Calibration").transform;
            pelvisCalibration = new GameObject("Pelvis Calibration").transform;
            leftFootCalibration = new GameObject("Left Foot Calibration").transform;
            rightFootCalibration = new GameObject("Right Foot Calibration").transform;
            headCalibration.SetParent(head.transform, false);
            pelvisCalibration.SetParent(pelvis.transform, false);
            leftFootCalibration.SetParent(leftFoot.transform, false);
            rightFootCalibration.SetParent(rightFoot.transform, false);

            headOffset = new GameObject("Head Offset").transform;
            leftHandOffset = new GameObject("Left Hand Offset").transform;
            rightHandOffset = new GameObject("Right Hand Offset").transform;
            pelvisOffset = new GameObject("Pelvis Offset").transform;
            leftFootOffset = new GameObject("Left Foot Offset").transform;
            rightFootOffset = new GameObject("Right Foot Offset").transform;

            headOffset.SetParent(headCalibration, false);
            leftHandOffset.SetParent(leftHandControllerOffset, false);
            rightHandOffset.SetParent(rightHandControllerOffset, false);
            pelvisOffset.SetParent(pelvisCalibration, false);
            leftFootOffset.SetParent(leftFootCalibration, false);
            rightFootOffset.SetParent(rightFootCalibration, false);
        }

        private void OnEnable()
        {
            if (_activePlayerSpaceManager != null)
            {
                _activePlayerSpaceManager.changed += OnActivePlayerSpaceChanged;
                OnActivePlayerSpaceChanged(_activePlayerSpaceManager.current);
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
                _beatSaberUtilities.controllersChanged += OnControllersDidChangeReference;
                _beatSaberUtilities.roomAdjustChanged += OnRoomAdjustChanged;
            }

            if (_deviceProvider != null)
            {
                _deviceProvider.devicesChanged += OnDevicesChanged;
            }

            UpdateOffsets();
            UpdateControllerOffsets();
            UpdateActiveTransforms();
            UpdateRenderModels();
            UpdateRenderModelsVisibility();
        }

        private void Start()
        {
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
            UpdateControllerOffsets();
        }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(
            ILogger<TrackingRig> logger,
            DiContainer container,
            IDeviceProvider deviceProvider,
            [InjectOptional] IRenderModelProvider renderModelProvider,
            PlayerAvatarManager playerAvatarManager,
            ActivePlayerSpaceManager activePlayerSpaceManager,
            ActiveOriginManager activeOriginManager,
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
            _activePlayerSpaceManager = activePlayerSpaceManager;
            _activeOriginManager = activeOriginManager;
            _settings = settings;
            _calibrationData = calibrationData;
            _vrControllerVisualsManager = vrControllerVisualsManager;
            _beatSaberUtilities = beatSaberUtilities;
            _fpfcSettings = fpfcSettings;
            _humanoidCalibrator = new HumanoidCalibrator(this, calibrationData, settings, activeOriginManager, beatSaberUtilities);
        }

        private void Update()
        {
            UpdateTransform(DeviceUse.Head, head);
            UpdateTransform(DeviceUse.LeftHand, leftHand);
            UpdateTransform(DeviceUse.RightHand, rightHand);
            UpdateTransform(DeviceUse.Waist, pelvis);
            UpdateTransform(DeviceUse.LeftFoot, leftFoot);
            UpdateTransform(DeviceUse.RightFoot, rightFoot);

            if (activeCalibrationMode != CalibrationMode.None)
            {
                _playerAvatarManager.ResizeCurrentAvatar(eyeHeight);

                // need to use trigger rather than triggerButton since the latter is not bound on all controller types with SteamVR
                if (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.trigger, out float leftTriggerValue) &&
                    InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue) &&
                    leftTriggerValue == 1 && rightTriggerValue == 1)
                {
                    switch (activeCalibrationMode)
                    {
                        case CalibrationMode.Automatic:
                            _humanoidCalibrator.CalibrateAutomatically();
                            break;

                        case CalibrationMode.Manual:
                            _humanoidCalibrator.CalibrateManually(_playerAvatarManager.currentlySpawnedAvatar);
                            break;
                    }

                    calibrationMode = activeCalibrationMode;

                    EndCalibration();
                }
            }
        }

        private void OnDisable()
        {
            if (_activePlayerSpaceManager != null)
            {
                _activePlayerSpaceManager.changed -= OnActivePlayerSpaceChanged;
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
                _beatSaberUtilities.controllersChanged -= OnControllersDidChangeReference;
                _beatSaberUtilities.roomAdjustChanged -= OnRoomAdjustChanged;
            }

            if (_deviceProvider != null)
            {
                _deviceProvider.devicesChanged -= OnDevicesChanged;
            }
        }

        private void OnDestroy()
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

        private VRController SetUpVRController(GameObject trackedNode, XRNode node, Transform controllerOffset, VRControllerTransformOffset transformOffset)
        {
            trackedNode.SetActive(false);

            VRController vrController = _container.InstantiateComponent<VRController>(trackedNode);
            vrController.enabled = false;
            vrController._node = node;
            vrController._viewAnchorTransform = controllerOffset;
            vrController._transformOffset = transformOffset;

            trackedNode.SetActive(true);

            return vrController;
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            EndCalibration();
            UpdateOffsets();
            UpdateActiveTransforms();
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
            UpdateActiveTransforms();
            UpdateRenderModels();
            trackingChanged?.Invoke();
        }

        private void OnControllersDidChangeReference()
        {
            UpdateControllerOffsets();
        }

        private void OnFpfcSettingsChanged(IFPFCSettings fpfcSettings)
        {
            UpdateBehaviourEnabled();
        }

        private void OnFocusChanged(bool hasFocus)
        {
            UpdateBehaviourEnabled();
        }

        private void UpdateBehaviourEnabled()
        {
            enabled = _beatSaberUtilities.hasFocus && !_fpfcSettings.Enabled;
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

            UpdateOffset(leftHandOffset, spawnedAvatar.prefab.leftHandOffset, spawnedAvatar.absoluteScale);
            UpdateOffset(rightHandOffset, spawnedAvatar.prefab.rightHandOffset, spawnedAvatar.absoluteScale);

            float fullBodyScale = _playerAvatarManager.currentlySpawnedAvatar.scaledEyeHeight / _settings.playerEyeHeight;
            float trackerScale = spawnedAvatar.absoluteScale / fullBodyScale;

            CalibrationData.FullBodyCalibration automaticCalibration = _calibrationData.automaticCalibration;
            CalibrationData.FullBodyCalibration manualCalibration = _calibrationData.GetAvatarManualCalibration(spawnedAvatar);

            UpdateOffset(headOffset, spawnedAvatar.prefab.headOffset, spawnedAvatar.prefab.headCalibrationOffset, spawnedAvatar.absoluteScale, manualCalibration.head, automaticCalibration.head);
            UpdateOffset(pelvisOffset, spawnedAvatar.prefab.pelvisOffset, spawnedAvatar.prefab.pelvisCalibrationOffset, trackerScale, manualCalibration.waist, automaticCalibration.waist);
            UpdateOffset(leftFootOffset, spawnedAvatar.prefab.leftLegOffset, spawnedAvatar.prefab.leftFootCalibrationOffset, trackerScale, manualCalibration.leftFoot, automaticCalibration.leftFoot);
            UpdateOffset(rightFootOffset, spawnedAvatar.prefab.rightLegOffset, spawnedAvatar.prefab.rightFootCalibrationOffset, trackerScale, manualCalibration.rightFoot, automaticCalibration.rightFoot);

            switch (calibrationMode)
            {
                case CalibrationMode.None:
                    _humanoidCalibrator.ApplyNoCalibration();
                    break;

                case CalibrationMode.Manual:
                    _humanoidCalibrator.ApplyManualCalibration(spawnedAvatar);
                    break;

                case CalibrationMode.Automatic:
                    _humanoidCalibrator.ApplyAutomaticCalibration();
                    break;
            }

            fullBodyTracking.localPosition = new Vector3(0, _settings.playerEyeHeight - _playerAvatarManager.currentlySpawnedAvatar.scaledEyeHeight, 0);
            fullBodyTracking.localScale = new Vector3(1, fullBodyScale, 1);
        }

        private void UpdateControllerOffsets()
        {
            _leftController.UpdateAnchorOffsetPose();
            _rightController.UpdateAnchorOffsetPose();
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

        private void OnActivePlayerSpaceChanged(Transform transform)
        {
            _logger.LogTrace("Updating constraints");

            if (transform != null)
            {
                List<ConstraintSource> sources = [new ConstraintSource { sourceTransform = transform, weight = 1 }];

                _parentConstraint.SetSources(sources);
                _scaleConstraint.SetSources(sources);
            }
            else
            {
                _parentConstraint.SetSources(kEmptyConstraintSources);
                _scaleConstraint.SetSources(kEmptyConstraintSources);
            }
        }

        private void UpdateTransform(DeviceUse deviceUse, TrackedNode trackedNode)
        {
            if (_deviceProvider.TryGetDevice(deviceUse, out TrackedDevice device) && device.isTracking)
            {
                trackedNode.transform.SetLocalPositionAndRotation(device.position, device.rotation);
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

        private void UpdateActiveTransforms()
        {
            if (_deviceProvider == null)
            {
                return;
            }

            _logger.LogTrace("Updating active transforms");

            UpdateActiveTransform(head, headOffset, DeviceUse.Head);
            UpdateActiveTransform(leftHand, leftHandOffset, DeviceUse.LeftHand);
            UpdateActiveTransform(rightHand, rightHandOffset, DeviceUse.RightHand);
            UpdateActiveTransform(pelvis, pelvisOffset, DeviceUse.Waist);
            UpdateActiveTransform(leftFoot, leftFootOffset, DeviceUse.LeftFoot);
            UpdateActiveTransform(rightFoot, rightFootOffset, DeviceUse.RightFoot);

            areBothHandsTracking = leftHand.isTracking && rightHand.isTracking;
            areAnyFullBodyTrackersTracking = pelvis.isTracking || leftFoot.isTracking || rightFoot.isTracking;

            trackingChanged?.Invoke();
        }

        private void UpdateActiveTransform(TrackedNode trackedNode, Transform offset, DeviceUse deviceUse)
        {
            if (_deviceProvider.TryGetDevice(deviceUse, out TrackedDevice trackedDevice))
            {
                trackedNode.isTracking = trackedDevice.isTracking;
                trackedNode.gameObject.SetActive(trackedDevice.isTracking);
                offset.gameObject.SetActive(trackedDevice.isTracking && IsCurrentlyCalibrated(deviceUse));
            }
            else
            {
                trackedNode.isTracking = false;
                trackedNode.gameObject.SetActive(false);
                offset.gameObject.SetActive(false);
            }
        }

        internal bool IsCurrentlyCalibrated(DeviceUse deviceUse)
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
                    calibration = _calibrationData.GetAvatarManualCalibration(_playerAvatarManager.currentlySpawnedAvatar);
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

            _ = UpdateRenderModelsAsync().ContinueWith((task) => _logger.LogError(task.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        private Task UpdateRenderModelsAsync()
        {
            _logger.LogTrace("Updating render models");

            return Task.WhenAll(
                UpdateRenderModelAsync(DeviceUse.LeftHand, _leftHandRenderModel),
                UpdateRenderModelAsync(DeviceUse.RightHand, _rightHandRenderModel),
                UpdateRenderModelAsync(DeviceUse.Waist, _pelvisRenderModel),
                UpdateRenderModelAsync(DeviceUse.LeftFoot, _leftFootRenderModel),
                UpdateRenderModelAsync(DeviceUse.RightFoot, _rightFootRenderModel));
        }

        private async Task UpdateRenderModelAsync(DeviceUse deviceUse, TrackedRenderModel trackedRenderModel)
        {
            RenderModel renderModel = await _renderModelProvider.GetRenderModelAsync(deviceUse);

            trackedRenderModel.transform.SetLocalPositionAndRotation(renderModel.localOrigin.position, renderModel.localOrigin.rotation);
            trackedRenderModel.meshFilter.mesh = renderModel?.mesh;
            trackedRenderModel.meshRenderer.material = renderModel?.material;
        }
    }
}
