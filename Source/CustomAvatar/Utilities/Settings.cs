using System;
using System.Collections.Generic;
using CustomAvatar.Tracking;
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
        public Lighting lighting { get; private set; } = new Lighting();
        public Mirror mirror { get; private set; } = new Mirror();
        public AutomaticFullBodyCalibration automaticCalibration { get; private set; } = new AutomaticFullBodyCalibration();
        public FullBodyMotionSmoothing fullBodyMotionSmoothing { get; private set; } = new FullBodyMotionSmoothing();
        [JsonProperty(Order = int.MaxValue)] internal Dictionary<string, AvatarSpecificSettings> avatarSpecificSettings = new Dictionary<string, AvatarSpecificSettings>();

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

            [JsonIgnore] public bool isCalibrated => !leftLeg.Equals(Pose.identity) || !rightLeg.Equals(Pose.identity) || !pelvis.Equals(Pose.identity);
        }

        public class AutomaticFullBodyCalibration
        {
            public Pose leftLeg = Pose.identity;
            public Pose rightLeg = Pose.identity;
            public Pose pelvis = Pose.identity;

            public float legOffset = 0.15f;
            public float pelvisOffset = 0.1f;

            public WaistTrackerPosition waistTrackerPosition = WaistTrackerPosition.Front;

            [JsonIgnore] public bool isCalibrated => !leftLeg.Equals(Pose.identity) || !rightLeg.Equals(Pose.identity) || !pelvis.Equals(Pose.identity);
        }

        public class AvatarSpecificSettings
        {
            public ManualFullBodyCalibration fullBodyCalibration { get; private set; } = new ManualFullBodyCalibration();
            public bool useAutomaticCalibration = false;
            public bool allowMaintainPelvisPosition = false;
            public bool bypassCalibration = false;
        }

        public AvatarSpecificSettings GetAvatarSettings(string fileName)
        {
            if (!avatarSpecificSettings.ContainsKey(fileName))
            {
                avatarSpecificSettings.Add(fileName, new AvatarSpecificSettings());
            }

            return avatarSpecificSettings[fileName];
        }
    }
}
