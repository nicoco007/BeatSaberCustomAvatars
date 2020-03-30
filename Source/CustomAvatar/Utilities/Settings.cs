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
    // ReSharper disable UnusedMember.Global
    internal class Settings
    {
        public bool isAvatarVisibleInFirstPerson { get; set; } = true;
        [JsonConverter(typeof(StringEnumConverter))] public AvatarResizeMode resizeMode { get; set; } = AvatarResizeMode.Height;
        public bool enableFloorAdjust { get; set; } = false;
        public bool moveFloorWithRoomAdjust { get; set; } = false;
        public string previousAvatarPath { get; set; } = null;
        public float playerArmSpan { get; set; } = AvatarTailor.kDefaultPlayerArmSpan;
        public bool useAutomaticFullBodyCalibration { get; set; } = false;
        public bool calibrateFullBodyTrackingOnStart { get; set; } = false;
        public float cameraNearClipPlane { get; set; } = 0.1f;
        public Vector2 mirrorSize { get; set; } = new Vector2(5f, 2.5f);
        public float mirrorRenderScale { get; set; } = 1.0f;
        public FullBodyMotionSmoothing fullBodyMotionSmoothing { get; set; } = new FullBodyMotionSmoothing();
        public Dictionary<string, AvatarSpecificSettings> avatarSpecificSettings { get; set; } = new Dictionary<string, AvatarSpecificSettings>();
        
        public class FullBodyMotionSmoothing
        {
            public TrackedPointSmoothing waist { get; set; } = new TrackedPointSmoothing { position = 15, rotation = 10 };
            public TrackedPointSmoothing feet { get; set; } = new TrackedPointSmoothing { position = 13, rotation = 17 };
        }

        public class TrackedPointSmoothing
        {
            public float position { get; set; }
            public float rotation { get; set; }
        }

        public class FullBodyCalibration
        {
            public Pose leftLeg { get; set; } = Pose.identity;
            public Pose rightLeg { get; set; } = Pose.identity;
            public Pose pelvis { get; set; } = Pose.identity;
        }

        public class AvatarSpecificSettings
        {
            public FullBodyCalibration fullBodyCalibration { get; set; } = new FullBodyCalibration();
        }

        public FullBodyCalibration GetAvatarSettings(string fullPath)
        {
            if (!avatarSpecificSettings.ContainsKey(fullPath))
            {
                avatarSpecificSettings.Add(fullPath, new AvatarSpecificSettings());
            }

            return avatarSpecificSettings[fullPath].fullBodyCalibration;
        }
    }
}
