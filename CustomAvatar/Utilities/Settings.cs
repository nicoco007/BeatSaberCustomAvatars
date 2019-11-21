using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class Settings
    {
        public bool isAvatarVisibleInFirstPerson { get; set; } = true;
        public AvatarResizeMode resizeMode { get; set; } = AvatarResizeMode.Height;
        public bool enableFloorAdjust { get; set; } = false;
        public string previousAvatarPath { get; set; }
        public float playerArmSpan { get; set; } = 1.7f;
        public bool calibrateFullBodyTrackingOnStart { get; set; } = false;
        public float cameraNearClipPlane { get; set; } = 0.03f;
        public FullBodyMotionSmoothing fullBodyMotionSmoothing { get; set; } = new FullBodyMotionSmoothing();
    }

    public class FullBodyMotionSmoothing
    {
	    public TrackedPoint waist { get; set; } = new TrackedPoint { position = 15, rotation = 10 };
	    public TrackedPoint feet { get; set; } = new TrackedPoint { position = 13, rotation = 17 };
    }

    public class TrackedPoint
    {
	    public float position { get; set; }
	    public float rotation { get; set; }
    }
}
