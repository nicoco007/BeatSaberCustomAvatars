//========= Copyright 2018, HTC Corporation. All rights reserved. ===========
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ViveSR
{
    namespace anipal
    {
        namespace Eye
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct SingleEyeExpression
            {
                public float eye_wide; /*!<A value representing how open eye widely.*/
                public float eye_squeeze; /*!<A value representing how the eye is closed tightly.*/
                public float eye_frown; /*!<A value representing user's frown.*/
            };

            [StructLayout(LayoutKind.Sequential)]
            public struct EyeExpression
            {
                public SingleEyeExpression left;
                public SingleEyeExpression right;
            };

            [StructLayout(LayoutKind.Sequential)]
            /** @struct EyeData
			* A struct containing all data listed below.
			*/
            public struct EyeData_v2
            {
                /** Indicate if there is a user in front of HMD. */
                public bool no_user;
                /** The frame sequence.*/
                public int frame_sequence;
                /** The time when the frame was capturing. in millisecond.*/
                public int timestamp;
                public VerboseData verbose_data;
                public EyeExpression expression_data;
            }
        }
    }
}