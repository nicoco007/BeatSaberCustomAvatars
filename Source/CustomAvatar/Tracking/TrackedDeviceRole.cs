//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;

namespace CustomAvatar.Tracking
{
    internal class TrackedDeviceTypeAttribute : Attribute
    {
        public string Name { get; set; }

        public TrackedDeviceTypeAttribute(string name)
        {
            Name = name;
        }
    }

    internal enum TrackedDeviceRole
    {
        Unknown,
        [TrackedDeviceType("vive")] ViveHeadset,
        [TrackedDeviceType("knuckles")] ValveIndexController,
        [TrackedDeviceType("vive_tracker")] ViveTracker,
        [TrackedDeviceType("vive_tracker_handed")] HeldInHand,
        [TrackedDeviceType("vive_tracker_left_foot")] LeftFoot,
        [TrackedDeviceType("vive_tracker_right_foot")] RightFoot,
        [TrackedDeviceType("vive_tracker_left_shoulder")] LeftShoulder,
        [TrackedDeviceType("vive_tracker_right_shoulder")] RightShoulder,
        [TrackedDeviceType("vive_tracker_waist")] Waist,
        [TrackedDeviceType("vive_tracker_chest")] Chest,
        [TrackedDeviceType("vive_tracker_camera")] Camera,
        [TrackedDeviceType("vive_tracker_keyboard")] Keyboard,
        [TrackedDeviceType("kinect_device")] KinectToVrTracker
    }
}
