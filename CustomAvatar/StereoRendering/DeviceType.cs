//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace CustomAvatar.StereoRendering
{
    // enum of supported device types
    public enum HmdType { Unsupported, SteamVR, OVR, WaveVR };

    public class StereoRenderDevice
    {
        public static HmdType GetHmdType()
        {
            HmdType type = HmdType.Unsupported;

#if (UNITY_ANDROID && VIVE_STEREO_WAVEVR)
            if (WaveVR_Render.Instance != null)
            {
                type = HmdType.WaveVR;
            }

            return type;
#else
            string deviceName = UnityEngine.XR.XRSettings.loadedDeviceName;

            if (deviceName == "OpenVR")
            {
                type = HmdType.SteamVR;
            }
            else if(deviceName == "Oculus")
            {
                type = HmdType.OVR;
            }

            return type;
#endif
        }

        public static IDeviceParamFactory InitParamFactory(HmdType hmdType)
        {
#if (VIVE_STEREO_STEAMVR)
            if (hmdType == HmdType.SteamVR)
            {
                return new SteamVRParamFactory();
            }
#endif

#if (VIVE_STEREO_OVR)
            if (hmdType == HmdType.OVR)
            {
                return new OVRParamFactory();
            }
#endif

#if (UNITY_ANDROID && VIVE_STEREO_WAVEVR)
            if (hmdType == HmdType.WaveVR)
            {
                return new WaveVRParamFactory();
            }
#endif

            return new UnityXRParamFactory();
        }

        public static bool IsNotUnityNativeSupport(HmdType type)
        {
            return type == HmdType.WaveVR;
        }
    }
}
