//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CustomAvatar.Utilities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;
using Zenject;

namespace CustomAvatar.Tracking.UnityXR
{
    // This implementation is heavily inspired by the Unity XR Hands sample.
    internal class UnityXRFingerTrackingProvider : IFingerTrackingProvider, ITickable
    {
        // these values are based on Index controllers and may not work as well with others
        private static readonly float[] kOpenFingerCurls = [-17f, 7f, 11f, 13f, 10f];
        private static readonly float[] kClosedFingerCurls = [136f, 291f, 291f, 292f, 287f];

        // Can't use XRHandSubsystem for field types since they'll fail to load if Unity.XR.Hands isn't installed.
        private readonly IList _subsystems = new List<XRHandSubsystem>();

        private readonly float[] _leftHandFingerCurls = new float[5];
        private readonly float[] _rightHandFingerCurls = new float[5];

        private readonly BeatSaberUtilities _beatSaberUtilities;

        private ISubsystem _subsystem;

        private bool _leftJointsTracked;
        private bool _rightJointsTracked;

        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private UnityXRFingerTrackingProvider(BeatSaberUtilities beatSaberUtilities)
        {
            _beatSaberUtilities = beatSaberUtilities;
        }

        public void Tick()
        {
            if (_subsystem != null && _subsystem.running)
            {
                return;
            }

            var subsystems = (List<XRHandSubsystem>)_subsystems;
            SubsystemManager.GetSubsystems(subsystems);
            XRHandSubsystem subsystem = subsystems.FirstOrDefault(s => s.running);

            if (subsystem == null)
            {
                return;
            }

            if (_subsystem != null)
            {
                ((XRHandSubsystem)_subsystem).updatedHands -= OnUpdatedHands;
            }

            _subsystem = subsystem;

            subsystem.updatedHands += OnUpdatedHands;
        }

        private void OnUpdatedHands(XRHandSubsystem handSubsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
        {
            // TODO: I'm not sure how I feel about putting this here. It's not the same layer of abstraction as TrackingRig,
            // where it's done for regular tracked devices (I would consider IDeviceProvider to be the same layer).
            if (!_beatSaberUtilities.hasFocus)
            {
                return;
            }

            // We have no game logic depending on the finger transforms, so early out here
            if (updateType == XRHandSubsystem.UpdateType.Dynamic)
            {
                return;
            }

            _leftJointsTracked = updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints);
            _rightJointsTracked = updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.RightHandJoints);

            UpdateJoints(handSubsystem.leftHand, _leftHandFingerCurls, _leftJointsTracked);
            UpdateJoints(handSubsystem.rightHand, _rightHandFingerCurls, _rightJointsTracked);
        }

        private void UpdateJoints(XRHand hand, float[] fingerCurls, bool areJointsTracked)
        {
            if (!areJointsTracked)
            {
                return;
            }

            NativeArray<XRHandJoint> joints = hand.m_Joints; // later versions of the Hands package make this public

            for (int fingerIndex = (int)XRHandFingerID.Thumb; fingerIndex <= (int)XRHandFingerID.Little; ++fingerIndex)
            {
                var fingerId = (XRHandFingerID)fingerIndex;
                int jointIndexBack = fingerId.GetBackJointID().ToIndex();
                int jointIndexFront = fingerId.GetFrontJointID().ToIndex();

                float fingerCurl = 0;

                if (!joints[jointIndexFront].TryGetPose(out Pose parentPose))
                {
                    continue;
                }

                //                                     Skip tip joint ↓
                for (int jointIndex = jointIndexFront + 1; jointIndex < jointIndexBack; ++jointIndex)
                {
                    if (!joints[jointIndex].TryGetPose(out Pose fingerJointPose))
                    {
                        continue;
                    }

                    // Finger joints rotate about the X axis in OpenXR; see https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#convention-of-hand-joints
                    float angle = Vector3.SignedAngle(parentPose.forward, fingerJointPose.forward, parentPose.right);
                    fingerCurl += ClampAngle180(angle);
                    parentPose = fingerJointPose;
                }

                fingerCurls[fingerIndex] = MapClamped(fingerCurl, kOpenFingerCurls[fingerIndex], kClosedFingerCurls[fingerIndex]);
            }
        }

        public bool TryGetFingerCurl(DeviceUse use, out FingerCurl curl)
        {
            if (use == DeviceUse.LeftHand)
            {
                curl = new FingerCurl(_leftHandFingerCurls[0], _leftHandFingerCurls[1], _leftHandFingerCurls[2], _leftHandFingerCurls[3], _leftHandFingerCurls[4]);
                return _leftJointsTracked;
            }
            else
            {
                curl = new FingerCurl(_rightHandFingerCurls[0], _rightHandFingerCurls[1], _rightHandFingerCurls[2], _rightHandFingerCurls[3], _rightHandFingerCurls[4]);
                return _rightJointsTracked;
            }
        }

        private static float ClampAngle180(float angle)
        {
            if (angle > 180f)
            {
                return angle - 360;
            }
            else
            {
                return angle;
            }
        }

        private static float MapClamped(float value, float from, float to)
        {
            return Mathf.Clamp01((value - from) / (to - from));
        }
    }
}

