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
            #region EyeShape
            public enum EyeShape
            {
                None = -1,
                Eye_Left_Blink = 0,
                Eye_Left_Wide,
                Eye_Left_Right,
                Eye_Left_Left,
                Eye_Left_Up,
                Eye_Left_Down,
                Eye_Right_Blink = 6,
                Eye_Right_Wide,
                Eye_Right_Right,
                Eye_Right_Left,
                Eye_Right_Up,
                Eye_Right_Down,
                Eye_Frown = 12,
                Max = 13,
            }

            [Serializable]
            public class EyeShapeTable
            {
                public SkinnedMeshRenderer skinnedMeshRenderer;
                public EyeShape[] eyeShapes;
            }
            #endregion

            #region VerboseData
            public enum EyeIndex { LEFT, RIGHT, }
            public enum GazeIndex { LEFT, RIGHT, COMBINE }

            /** @enum SingleEyeDataValidity
			An enum type for getting validity from the structure: eye data's bitmask
			*/
            public enum SingleEyeDataValidity : int
            {
                /** The validity of the origin of gaze of the eye data */
                SINGLE_EYE_DATA_GAZE_ORIGIN_VALIDITY,
                /** The validity of the direction of gaze of the eye data */
                SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY,
                /** The validity of the diameter of gaze of the eye data */
                SINGLE_EYE_DATA_PUPIL_DIAMETER_VALIDITY,
                /** The validity of the openness of the eye data */
                SINGLE_EYE_DATA_EYE_OPENNESS_VALIDITY,
                /** The validity of normalized position of pupil */
                SINGLE_EYE_DATA_PUPIL_POSITION_IN_SENSOR_AREA_VALIDITY
            };

            public enum TrackingImprovement : int
            {
                TRACKING_IMPROVEMENT_USER_POSITION_HMD,
                TRACKING_IMPROVEMENT_CALIBRATION_CONTAINS_POOR_DATA,
                TRACKING_IMPROVEMENT_CALIBRATION_DIFFERENT_BRIGHTNESS,
                TRACKING_IMPROVEMENT_IMAGE_QUALITY,
                TRACKING_IMPROVEMENT_INCREASE_EYE_RELIEF,
            };

            [StructLayout(LayoutKind.Sequential)]
            public struct TrackingImprovements
            {
                public int count;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
                public TrackingImprovement[] items;
            };

            /** @struct SingleEyeData
			* A struct containing status related an eye.
			* @image html EyeData.png width=1040px height=880px
			*/
            [StructLayout(LayoutKind.Sequential)]
            public struct SingleEyeData
            {
                /** The bits containing all validity for this frame.*/
                public System.UInt64 eye_data_validata_bit_mask;
                /** The point in the eye from which the gaze ray originates in meter miles.(right-handed coordinate system)*/
                public Vector3 gaze_origin_mm;
                /** The normalized gaze direction of the eye in [0,1].(right-handed coordinate system)*/
                public Vector3 gaze_direction_normalized;
                /** The diameter of the pupil in meter miles*/
                public float pupil_diameter_mm;
                /** A value representing how open the eye is.*/
                public float eye_openness;
                /** The normalized position of a pupil in [0,1]*/
                public Vector2 pupil_position_in_sensor_area;

                public bool GetValidity(SingleEyeDataValidity validity)
                {
                    return (eye_data_validata_bit_mask & (ulong)(1 << (int)validity)) > 0;
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct CombinedEyeData
            {
                public SingleEyeData eye_data;
                public bool convergence_distance_validity;
                public float convergence_distance_mm;
            }

            [StructLayout(LayoutKind.Sequential)]
            /** @struct VerboseData
			* A struct containing all data listed below.
			*/
            public struct VerboseData
            {
                /** A instance of the struct as @ref EyeData related to the left eye*/
                public SingleEyeData left;
                /** A instance of the struct as @ref EyeData related to the right eye*/
                public SingleEyeData right;
                /** A instance of the struct as @ref EyeData related to the combined eye*/
                public CombinedEyeData combined;
                public TrackingImprovements tracking_improvements;
            }
            #endregion

            #region EyeParameter
            [StructLayout(LayoutKind.Sequential)]
            /** @struct GazeRayParameter
			* A struct containing all data listed below.
			*/
            public struct GazeRayParameter
            {
                /** The sensitive factor of gaze ray in [0,1]. The bigger factor is, the more sensitive the gaze ray is.*/
                public double sensitive_factor;
            };

            [StructLayout(LayoutKind.Sequential)]
            /** @struct EyeParameter
			* A struct containing all data listed below.
			*/
            public struct EyeParameter
            {
                public GazeRayParameter gaze_ray_parameter;
            };
            #endregion

            #region FocusInfo
            /// <summary>
            /// The data structure to indicate Focus information
            /// </summary>
            public struct FocusInfo
            {
                public Vector3 point;
                public Vector3 normal;
                public float distance;
                public Collider collider;
                public Rigidbody rigidbody;
                public Transform transform;
            }
            #endregion

            #region CalibrationResult
            public enum CalibrationResult
            {
                SUCCESS,
                FAIL,
                BUSY,
            }
            #endregion
        }
    }
}