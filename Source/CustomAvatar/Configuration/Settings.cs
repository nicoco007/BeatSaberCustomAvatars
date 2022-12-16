//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using CustomAvatar.Avatar;
using CustomAvatar.Lighting;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace CustomAvatar.Configuration
{
    internal class Settings
    {
        public ObservableValue<bool> isAvatarVisibleInFirstPerson { get; } = new ObservableValue<bool>();
        public ObservableValue<bool> moveFloorWithRoomAdjust { get; } = new ObservableValue<bool>();
        public ObservableValue<AvatarResizeMode> resizeMode { get; } = new ObservableValue<AvatarResizeMode>(AvatarResizeMode.Height);
        public ObservableValue<FloorHeightAdjustMode> floorHeightAdjust { get; } = new ObservableValue<FloorHeightAdjustMode>(FloorHeightAdjustMode.Off);
        public string previousAvatarPath { get; set; }
        public ObservableValue<float> playerEyeHeight { get; } = new ObservableValue<float>(BeatSaberUtilities.kDefaultPlayerEyeHeight);
        public ObservableValue<float> playerArmSpan { get; } = new ObservableValue<float>(BeatSaberUtilities.kDefaultPlayerArmSpan);
        public bool calibrateFullBodyTrackingOnStart { get; set; }
        public ObservableValue<bool> enableLocomotion { get; } = new ObservableValue<bool>(true);
        public ObservableValue<float> cameraNearClipPlane { get; } = new ObservableValue<float>(0.1f);
        public ObservableValue<bool> showAvatarInSmoothCamera { get; } = new ObservableValue<bool>(true);
        public bool showAvatarInMirrors { get; set; } = true;
        public ObservableValue<SkinWeights> skinWeights { get; } = new ObservableValue<SkinWeights>(SkinWeights.FourBones);
        public Lighting lighting { get; } = new Lighting();
        public Mirror mirror { get; } = new Mirror();
        public AutomaticFullBodyCalibration automaticCalibration { get; } = new AutomaticFullBodyCalibration();

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
            public int pixelLightCount { get; set; } = 2;
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
        }

        public class Mirror
        {
            public ObservableValue<float> renderScale { get; } = new ObservableValue<float>(1);
            public ObservableValue<int> antiAliasingLevel { get; } = new ObservableValue<int>(1);
            public bool renderInExternalCameras { get; set; } = false;
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
