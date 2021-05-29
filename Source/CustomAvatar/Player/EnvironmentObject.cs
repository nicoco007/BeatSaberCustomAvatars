using System;
using System.Linq;
using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class EnvironmentObject : MonoBehaviour
    {
        // found this property through UnityExplorer, hopefully it doesn't disappear in future versions of Unity
        private static readonly Action<Renderer, Transform> kStaticBatchRootTransformSetter = ReflectionExtensions.CreatePropertySetter<Renderer, Transform>("staticBatchRootTransform");

        private ILogger<EnvironmentObject> _logger;
        private PlayerAvatarManager _playerAvatarManager;
        private Settings _settings;
        private BeatSaberUtilities _beatSaberUtilities;

        private float _originalY;

        internal void Awake()
        {
            _originalY = transform.localPosition.y;

            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>().Where(mr => mr.isPartOfStaticBatch))
            {
                kStaticBatchRootTransformSetter(renderer, transform);
            }
        }

        [Inject]
        internal void Construct(ILogger<EnvironmentObject> logger, PlayerAvatarManager playerAvatarManager, Settings settings, BeatSaberUtilities beatSaberUtilities)
        {
            _logger = logger;
            _playerAvatarManager = playerAvatarManager;
            _settings = settings;
            _beatSaberUtilities = beatSaberUtilities;
        }

        internal void Start()
        {
            _playerAvatarManager.avatarChanged += OnAvatarChanged;
            _playerAvatarManager.avatarScaleChanged += OnAvatarScaleChanged;
            _settings.floorHeightAdjust.changed += OnFloorHeightAdjustChanged;

            foreach (Mirror mirror in GetComponentsInChildren<Mirror>())
            {
                _logger.Trace($"Replacing {nameof(MirrorRendererSO)} on '{mirror.name}'");

                MirrorRendererSO original = mirror.GetField<MirrorRendererSO, Mirror>("_mirrorRenderer");
                MirrorRendererSO renderer = Instantiate(original);
                renderer.name = original.name + " (Moved Floor Instance)";
                mirror.SetField("_mirrorRenderer", renderer);
            }

            UpdateOffset();
        }

        internal void OnDestroy()
        {
            _playerAvatarManager.avatarChanged -= OnAvatarChanged;
            _playerAvatarManager.avatarScaleChanged -= OnAvatarScaleChanged;
            _settings.floorHeightAdjust.changed -= OnFloorHeightAdjustChanged;
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            UpdateOffset();
        }

        private void OnAvatarScaleChanged(float scale)
        {
            UpdateOffset();
        }

        private void OnFloorHeightAdjustChanged(FloorHeightAdjust value)
        {
            UpdateOffset();
        }

        private void UpdateOffset()
        {
            float floorOffset = _playerAvatarManager.GetFloorOffset();

            if (_settings.moveFloorWithRoomAdjust)
            {
                floorOffset += _beatSaberUtilities.roomCenter.y;
            }

            transform.localPosition = new Vector3(transform.localPosition.x, _originalY + floorOffset, transform.localPosition.z);
        }
    }
}
