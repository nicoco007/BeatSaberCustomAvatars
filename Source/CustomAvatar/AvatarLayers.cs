namespace CustomAvatar
{
    public static class AvatarLayers
    {
        public static readonly int kAlwaysVisible = 10; // Beat Saber's "Avatar" layer
        public static readonly int kOnlyInThirdPerson = 20;

        public static readonly int kAlwaysVisibleMask = 1 << kAlwaysVisible;
        public static readonly int kOnlyInThirdPersonMask = 1 << kOnlyInThirdPerson;
        public static readonly int kAllLayersMask = kAlwaysVisibleMask | kOnlyInThirdPersonMask;
    }
}
