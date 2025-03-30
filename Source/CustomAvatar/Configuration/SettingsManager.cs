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

using System.IO;
using CustomAvatar.Logging;
using CustomAvatar.Utilities.Converters;
using IPA.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace CustomAvatar.Configuration
{
    internal class SettingsManager
    {
        public static readonly string kSettingsPath = Path.Join(UnityGame.UserDataPath, "CustomAvatars.json");

        private readonly ILogger<SettingsManager> _logger;
        private readonly JsonSerializer _jsonSerializer;

        public SettingsManager(ILogger<SettingsManager> logger)
        {
            _logger = logger;
            _jsonSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
            {
                Converters = {
                    new StringEnumConverter(),
                    new FloatJsonConverter(),
                    new Vector2JsonConverter(),
                    new ObservableValueJsonConverter()
                },
                Error = HandleDeserializationError,
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            });
        }

        public Settings settings { get; private set; }

        public void Load()
        {
            if (!File.Exists(kSettingsPath))
            {
                settings = new Settings();
                return;
            }

            _logger.LogInformation($"Loading settings from '{kSettingsPath}'");

            using (StreamReader reader = new(kSettingsPath))
            using (JsonTextReader jsonReader = new(reader))
            {
                settings = _jsonSerializer.Deserialize<Settings>(jsonReader) ?? new Settings();
            }
        }

        public void Save()
        {
            _logger.LogInformation($"Saving settings to '{kSettingsPath}'");

            using (StreamWriter writer = new(kSettingsPath))
            using (JsonTextWriter jsonWriter = new(writer))
            {
                _jsonSerializer.Serialize(jsonWriter, settings);
                jsonWriter.Flush();
            }
        }

        private void HandleDeserializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
        {
            ErrorContext ctx = e.ErrorContext;

            _logger.LogError($"Failed to deserialize property '{ctx.Path}': {ctx.Error}");

            ctx.Handled = true;
        }
    }
}
