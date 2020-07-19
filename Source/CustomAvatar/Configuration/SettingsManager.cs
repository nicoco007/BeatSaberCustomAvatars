using System;
using System.IO;
using CustomAvatar.Logging;
using Newtonsoft.Json;
using CustomAvatar.Utilities.Converters;
using System.Linq;

namespace CustomAvatar.Configuration
{
    internal class SettingsManager : IDisposable
    {
        public readonly string kSettingsPath = Path.Combine(Environment.CurrentDirectory, "UserData", "CustomAvatars.json");

        public Settings settings;

        private ILogger _logger;

        private SettingsManager(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger<SettingsManager>();

            Load();
        }

        public void Dispose()
        {
            Save();
        }

        public void Load()
        {
            _logger.Info("Loading settings from " + kSettingsPath);

            if (!File.Exists(kSettingsPath))
            {
                _logger.Info("File does not exist, using default settings");

                settings = new Settings();
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
            foreach (string fileName in settings.avatarSpecificSettings.Keys.ToList())
            {
                if (!File.Exists(Path.Combine(PlayerAvatarManager.kCustomAvatarsPath, fileName)) || Path.IsPathRooted(fileName))
                {
                    settings.avatarSpecificSettings.Remove(fileName);
                }
            }

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
                Converters = { new Vector2JsonConverter(), new Vector3JsonConverter(), new QuaternionJsonConverter(), new PoseJsonConverter(), new FloatJsonConverter(), new ColorJsonConverter() }
            };
        }
    }
}
