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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using CustomAvatar.Avatar;
using CustomAvatar.Player;
using CustomAvatar.Lighting;
using CustomAvatar.Tracking;
using Newtonsoft.Json;
using UnityEngine;

namespace CustomAvatar.Configuration
{
    internal class Settings
    {
        public readonly ObservableValue<bool> isAvatarVisibleInFirstPerson = new ObservableValue<bool>();
        public readonly ObservableValue<bool> moveFloorWithRoomAdjust = new ObservableValue<bool>();
        public readonly ObservableValue<AvatarResizeMode> resizeMode = new ObservableValue<AvatarResizeMode>(AvatarResizeMode.Height);
        public readonly ObservableValue<FloorHeightAdjust> floorHeightAdjust = new ObservableValue<FloorHeightAdjust>(FloorHeightAdjust.Off);
        public string previousAvatarPath = null;
        public readonly ObservableValue<float> playerArmSpan = new ObservableValue<float>(VRPlayerInput.kDefaultPlayerArmSpan);
        public bool calibrateFullBodyTrackingOnStart = false;
        public readonly ObservableValue<bool> enableLocomotion = new ObservableValue<bool>();
        public float cameraNearClipPlane = 0.1f;
        public bool showAvatarInSmoothCamera = true;
        public readonly Lighting lighting = new Lighting();
        public readonly Mirror mirror = new Mirror();
        public readonly AutomaticFullBodyCalibration automaticCalibration = new AutomaticFullBodyCalibration();
        public readonly FullBodyMotionSmoothing fullBodyMotionSmoothing = new FullBodyMotionSmoothing();

        [JsonProperty(PropertyName = "avatarSpecificSettings", Order = int.MaxValue)] private Dictionary<string, AvatarSpecificSettings> _avatarSpecificSettings = new Dictionary<string, AvatarSpecificSettings>();

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            RemoveInvalidAvatarSettings();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            RemoveInvalidAvatarSettings();
        }

        private void RemoveInvalidAvatarSettings()
        {
            foreach (string fileName in _avatarSpecificSettings.Keys.ToList())
            {
                if (!File.Exists(Path.Combine(PlayerAvatarManager.kCustomAvatarsPath, fileName)) || Path.IsPathRooted(fileName))
                {
                    _avatarSpecificSettings.Remove(fileName);
                }
            }
        }

        public class Lighting
        {
            public LightingLevel level = LightingLevel.Off;
            public ShadowQuality shadowQuality = ShadowQuality.Disable;
            public ShadowLevel shadowLevel = ShadowLevel.Directional;
            public ShadowResolution shadowResolution = ShadowResolution.Low;
            public int pixelLightCount = 2;
        }

        public class Mirror
        {
            public Vector2 size = new Vector2(5f, 2.5f);
            public float renderScale = 1.0f;
        }

        public class FullBodyMotionSmoothing
        {
            public readonly TrackedPointSmoothing waist = new TrackedPointSmoothing { position = 0.5f, rotation = 0.2f };
            public readonly TrackedPointSmoothing feet = new TrackedPointSmoothing { position = 0.5f, rotation = 0.2f };
        }

        public class TrackedPointSmoothing
        {
            public float position;
            public float rotation;
        }

        public class AutomaticFullBodyCalibration
        {
            public float legOffset = 0.15f;
            public float pelvisOffset = 0.1f;

            public WaistTrackerPosition waistTrackerPosition = WaistTrackerPosition.Front;
        }

        public class AvatarSpecificSettings
        {
            public readonly ObservableValue<bool> useAutomaticCalibration = new ObservableValue<bool>();
            public readonly ObservableValue<bool> bypassCalibration = new ObservableValue<bool>();
            public readonly ObservableValue<bool> ignoreExclusions = new ObservableValue<bool>(false);
            public bool allowMaintainPelvisPosition = false;
        }

        public AvatarSpecificSettings GetAvatarSettings(string fileName)
        {
            if (!_avatarSpecificSettings.ContainsKey(fileName))
            {
                _avatarSpecificSettings.Add(fileName, new AvatarSpecificSettings());
            }

            return _avatarSpecificSettings[fileName];
        }
    }
}
