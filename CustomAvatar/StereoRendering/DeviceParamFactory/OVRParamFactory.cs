//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========
using UnityEngine;

namespace CustomAvatar.StereoRendering
{
#if (VIVE_STEREO_OVR)
    public class OVRParamFactory : IDeviceParamFactory
    {
        public int GetRenderWidth()
        {
#if UNITY_2017_2_OR_NEWER
            var renderDesc = OVRManager.display.GetEyeRenderDesc(UnityEngine.XR.XRNode.LeftEye);
            return (int)renderDesc.resolution.x;
#else
            var renderDesc = OVRManager.display.GetEyeRenderDesc(UnityEngine.VR.VRNode.LeftEye);
            return (int)renderDesc.resolution.x;
#endif
        }

        public int GetRenderHeight()
        {
#if UNITY_2017_2_OR_NEWER
            var renderDesc = OVRManager.display.GetEyeRenderDesc(UnityEngine.XR.XRNode.LeftEye);
            return (int)renderDesc.resolution.y;
#else
            var renderDesc = OVRManager.display.GetEyeRenderDesc(UnityEngine.VR.VRNode.LeftEye);
            return (int)renderDesc.resolution.y;
#endif
        }

        public Vector3 GetEyeSeperation(int eye)
        {
            if (eye == 0)
            {
                return new Vector3(-0.03283963f, 0, 0);
            }
            else
            {
                return new Vector3(0.03283963f, 0, 0);
            }
        }

        public Quaternion GetEyeLocalRotation(int eye)
        {
            return Quaternion.identity;
        }

        public Matrix4x4 GetProjectionMatrix(int eye, float nearPlane, float farPlane)
        {
            var ovrCamera = OVRManager.instance.gameObject.GetComponent<OVRCameraRig>();

            Matrix4x4 projMat = Matrix4x4.identity;
            switch (eye)
            {
                case 0:
                    projMat = ovrCamera.leftEyeCamera.projectionMatrix;
                    break;
                case 1:
                    projMat = ovrCamera.rightEyeCamera.projectionMatrix;
                    break;
            }

            return projMat;
        }
    }
#endif
}
