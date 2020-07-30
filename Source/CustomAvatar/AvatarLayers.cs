namespace CustomAvatar
{
    public static class AvatarLayers
    {
        public const int kAlwaysVisible = 10; // Beat Saber's "Avatar" layer
        public const int kOnlyInThirdPerson = 20;

        public const int kAlwaysVisibleMask = 1 << kAlwaysVisible;
        public const int kOnlyInThirdPersonMask = 1 << kOnlyInThirdPerson;
        public const int kAllLayersMask = kAlwaysVisibleMask | kOnlyInThirdPersonMask;
    }
}
