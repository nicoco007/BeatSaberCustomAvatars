//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
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

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using CustomAvatar.Avatar;
using CustomAvatar.Configuration;
using CustomAvatar.Player;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class TrackingHelper
    {
        private readonly Settings _settings;
        private readonly BeatSaberUtilities _beatSaberUtilities;

        private TrackingHelper(Settings settings, BeatSaberUtilities beatSaberUtilities)
        {
            _settings = settings;
            _beatSaberUtilities = beatSaberUtilities;
        }

        public void SetLocalPose(Vector3 position, Quaternion rotation, Transform target, Transform parent = null)
        {
            ApplyLocalPose(ref position, ref rotation, parent);

            target.position = position;
            target.rotation = rotation;
        }

        public void ApplyLocalPose(ref Vector3 position, ref Quaternion rotation, Transform parent = null)
        {
            // if avatar transform has a parent, use that as the origin
            // this is like localPosition/localRotation but on the parent object
            if (parent)
            {
                position = parent.TransformPoint(position);
                rotation = parent.rotation * rotation;
            }
        }

        /// <summary>
        /// Applies the game's room adjustment to the specified position and rotation.
        /// For the inverse, see of <see cref="ApplyInverseRoomAdjust(ref Vector3, ref Quaternion)"/>.
        /// </summary>
        /// <param name="position">Position to adjust.</param>
        /// <param name="rotation">Rotation to adjust.</param>
        public void ApplyRoomAdjust(ref Vector3 position, ref Quaternion rotation)
        {
            Vector3 roomCenter = _beatSaberUtilities.roomCenter;
            Quaternion roomRotation = _beatSaberUtilities.roomRotation;

            position = roomCenter + roomRotation * position;
            rotation = roomRotation * rotation;

            if (_settings.moveFloorWithRoomAdjust)
            {
                position.y -= roomCenter.y;
            }
        }

        /// <summary>
        /// Applies the inverse of the game's room adjustment to the specified position and rotation.
        /// Inverse of <see cref="ApplyRoomAdjust(ref Vector3, ref Quaternion)"/>.
        /// </summary>
        /// <param name="position">Position to adjust.</param>
        /// <param name="rotation">Rotation to adjust.</param>
        public void ApplyInverseRoomAdjust(ref Vector3 position, ref Quaternion rotation)
        {
            Vector3 roomCenter = _beatSaberUtilities.roomCenter;
            Quaternion roomRotation = _beatSaberUtilities.roomRotation;

            position = Quaternion.Inverse(roomRotation) * (position - roomCenter);
            rotation = rotation * Quaternion.Inverse(roomRotation);
        }

        /// <summary>
        /// Move tracked point upwards based on the difference between avatar height and player height.
        /// This essentially moves the trackers up as if the player was on stilts.
        /// </summary>
        public void ApplyFloorOffset(SpawnedAvatar spawnedAvatar, ref Vector3 position)
        {
            if (_settings.floorHeightAdjust == FloorHeightAdjust.Off || !spawnedAvatar) return;

            position.y += spawnedAvatar.scaledEyeHeight - _beatSaberUtilities.GetRoomAdjustedPlayerEyeHeight();
        }

        /// <summary>
        /// Scales the vertical movement of a tracked point based on the quotient of avatar height and player height.
        /// This moves the trackers as if the player was the height of the avatar, but only vertically (no horizontal scaling).
        /// For the inverse, see of <see cref="ApplyInverseFloorScaling(SpawnedAvatar, ref Vector3)"/>.
        /// </summary>
        public void ApplyFloorScaling(SpawnedAvatar spawnedAvatar, ref Vector3 position)
        {
            if (_settings.floorHeightAdjust == FloorHeightAdjust.Off || !spawnedAvatar) return;

            position.y *= spawnedAvatar.scaledEyeHeight / _beatSaberUtilities.GetRoomAdjustedPlayerEyeHeight();
        }

        /// <summary>
        /// Scales the vertical movement of a tracked point based on the inverse of the quotient of avatar height and player height.
        /// Inverse of <see cref="ApplyFloorScaling(SpawnedAvatar, ref Vector3)"/>.
        /// </summary>
        public void ApplyInverseFloorScaling(SpawnedAvatar spawnedAvatar, ref Vector3 position)
        {
            if (_settings.floorHeightAdjust == FloorHeightAdjust.Off || !spawnedAvatar) return;

            position.y /= spawnedAvatar.scaledEyeHeight / _beatSaberUtilities.GetRoomAdjustedPlayerEyeHeight();
        }
    }
}
