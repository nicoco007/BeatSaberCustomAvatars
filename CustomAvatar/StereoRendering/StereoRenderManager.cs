//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;

namespace CustomAvatar.StereoRendering
{
    [DisallowMultipleComponent]
    public class StereoRenderManager : MonoBehaviour
    {
        // singleton
        private static StereoRenderManager instance = null;

        // flags
        private static bool isApplicationQuitting = false;

        // device types
        public HmdType hmdType;

        // factory for device-specific things
        public IDeviceParamFactory paramFactory;

        // the camera that represents HMD
        private static GameObject mainCameraParent;
        private static Camera mainCamera;

        // all current stereo renderers
        public List<StereoRenderer> stereoRendererList = new List<StereoRenderer>();

        /////////////////////////////////////////////////////////////////////////////////
        // initialization

        // whehter we have initialized the singleton
        public static bool Active { get { return instance != null; } }

        // singleton interface
        public static StereoRenderManager Instance
        {
            get
            {
                Initialize();
                return instance;
            }
        }

        private static void Initialize()
        {
            if (Active || isApplicationQuitting) { return; }

            // try to get existing manager
            var instances = FindObjectsOfType<StereoRenderManager>();
            if (instances.Length > 0)
            {
                instance = instances[0];
                if (instances.Length > 1) { Debug.LogError("Multiple StereoRenderManager is not supported."); }
            }

            // pop warning if no VR device detected
            if (!UnityEngine.XR.XRSettings.enabled) { Debug.LogError("VR is not enabled for this application."); }

            // get HMD head
            Camera head = GetHmdRig();
            if (head == null) { return; }
            if (head.transform.parent == null)
            {
                Debug.LogError("HMD rig is not of proper hierarchy. You need a \"rig\" object as its root.");
                return;
            }

            // if no exsiting instance, attach a new one to HMD camera
            if (!Active)
            {
                instance = head.gameObject.AddComponent<StereoRenderManager>();
            }

            // record camera components
            if (Active)
            {
                mainCamera = head;
                mainCameraParent = head.transform.parent.gameObject;
            }
        }

        private static Camera GetHmdRig()
        {
            Camera target = null;

            if (Camera.main != null)
            {
                target = Camera.main;
            }
            else
            {
                Debug.LogError("No Camera tagged as \"MainCamera\" found.");
            }

            return target;
        }

        public void InitParamFactory()
        {
            // if not yet initialized
            if (paramFactory == null)
            {
                // get device type
                hmdType = StereoRenderDevice.GetHmdType();

                // create parameter factory
                paramFactory = StereoRenderDevice.InitParamFactory(hmdType);
                if (paramFactory == null)
                {
                    Debug.LogError("Current VR device is unsupported.");
                }
            }
        }

        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        /////////////////////////////////////////////////////////////////////////////////
        // callbacks

        public void AddToManager(StereoRenderer stereoRenderer)
        {
            stereoRendererList.Add(stereoRenderer);
        }

        public void RemoveFromManager(StereoRenderer stereoRenderer)
        {
            stereoRendererList.Remove(stereoRenderer);
        }
    }
}
