//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========
using UnityEngine;

namespace CustomAvatar.StereoRendering
{
#if (VIVE_STEREO_STEAMVR)
    public class SteamVRParamFactory : IDeviceParamFactory
    {
        public int GetRenderWidth()
        {
            return (int)SteamVR.instance.sceneWidth;
        }

        public int GetRenderHeight()
        {
            return (int)SteamVR.instance.sceneHeight;
        }

        public Vector3 GetEyeSeperation(int eye)
        {
            var eyePos = SteamVR.instance.eyes[eye].pos;
            eyePos.z = 0.0f;
            return eyePos;
        }

        public Quaternion GetEyeLocalRotation(int eye)
        {
            return SteamVR.instance.eyes[eye].rot;
        }

        public Matrix4x4 GetProjectionMatrix(int eye, float nearPlane, float farPlane)
        {
            return HMDMatrix4x4ToMatrix4x4(SteamVR.instance.hmd.GetProjectionMatrix((Valve.VR.EVREye)eye, nearPlane, farPlane));
        }

        // transform a SteamVR matrix format to Unity matrix format
        private Matrix4x4 HMDMatrix4x4ToMatrix4x4(Valve.VR.HmdMatrix44_t hmdMatrix)
        {
            Matrix4x4 m = Matrix4x4.identity;

            m[0, 0] = hmdMatrix.m0;
            m[0, 1] = hmdMatrix.m1;
            m[0, 2] = hmdMatrix.m2;
            m[0, 3] = hmdMatrix.m3;

            m[1, 0] = hmdMatrix.m4;
            m[1, 1] = hmdMatrix.m5;
            m[1, 2] = hmdMatrix.m6;
            m[1, 3] = hmdMatrix.m7;

            m[2, 0] = hmdMatrix.m8;
            m[2, 1] = hmdMatrix.m9;
            m[2, 2] = hmdMatrix.m10;
            m[2, 3] = hmdMatrix.m11;

            m[3, 0] = hmdMatrix.m12;
            m[3, 1] = hmdMatrix.m13;
            m[3, 2] = hmdMatrix.m14;
            m[3, 3] = hmdMatrix.m15;

            return m;
        }
    }
#endif
}
