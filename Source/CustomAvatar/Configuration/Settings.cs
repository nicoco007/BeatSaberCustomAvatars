using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using CustomAvatar.Tracking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace CustomAvatar.Configuration
{
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable ClassWithVirtualMembersNeverInherited.Global
    // ReSharper disable RedundantDefaultMemberInitializer
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    // ReSharper disable UnusedMember.Global
    // ReSharper disable FieldCanBeMadeReadOnly.Global
    // ReSharper disable InconsistentNaming
    internal class Settings
    {
        public event Action<bool> firstPersonEnabledChanged;
        public event Action<bool> moveFloorWithRoomAdjustChanged;

        private bool _isAvatarVisibleInFirstPerson;
        public bool isAvatarVisibleInFirstPerson
        {
            get => _isAvatarVisibleInFirstPerson;
            set
            {
                _isAvatarVisibleInFirstPerson = value;
                firstPersonEnabledChanged?.Invoke(value);
            }
        }

        private bool _moveFloorWithRoomAdjust = false;

        public bool moveFloorWithRoomAdjust
        {
            get => _moveFloorWithRoomAdjust;
            set
            {
                _moveFloorWithRoomAdjust = value;
                moveFloorWithRoomAdjustChanged?.Invoke(value);
            }
        }

        [JsonConverter(typeof(StringEnumConverter))] public AvatarResizeMode resizeMode = AvatarResizeMode.Height;
        public bool enableFloorAdjust = false;
        public string previousAvatarPath = null;
        public float playerArmSpan = AvatarTailor.kDefaultPlayerArmSpan;
        public bool calibrateFullBodyTrackingOnStart = false;
        public float cameraNearClipPlane = 0.1f;
        public readonly Lighting lighting = new Lighting();
        public readonly Mirror mirror = new Mirror();
        public readonly AutomaticFullBodyCalibration automaticCalibration = new AutomaticFullBodyCalibration();
        public readonly FullBodyMotionSmoothing fullBodyMotionSmoothing = new FullBodyMotionSmoothing();

        [JsonProperty(PropertyName = "avatarSpecificSettings", Order = int.MaxValue)] private Dictionary<string, AvatarSpecificSettings> _avatarSpecificSettings = new Dictionary<string, AvatarSpecificSettings>();

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            RemoveInvalidAvatarSettings();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            RemoveInvalidAvatarSettings();
        }

        private void RemoveInvalidAvatarSettings()
        {
            foreach (string fileName in _avatarSpecificSettings.Keys.ToList())
            {
                if (!File.Exists(Path.Combine(PlayerAvatarManager.kCustomAvatarsPath, fileName)) || Path.IsPathRooted(fileName))
                {
                    _avatarSpecificSettings.Remove(fileName);
                }
            }
        }

        public class Lighting
        {
            public bool enabled = false;
            public bool castShadows = false;
            [JsonConverter(typeof(StringEnumConverter))] public ShadowResolution shadowResolution = ShadowResolution.Medium;
            public bool enableDynamicLighting = false;
        }

        public class Mirror
        {
            public Vector3 positionOffset = new Vector3(0, -1f, 0);
            public Vector2 size = new Vector2(5f, 4f);
            public float renderScale = 1.0f;
        }

        public class FullBodyMotionSmoothing
        {
            public readonly TrackedPointSmoothing waist = new TrackedPointSmoothing { position = 15, rotation = 10 };
            public readonly TrackedPointSmoothing feet = new TrackedPointSmoothing { position = 13, rotation = 17 };
        }

        public class TrackedPointSmoothing
        {
            public float position;
            public float rotation;
        }

        public class AutomaticFullBodyCalibration
        {
            public float legOffset = 0.15f;
            public float pelvisOffset = 0.1f;

            public WaistTrackerPosition waistTrackerPosition = WaistTrackerPosition.Front;
        }

        public class AvatarSpecificSettings
        {
            public bool useAutomaticCalibration = false;
            public bool allowMaintainPelvisPosition = false;
            public bool bypassCalibration = false;
        }

        public AvatarSpecificSettings GetAvatarSettings(string fileName)
        {
            if (!_avatarSpecificSettings.ContainsKey(fileName))
            {
                _avatarSpecificSettings.Add(fileName, new AvatarSpecificSettings());
            }

            return _avatarSpecificSettings[fileName];
        }
    }
}
