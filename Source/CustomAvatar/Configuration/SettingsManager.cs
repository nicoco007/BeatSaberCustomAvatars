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

using System;
using System.IO;
using CustomAvatar.Logging;
using Newtonsoft.Json;
using CustomAvatar.Utilities.Converters;
using Newtonsoft.Json.Converters;
using CustomAvatar.Avatar;
using CustomAvatar.Player;

namespace CustomAvatar.Configuration
{
    internal class SettingsManager : IDisposable
    {
        public readonly string kSettingsPath = Path.Combine(Environment.CurrentDirectory, "UserData", "CustomAvatars.json");

        public Settings settings;

        private ILogger<SettingsManager> _logger;

        private SettingsManager(ILogger<SettingsManager> logger)
        {
            _logger = logger;

            Load();
        }
        
        public void Dispose()
        {
            Save();
        }

        public void Load()
        {
            _logger.Info($"Loading settings from '{kSettingsPath}'");

            if (!File.Exists(kSettingsPath))
            {
                _logger.Info("File does not exist, using default settings");

                settings = new Settings();

                return;
            }

            try
            {
                using (var reader = new StreamReader(kSettingsPath))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var serializer = GetSerializer();
                    settings = serializer.Deserialize<Settings>(jsonReader) ?? new Settings();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load settings from file; using default settings");
                _logger.Error(ex);

                settings = new Settings();
            }
        }

        public void Save()
        {
            _logger.Info($"Saving settings to '{kSettingsPath}'");

            using (var writer = new StreamWriter(kSettingsPath))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                var serializer = GetSerializer();
                serializer.Serialize(jsonWriter, settings);
                jsonWriter.Flush();
            }
        }

        private JsonSerializer GetSerializer()
        {
            return new JsonSerializer
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = {
                    new StringEnumConverter(),
                    new FloatJsonConverter(),
                    new Vector2JsonConverter(),
                    new ObservableValueJsonConverter<bool>(),
                    new ObservableValueJsonConverter<float>(),
                    new ObservableValueJsonConverter<AvatarResizeMode>(),
                    new ObservableValueJsonConverter<FloorHeightAdjust>()
                }
            };
        }
    }
}
