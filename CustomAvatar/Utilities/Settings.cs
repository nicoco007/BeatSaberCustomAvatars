using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class Settings
    {
        public bool IsAvatarVisibleInFirstPerson { get; set; } = true;
        public AvatarResizeMode ResizeMode { get; set; } = AvatarResizeMode.Height;
        public bool EnableFloorAdjust { get; set; } = false;
        public string PreviousAvatarPath { get; set; }
        public float PlayerArmSpan { get; set; } = 1.7f;
        public bool CalibrateFullBodyTrackingOnStart { get; set; } = false;
        public float CameraNearClipPlane { get; set; } = 0.03f;
        public FullBodyMotionSmoothing FullBodyMotionSmoothing { get; set; } = new FullBodyMotionSmoothing();
    }

    public class FullBodyMotionSmoothing
    {
	    public TrackedPoint Waist { get; set; } = new TrackedPoint { Position = 15, Rotation = 10 };
	    public TrackedPoint Feet { get; set; } = new TrackedPoint { Position = 13, Rotation = 17 };
    }

    public class TrackedPoint
    {
	    public float Position { get; set; }
	    public float Rotation { get; set; }
    }
}
