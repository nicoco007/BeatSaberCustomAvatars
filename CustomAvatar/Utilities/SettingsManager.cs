using System;
using System.IO;
using Newtonsoft.Json;

namespace CustomAvatar.Utilities
{
    internal static class SettingsManager
    {
        public static readonly string kSettingsPath = Path.Combine(Environment.CurrentDirectory, "UserData", "CustomAvatars.json");
        public static Settings settings { get; private set; }

        public static void LoadSettings()
        {
            Plugin.logger.Info("Loading settings from " + kSettingsPath);

            if (!File.Exists(kSettingsPath))
            {
                Plugin.logger.Info("File does not exist, using default settings");

                settings = new Settings();
                return;
            }

            using (var reader = new StreamReader(kSettingsPath))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var serializer = GetSerializer();
                settings = serializer.Deserialize<Settings>(jsonReader);
            }
        }

        public static void SaveSettings()
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
                Converters = { new Vector3JsonConverter(), new QuaternionJsonConverter(), new PoseJsonConverter() }
            };
        }
    }
}
