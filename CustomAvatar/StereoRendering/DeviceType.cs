//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

namespace CustomAvatar.StereoRendering
{
    // enum of supported device types
    public enum HmdType { Unsupported, SteamVR, OVR, WaveVR, UnityXR };

    public class StereoRenderDevice
    {
        public static HmdType GetHmdType()
		{
            return HmdType.UnityXR;
        }

        public static IDeviceParamFactory InitParamFactory(HmdType hmdType)
        {
            return new UnityXRParamFactory();
        }

        public static bool IsNotUnityNativeSupport(HmdType type)
        {
            return type == HmdType.WaveVR;
        }
    }
}
