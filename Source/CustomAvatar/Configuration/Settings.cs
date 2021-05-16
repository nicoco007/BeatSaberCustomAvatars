//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright � 2018-2021  Beat Saber Custom Avatars Contributors
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
        public ObservableValue<bool> isAvatarVisibleInFirstPerson { get; } = new ObservableValue<bool>();
        public ObservableValue<bool> moveFloorWithRoomAdjust { get; } = new ObservableValue<bool>();
        public ObservableValue<AvatarResizeMode> resizeMode { get; } = new ObservableValue<AvatarResizeMode>(AvatarResizeMode.Height);
        public ObservableValue<FloorHeightAdjust> floorHeightAdjust { get; } = new ObservableValue<FloorHeightAdjust>(FloorHeightAdjust.Off);
        public string previousAvatarPath { get; set; }
        public ObservableValue<float> playerArmSpan { get; } = new ObservableValue<float>(VRPlayerInput.kDefaultPlayerArmSpan);
        public bool calibrateFullBodyTrackingOnStart { get; set; }
        public ObservableValue<bool> enableLocomotion { get; } = new ObservableValue<bool>(true);
        public ObservableValue<float> cameraNearClipPlane { get; } = new ObservableValue<float>(0.1f);
        public bool showAvatarInSmoothCamera { get; set; } = true;
        public Lighting lighting { get; } = new Lighting();
        public Mirror mirror { get; } = new Mirror();
        public AutomaticFullBodyCalibration automaticCalibration { get; } = new AutomaticFullBodyCalibration();
        public FullBodyMotionSmoothing fullBodyMotionSmoothing { get; } = new FullBodyMotionSmoothing();

        [JsonProperty(PropertyName = "avatarSpecificSettings", Order = int.MaxValue)] private readonly Dictionary<string, AvatarSpecificSettings> _avatarSpecificSettings = new Dictionary<string, AvatarSpecificSettings>();

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
            public ShadowQuality shadowQuality { get; set; }
            public ShadowResolution shadowResolution { get; set; }
            public EnvironmentLighting environment { get; } = new EnvironmentLighting();
            public LightingGroup sabers { get; } = new LightingGroup();
        }

        public class LightingGroup
        {
            [JsonProperty(Order = int.MinValue)]
            public bool enabled { get; set; }
            public float intensity { get; set; } = 1;
        }

        public class EnvironmentLighting : LightingGroup
        {
            public EnvironmentLightingType type { get; set; } = EnvironmentLightingType.Dynamic;
            public int pixelLightCount { get; set; } = 2;
        }

        public class Mirror
        {
            public Vector2 size { get; set; } = new Vector2(4f, 2f);
            public float renderScale { get; set; } = 1.0f;
            public int antiAliasing { get; set; } = 2;
        }

        public class FullBodyMotionSmoothing
        {
            public TrackedPointSmoothing waist { get; } = new TrackedPointSmoothing { position = 0.5f, rotation = 0.2f };
            public TrackedPointSmoothing feet { get; } = new TrackedPointSmoothing { position = 0.5f, rotation = 0.2f };
        }

        public class TrackedPointSmoothing
        {
            public float position { get; set; }
            public float rotation { get; set; }
        }

        public class AutomaticFullBodyCalibration
        {
            public float legOffset { get; set; } = 0.15f;
            public float pelvisOffset { get; set; } = 0.1f;

            public WaistTrackerPosition waistTrackerPosition { get; set; }
        }

        public class AvatarSpecificSettings
        {
            public ObservableValue<bool> useAutomaticCalibration { get; } = new ObservableValue<bool>();
            public ObservableValue<bool> bypassCalibration { get; } = new ObservableValue<bool>();
            public ObservableValue<bool> ignoreExclusions { get; } = new ObservableValue<bool>(false);
            public bool allowMaintainPelvisPosition { get; set; } = false;
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
