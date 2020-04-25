using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace CustomAvatar.Utilities
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

        [JsonConverter(typeof(StringEnumConverter))] public AvatarResizeMode resizeMode = AvatarResizeMode.Height;
        public bool enableFloorAdjust = false;
        public bool moveFloorWithRoomAdjust = false;
        public string previousAvatarPath = null;
        public float playerArmSpan = AvatarTailor.kDefaultPlayerArmSpan;
        public bool calibrateFullBodyTrackingOnStart = false;
        public float cameraNearClipPlane = 0.1f;
        public Lighting lighting { get; private set; } = new Lighting();
        public Mirror mirror { get; private set; } = new Mirror();
        public ManualTrackerOffsets trackerOffsets { get; private set; } = new ManualTrackerOffsets();
        public FullBodyMotionSmoothing fullBodyMotionSmoothing { get; private set; } = new FullBodyMotionSmoothing();
        [JsonProperty(Order = int.MaxValue)] private Dictionary<string, AvatarSpecificSettings> avatarSpecificSettings = new Dictionary<string, AvatarSpecificSettings>();

        public class Lighting
        {
            public bool enabled = true;
            public bool castShadows = false;
            [JsonConverter(typeof(StringEnumConverter))] public ShadowResolution shadowResolution = ShadowResolution.Medium;

            [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Reuse)]
            public readonly LightDefinition[] lights =
            {
                new LightDefinition { type = LightType.Directional, rotation = new Vector3(135, 0, 0) },
                new LightDefinition { type = LightType.Directional, rotation = new Vector3(45, 0, 0) }
            };
        }

        public class LightDefinition
        {
            [JsonConverter(typeof(StringEnumConverter))] public LightType type = LightType.Directional;
            public Vector3 position = Vector3.zero;
            public Vector3 rotation = Vector3.zero;
            public Color color = Color.white;
            public float intensity = 1;
            public float spotAngle = 30;
            public float range = 10;
        }

        public class Mirror
        {
            public Vector3 positionOffset = new Vector3(0, -1f, 0);
            public Vector2 size = new Vector2(5f, 4f);
            public float renderScale = 1.0f;
        }

        public class FullBodyMotionSmoothing
        {
            public TrackedPointSmoothing waist { get; private set; } = new TrackedPointSmoothing { position = 15, rotation = 10 };
            public TrackedPointSmoothing feet { get; private set; } = new TrackedPointSmoothing { position = 13, rotation = 17 };
        }

        public class TrackedPointSmoothing
        {
            public float position;
            public float rotation;
        }

        public class FullBodyCalibration
        {
            public event Action calibrationChanged;

            public Pose leftLeg
            {
                get => _leftLeg;
                set
                {
                    _leftLeg = value;
                    calibrationChanged?.Invoke();
                }
            }

            public Pose rightLeg
            {
                get => _rightLeg;
                set
                {
                    _rightLeg = value;
                    calibrationChanged?.Invoke();
                }
            }

            public Pose pelvis
            {
                get => _pelvis;
                set
                {
                    _pelvis = value;
                    calibrationChanged?.Invoke();
                }
            }

            private Pose _leftLeg = Pose.identity;
            private Pose _rightLeg = Pose.identity;
            private Pose _pelvis = Pose.identity;

            [JsonIgnore] public bool isDefault => leftLeg.Equals(Pose.identity) && rightLeg.Equals(Pose.identity) && pelvis.Equals(Pose.identity);
        }

        public class ManualTrackerOffsets
        {
            public event Action offsetChanged;

            public float leftLegOffset
            {
                get => _leftLegOffset;
                set
                {
                    _leftLegOffset = value;
                    offsetChanged?.Invoke();
                }
            }

            public float rightLegOffset
            {
                get => _rightLegOffset;
                set
                {
                    _rightLegOffset = value;
                    offsetChanged?.Invoke();
                }
            }

            public float pelvisOffset
            {
                get => _pelvisOffset;
                set
                {
                    _pelvisOffset = value;
                    offsetChanged?.Invoke();
                }
            }

            private float _leftLegOffset = 0.15f;
            private float _rightLegOffset = 0.15f;
            private float _pelvisOffset = 0.1f;
        }

        public class AvatarSpecificSettings
        {
            public FullBodyCalibration fullBodyCalibration { get; private set; } = new FullBodyCalibration();
            public bool useAutomaticCalibration = false;
            public bool allowMaintainPelvisPosition = false;
        }

        public AvatarSpecificSettings GetAvatarSettings(string fullPath)
        {
            if (!avatarSpecificSettings.ContainsKey(fullPath))
            {
                avatarSpecificSettings.Add(fullPath, new AvatarSpecificSettings());
            }

            return avatarSpecificSettings[fullPath];
        }
    }
}
