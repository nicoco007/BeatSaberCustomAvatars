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
    }
}
