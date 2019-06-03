//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========
using UnityEngine;
#if (VIVE_STEREO_WAVEVR)
using wvr;
#endif

namespace CustomAvatar.StereoRendering
{
#if (VIVE_STEREO_WAVEVR)
    public class WaveVRParamFactory : IDeviceParamFactory
    {
        public int GetRenderWidth()
        {
#if UNITY_EDITOR
            return 1440;
#else
            return (int)WaveVR_Render.Instance.sceneWidth;
#endif
        }

        public int GetRenderHeight()
        {
#if UNITY_EDITOR
            return 1600;
#else
            return (int)WaveVR_Render.Instance.sceneHeight;
#endif
        }

        public Vector3 GetEyeSeperation(int eye)
        {
#if UNITY_EDITOR
            float ipd = WaveVR_Render.Instance.ipd;
            if (eye == 0)
            {
                return new Vector3(-0.5f * ipd, 0, 0);
            }
            else
            {
                return new Vector3(0.5f * ipd, 0, 0);
            }
#else
            return WaveVR_Render.Instance.eyes[eye].pos;
#endif
        }

        public Quaternion GetEyeLocalRotation(int eye)
        {
#if UNITY_EDITOR
            return Quaternion.identity;
#else
            return WaveVR_Render.Instance.eyes[eye].rot;
#endif
        }

        public Matrix4x4 GetProjectionMatrix(int eye, float nearPlane, float farPlane)
        {
            if (eye == 0)
            {
                var cam = WaveVR_Render.Instance.lefteye.GetComponent<Camera>();
                return cam.projectionMatrix;
            }
            else
            {
                var cam = WaveVR_Render.Instance.righteye.GetComponent<Camera>();
                return cam.projectionMatrix;
            }
        }
    }
#endif
        }
