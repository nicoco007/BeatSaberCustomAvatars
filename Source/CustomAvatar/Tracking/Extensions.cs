using UnityEngine.XR;

namespace CustomAvatar.Tracking
{
    internal static class Extensions
    {
        public static bool HasCharacteristics(this InputDevice inputDevice, InputDeviceCharacteristics characteristics)
        {
            return (inputDevice.characteristics & characteristics) == characteristics;
        }
    }
}
