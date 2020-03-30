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
    // ReSharper disable FieldCanBeMadeReadOnly.Global
    // ReSharper disable InconsistentNaming
    internal class Settings
    {
        public bool isAvatarVisibleInFirstPerson = true;
        [JsonConverter(typeof(StringEnumConverter))] public AvatarResizeMode resizeMode = AvatarResizeMode.Height;
        public bool enableFloorAdjust = false;
        public bool moveFloorWithRoomAdjust = false;
        public string previousAvatarPath = null;
        public float playerArmSpan = AvatarTailor.kDefaultPlayerArmSpan;
        public bool calibrateFullBodyTrackingOnStart = false;
        public float cameraNearClipPlane = 0.1f;
        public Vector2 mirrorSize = new Vector2(5f, 2.5f);
        public float mirrorRenderScale = 1.0f;
        public FullBodyMotionSmoothing fullBodyMotionSmoothing = new FullBodyMotionSmoothing();
        [JsonProperty] private Dictionary<string, AvatarSpecificSettings> avatarSpecificSettings = new Dictionary<string, AvatarSpecificSettings>();
        
        public class FullBodyMotionSmoothing
        {
            public TrackedPointSmoothing waist = new TrackedPointSmoothing { position = 15, rotation = 10 };
            public TrackedPointSmoothing feet = new TrackedPointSmoothing { position = 13, rotation = 17 };
        }

        public class TrackedPointSmoothing
        {
            public float position;
            public float rotation;
        }

        public class FullBodyCalibration
        {
            public Pose leftLeg = Pose.identity;
            public Pose rightLeg = Pose.identity;
            public Pose pelvis = Pose.identity;

            [JsonIgnore] public bool isDefault => leftLeg.Equals(Pose.identity) && rightLeg.Equals(Pose.identity) && pelvis.Equals(Pose.identity);
        }

        public class AvatarSpecificSettings
        {
            public FullBodyCalibration fullBodyCalibration = new FullBodyCalibration();
            public bool useAutomaticCalibration = false;
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
