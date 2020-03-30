using System;
using System.IO;
using Newtonsoft.Json;

namespace CustomAvatar.Utilities
{
    internal class SettingsManager
    {
        public static readonly string kSettingsPath = Path.Combine(Environment.CurrentDirectory, "UserData", "CustomAvatars.json");

        public static Settings settings { get; private set; }

        public static void Load()
        {
            Plugin.logger.Info("Loading settings from " + kSettingsPath);

            if (!File.Exists(kSettingsPath))
            {
                Plugin.logger.Info("File does not exist, using default settings");

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
                Plugin.logger.Error("Failed to load settings from file; using default settings");
                Plugin.logger.Error(ex);

                settings = new Settings();
            }
        }

        public static void Save()
        {
            Plugin.logger.Info("Saving settings to " + kSettingsPath);

            using (var writer = new StreamWriter(kSettingsPath))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                var serializer = GetSerializer();
                serializer.Serialize(jsonWriter, settings);
                jsonWriter.Flush();
            }
        }

        private static JsonSerializer GetSerializer()
        {
            return new JsonSerializer
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new Vector2JsonConverter(), new Vector3JsonConverter(), new QuaternionJsonConverter(), new PoseJsonConverter() }
            };
        }
    }
}
