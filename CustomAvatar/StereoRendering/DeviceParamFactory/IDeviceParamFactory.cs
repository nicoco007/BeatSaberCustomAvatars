//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========
using UnityEngine;

namespace CustomAvatar.StereoRendering
{
    // Interface for getting device-specific parameters 
    public interface IDeviceParamFactory
    {
        /// <summary>
        /// Get width of the texture which would be presented to HMD (single eye).
        /// </summary>
        int GetRenderWidth();

        /// <summary>
        /// Get height of the texture which would be presented to HMD (single eye).
        /// </summary>
        int GetRenderHeight();
        
        /// <summary>
        /// Get eye position relative to head coordinates.
        /// </summary>
        /// <param name="eye">Current rendering eye, 0 = left, 1 = right.</param>
        Vector3 GetEyeSeperation(int eye);

        /// <summary>
        /// Get eye rotation relative to head coordinates.
        /// </summary>
        /// <param name="eye">Current rendering eye, 0 = left, 1 = right.</param>
        Quaternion GetEyeLocalRotation(int eye);

        /// <summary>
        /// Get camera projection matrix for each eye.
        /// </summary>
        /// <param name="eye">Current rendering eye, 0 = left, 1 = right.</param>
        /// <param name="nearPlane">Camera near clip plane.</param>
        /// <param name="farPlane">Camera far clip plane.</param>
        Matrix4x4 GetProjectionMatrix(int eye, float nearPlane, float farPlane);
    }
}
