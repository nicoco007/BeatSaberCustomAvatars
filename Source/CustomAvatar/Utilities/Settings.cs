using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class Settings
    {
        public bool isAvatarVisibleInFirstPerson = true;
        public AvatarResizeMode resizeMode = AvatarResizeMode.Height;
        public bool enableFloorAdjust = false;
        public bool moveFloorWithRoomAdjust = false;
        public string previousAvatarPath = null;
        public float playerArmSpan = 1.7f;
        public bool calibrateFullBodyTrackingOnStart = false;
        public float cameraNearClipPlane = 0.1f;
        public float eyeTrackingScale = 1.0f;
        public FullBodyMotionSmoothing fullBodyMotionSmoothing = new FullBodyMotionSmoothing();
        public FullBodyCalibration fullBodyCalibration = new FullBodyCalibration();

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
        }
    }
}
