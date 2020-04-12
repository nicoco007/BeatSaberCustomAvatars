using System;
using System.IO;
using CustomAvatar.Logging;
using Newtonsoft.Json;

namespace CustomAvatar.Utilities
{
    internal class SettingsManager
    {
        public readonly string kSettingsPath = Path.Combine(Environment.CurrentDirectory, "UserData", "CustomAvatars.json");

        private bool _hasReset;

        private ILogger _logger;

        private SettingsManager(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SettingsManager>();
        }

        public Settings Load()
        {
            _logger.Info("Loading settings from " + kSettingsPath);

            if (!File.Exists(kSettingsPath))
            {
                _logger.Info("File does not exist, using default settings");

                return new Settings();
            }

            try
            {
                using (var reader = new StreamReader(kSettingsPath))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var serializer = GetSerializer();
                    return serializer.Deserialize<Settings>(jsonReader) ?? new Settings();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load settings from file; using default settings");
                _logger.Error(ex);

                _hasReset = true;
                return new Settings();
            }
        }

        public void Save(Settings settings)
        {
            if (_hasReset) return;

            _logger.Info("Saving settings to " + kSettingsPath);

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
                Converters = { new Vector2JsonConverter(), new Vector3JsonConverter(), new QuaternionJsonConverter(), new PoseJsonConverter(), new FloatJsonConverter() }
            };
        }
    }
}
