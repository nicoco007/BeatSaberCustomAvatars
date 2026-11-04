//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using System.ComponentModel;
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
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace CustomAvatar.Tracking
{
    [DisallowMultipleComponent]
    internal class TrackingRig : MonoBehaviour
    {
        [SerializeField] private SimpleParentConstraint _parentConstraint;
        [SerializeField] private TrackedRenderModel _leftHandRenderModel;
        [SerializeField] private TrackedRenderModel _rightHandRenderModel;
        [SerializeField] private TrackedRenderModel _pelvisRenderModel;
        [SerializeField] private TrackedRenderModel _leftFootRenderModel;
        [SerializeField] private TrackedRenderModel _rightFootRenderModel;
        [SerializeField] private HumanoidCalibrator _humanoidCalibrator;

        private ILogger<TrackingRig> _logger;
        private IDeviceProvider _deviceProvider;
        private IRenderModelProvider _renderModelProvider;
        private PlayerAvatarManager _playerAvatarManager;
        private ActiveCameraManager _activeCameraManager;
        private Settings _settings;
        private CalibrationData _calibrationData;
        private VRControllerVisualsManager _vrControllerVisualsManager;
        private BeatSaberUtilities _beatSaberUtilities;
        private IFPFCSettings _fpfcSettings;

        private bool _showRenderModels;

        internal event Action trackingChanged;

        internal event Action<bool> isCalibratingChanged;

        internal event Action<bool> isAdjustingChanged;

        internal bool areBothHandsTracking { get; private set; }

        internal bool areAnyFullBodyTrackersTracking { get; private set; }

        // local to the current active origin (parent of VRCenterAdjust or world if no parent)
        internal float eyeHeight => (_activeCameraManager.current != null && _activeCameraManager.current.origin != null ? _activeCameraManager.current.origin.InverseTransformPoint(head.transform.position).y : head.transform.position.y) - (_settings.moveFloorWithRoomAdjust ? _beatSaberUtilities.roomCenter.y : 0);

        [field: SerializeField]
        internal SimpleNode head { get; private set; }

        [field: SerializeField]
        internal ControllerNode leftHand { get; private set; }

        [field: SerializeField]
        internal ControllerNode rightHand { get; private set; }

        [field: SerializeField]
        internal CalibratableNode pelvis { get; private set; }

        [field: SerializeField]
        internal CalibratableNode leftFoot { get; private set; }

        [field: SerializeField]
        internal CalibratableNode rightFoot { get; private set; }

        [field: SerializeField]
        internal Transform fullBodyTracking { get; private set; }

        internal bool isCalibrating
        {
            get;
            private set
            {
                field = value;
                _humanoidCalibrator.gameObject.SetActive(value);
                isCalibratingChanged?.Invoke(value);
            }
        }

        internal bool isAdjusting
        {
            get;
            private set
            {
                field = value;
                isAdjustingChanged?.Invoke(value);
            }
        }

        internal AdjustMode adjustMode { get; set; }

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

        internal void BeginCalibration()
        {
            if (isAdjusting)
            {
                return;
            }

            isCalibrating = true;

            SetIKEnabled(false);
        }

        internal void EndCalibration()
        {
            isCalibrating = false;

            SetIKEnabled(true);

            _playerAvatarManager.ResizeCurrentAvatar();

            UpdateCalibrationStates();

            trackingChanged?.Invoke();
        }

        internal void BeginAdjust()
        {
            if (isCalibrating)
            {
                return;
            }

            leftHand.trackerGrabber.enabled = true;
            rightHand.trackerGrabber.enabled = true;

            pelvis.grabSphere.SetActive(pelvis.isCalibrated);
            leftFoot.grabSphere.SetActive(leftFoot.isCalibrated);
            rightFoot.grabSphere.SetActive(rightFoot.isCalibrated);

            isAdjusting = true;
        }

        internal void EndAdjust()
        {
            leftHand.trackerGrabber.enabled = false;
            rightHand.trackerGrabber.enabled = false;

            pelvis.grabSphere.SetActive(false);
            leftFoot.grabSphere.SetActive(false);
            rightFoot.grabSphere.SetActive(false);

            _humanoidCalibrator.Adjust();

            isAdjusting = false;
        }

        internal void ClearAdjust()
        {
            _humanoidCalibrator.ClearAdjust();
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

            InputSystem.onAfterUpdate += OnAfterUpdate;
        }

        protected void Start()
        {
            if (_fpfcSettings != null)
            {
                _fpfcSettings.PropertyChanged += OnFpfcSettingsPropertyChanged;
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
        }

        protected void OnAfterUpdate()
        {
            UpdateTransform(head);
            UpdateTransform(pelvis);
            UpdateTransform(leftFoot);
            UpdateTransform(rightFoot);
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

            InputSystem.onAfterUpdate -= OnAfterUpdate;
        }

        protected void OnDestroy()
        {
            if (_fpfcSettings != null)
            {
                _fpfcSettings.PropertyChanged -= OnFpfcSettingsPropertyChanged;
            }

            if (_beatSaberUtilities != null)
            {
                _beatSaberUtilities.focusChanged -= OnFocusChanged;
            }
        }

        private void OnAvatarChanged(SpawnedAvatar spawnedAvatar)
        {
            EndCalibration();
            UpdateOffsets();
            UpdateNodeStates();
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

        private void OnFpfcSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
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

            CalibrationData.FullBodyCalibration calibration = _calibrationData.calibration;

            AvatarPrefab avatarPrefab = spawnedAvatar.prefab;
            UpdateOffset(head.offset, avatarPrefab.headOffset, _playerAvatarManager.scale);
            UpdateOffset(pelvis.offset, avatarPrefab.pelvisOffset, avatarPrefab.pelvisCalibrationOffset, trackerScale, calibration.waist);
            UpdateOffset(leftFoot.offset, avatarPrefab.leftLegOffset, avatarPrefab.leftFootCalibrationOffset, trackerScale, calibration.leftFoot);
            UpdateOffset(rightFoot.offset, avatarPrefab.rightLegOffset, avatarPrefab.rightFootCalibrationOffset, trackerScale, calibration.rightFoot);

            _humanoidCalibrator.ApplyCalibration();
            _humanoidCalibrator.ApplyAdjust();

            fullBodyTracking.localPosition = new Vector3(0, _settings.playerEyeHeight - _playerAvatarManager.scaledEyeHeight, 0);
            fullBodyTracking.localScale = new Vector3(1, fullBodyScale, 1);
        }

        private void UpdateOffset(Transform transform, Pose offset, Pose calibrationOffset, float scale, Pose calibration)
        {
            if (!calibration.Equals(Pose.identity))
            {
                UpdateOffset(transform, calibrationOffset, scale);
            }
            else
            {
                UpdateOffset(transform, offset, scale);
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
                _parentConstraint.sourceTransform = activeCamera.playerSpace;
            }
            else
            {
                _parentConstraint.sourceTransform = null;
            }
        }

        private void UpdateTransform(ITrackedNode trackedNode)
        {
            if (_deviceProvider.TryGetDeviceState(trackedNode.deviceUse, out DeviceState deviceState) && deviceState.isTracking)
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

            UpdateNodeState(head);
            UpdateNodeState(leftHand);
            UpdateNodeState(rightHand);
            UpdateNodeState(pelvis);
            UpdateNodeState(leftFoot);
            UpdateNodeState(rightFoot);

            areBothHandsTracking = leftHand.isTracking && rightHand.isTracking;
            areAnyFullBodyTrackersTracking = pelvis.isTracking || leftFoot.isTracking || rightFoot.isTracking;

            trackingChanged?.Invoke();
        }

        private void UpdateNodeState(CalibratableNode calibratableNode)
        {
            if (_deviceProvider.TryGetDeviceState(calibratableNode.deviceUse, out DeviceState deviceState))
            {
                calibratableNode.isTracking = deviceState.isTracking;
                calibratableNode.gameObject.SetActive(deviceState.isTracking);
            }
            else
            {
                calibratableNode.isTracking = false;
                calibratableNode.gameObject.SetActive(false);
            }

            UpdateCalibrationState(calibratableNode);
        }

        private void UpdateCalibrationStates()
        {
            UpdateCalibrationState(pelvis);
            UpdateCalibrationState(leftFoot);
            UpdateCalibrationState(rightFoot);
        }

        private void UpdateCalibrationState(CalibratableNode calibratableNode)
        {
            calibratableNode.isCalibrated = IsCurrentlyCalibrated(calibratableNode.deviceUse);
        }

        private void UpdateNodeState(ITrackedNode trackedNode)
        {
            // To mimic what VRController does, we only check if the device is connected.
            trackedNode.isTracking = _deviceProvider.TryGetDeviceState(trackedNode.deviceUse, out DeviceState deviceState) && deviceState.isConnected;
        }

        private bool IsCurrentlyCalibrated(DeviceUse deviceUse)
        {
            if (deviceUse is not DeviceUse.Waist and not DeviceUse.LeftFoot and not DeviceUse.RightFoot)
            {
                return true;
            }

            CalibrationData.FullBodyCalibration calibration = _calibrationData.calibration;

            Pose pose = deviceUse switch
            {
                DeviceUse.Waist => calibration.waist,
                DeviceUse.LeftFoot => calibration.leftFoot,
                DeviceUse.RightFoot => calibration.rightFoot,
                _ => Pose.identity,
            };

            return !pose.Equals(Pose.identity);
        }

        private void UpdateRenderModelsVisibility()
        {
            if (_renderModelProvider == null)
            {
                return;
            }

            bool show = _showRenderModels && _settings.showRenderModels && _beatSaberUtilities.hasFocus;
            _leftHandRenderModel.gameObject.SetActive(show);
            _rightHandRenderModel.gameObject.SetActive(show);
            _pelvisRenderModel.gameObject.SetActive(show);
            _leftFootRenderModel.gameObject.SetActive(show);
            _rightFootRenderModel.gameObject.SetActive(show);
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
                UpdateRenderModelAsync(leftHand, _leftHandRenderModel),
                UpdateRenderModelAsync(rightHand, _rightHandRenderModel),
                UpdateRenderModelAsync(pelvis, _pelvisRenderModel),
                UpdateRenderModelAsync(leftFoot, _leftFootRenderModel),
                UpdateRenderModelAsync(rightFoot, _rightFootRenderModel));
        }

        private async Task UpdateRenderModelAsync(ITrackedNode trackedNode, TrackedRenderModel trackedRenderModel)
        {
            RenderModel renderModel;

            if (trackedNode.isTracking && (renderModel = await _renderModelProvider.GetRenderModelAsync(trackedNode.deviceUse)) != null)
            {
                trackedRenderModel.SetRenderModel(renderModel);
            }
            else
            {
                trackedRenderModel.SetRenderModel(null);
            }
        }

        internal class Mirrored : MonoBehaviour
        {
            private static readonly ProfilerMarker kOnBeforeRenderProfilerMarker = new($"{nameof(MirroredTrackingRig)}.{nameof(OnBeforeRender)}");

            [SerializeField] private MirroredControllerNode _leftHand;
            [SerializeField] private MirroredControllerNode _rightHand;
            [SerializeField] private Transform _fullBodyTracking;
            [SerializeField] private MirroredCalibratableNode _pelvis;
            [SerializeField] private MirroredCalibratableNode _leftFoot;
            [SerializeField] private MirroredCalibratableNode _rightFoot;
            [SerializeField] private Transform _humanoidCalibrator;
            [SerializeField] private MirroredCalibrationPoint _pelvisCalibration;
            [SerializeField] private MirroredCalibrationPoint _leftFootCalibration;
            [SerializeField] private MirroredCalibrationPoint _rightFootCalibration;

            private TrackingRig _trackingRig;

            [Inject]
            protected void Construct(TrackingRig trackingRig)
            {
                _trackingRig = trackingRig;
            }

            protected void Awake()
            {
                _leftHand.Init(_trackingRig.leftHand);
                _rightHand.Init(_trackingRig.rightHand);
                _pelvis.Init(_trackingRig.pelvis);
                _leftFoot.Init(_trackingRig.leftFoot);
                _rightFoot.Init(_trackingRig.rightFoot);
            }

            protected void OnEnable()
            {
                Application.onBeforeRender += OnBeforeRender;

                _trackingRig.trackingChanged += OnTrackingChanged;
                _trackingRig.isCalibratingChanged += OnIsCalibratingChanged;
                _trackingRig.isAdjustingChanged += OnIsAdjustingChanged;

                OnTrackingChanged();
                OnIsCalibratingChanged(_trackingRig.isCalibrating);
                OnIsAdjustingChanged(_trackingRig.isAdjusting);
            }

            protected void OnDisable()
            {
                Application.onBeforeRender -= OnBeforeRender;

                _trackingRig.trackingChanged -= OnTrackingChanged;
                _trackingRig.isCalibratingChanged -= OnIsCalibratingChanged;
                _trackingRig.isAdjustingChanged -= OnIsAdjustingChanged;
            }

            private void OnTrackingChanged()
            {
                SetActive(_pelvis, _pelvisCalibration, _trackingRig.pelvis.isTracking);
                SetActive(_leftFoot, _leftFootCalibration, _trackingRig.leftFoot.isTracking);
                SetActive(_rightFoot, _rightFootCalibration, _trackingRig.rightFoot.isTracking);
            }

            private void OnIsCalibratingChanged(bool isCalibrating)
            {
                _humanoidCalibrator.gameObject.SetActive(isCalibrating);
            }

            private void OnIsAdjustingChanged(bool isAdjusting)
            {
                _pelvis.grabSphere.SetActive(_trackingRig.pelvis.grabSphere.activeSelf);
                _leftFoot.grabSphere.SetActive(_trackingRig.leftFoot.grabSphere.activeSelf);
                _rightFoot.grabSphere.SetActive(_trackingRig.rightFoot.grabSphere.activeSelf);
            }

            private void SetActive(MirroredCalibratableNode node, MirroredCalibrationPoint point, bool active)
            {
                node.gameObject.SetActive(active);
                point.gameObject.SetActive(active);
            }

            private void OnBeforeRender()
            {
                kOnBeforeRenderProfilerMarker.Begin(this);

                try
                {
                    transform.CopyMirroredPoseFrom(_trackingRig.transform, transform.parent);

                    _leftHand.OnBeforeRender();
                    _rightHand.OnBeforeRender();

                    _fullBodyTracking.localPosition = _trackingRig.fullBodyTracking.localPosition;
                    _fullBodyTracking.localScale = _trackingRig.fullBodyTracking.localScale;

                    _pelvis.OnBeforeRender();
                    _leftFoot.OnBeforeRender();
                    _rightFoot.OnBeforeRender();

                    _humanoidCalibrator.CopyLocalPositionAndRotationFrom(_trackingRig._humanoidCalibrator.transform);

                    _pelvisCalibration.CopyFrom(_trackingRig._humanoidCalibrator.pelvis);
                    _leftFootCalibration.CopyFrom(_trackingRig._humanoidCalibrator.leftFoot);
                    _rightFootCalibration.CopyFrom(_trackingRig._humanoidCalibrator.rightFoot);
                }
                finally
                {
                    kOnBeforeRenderProfilerMarker.End();
                }
            }
        }
    }
}
