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
            /** @struct EyeData
			* A struct containing all data listed below.
			*/
            public struct EyeData
            {
                public bool no_user;
                /** The frame sequence.*/
                public int frame_sequence;
                /** The time when the frame was capturing. in millisecond.*/
                public int timestamp;
                public VerboseData verbose_data;
            }
        }
    }
}