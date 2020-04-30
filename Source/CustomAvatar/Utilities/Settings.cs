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
        public AutomaticFullBodyCalibration automaticCalibration { get; private set; } = new AutomaticFullBodyCalibration();
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

        public class ManualFullBodyCalibration
        {
            public Pose leftLeg = Pose.identity;
            public Pose rightLeg = Pose.identity;
            public Pose pelvis = Pose.identity;

            [JsonIgnore] public bool isDefault => leftLeg.Equals(Pose.identity) && rightLeg.Equals(Pose.identity) && pelvis.Equals(Pose.identity);
        }

        public class AutomaticFullBodyCalibration
        {
            public Pose leftLeg = Pose.identity;
            public Pose rightLeg = Pose.identity;
            public Pose pelvis = Pose.identity;

            public float leftLegOffset = 0.15f;
            public float rightLegOffset = 0.15f;
            public float pelvisOffset = 0.1f;

            [JsonIgnore] public bool isDefault => leftLeg.Equals(Pose.identity) && rightLeg.Equals(Pose.identity) && pelvis.Equals(Pose.identity);
        }

        public class AvatarSpecificSettings
        {
            public ManualFullBodyCalibration fullBodyCalibration { get; private set; } = new ManualFullBodyCalibration();
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
