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
        public static readonly string kSettingsPath = Path.Combine(UnityGame.UserDataPath, "CustomAvatars.json");

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

            using (var reader = new StreamReader(kSettingsPath))
            using (var jsonReader = new JsonTextReader(reader))
            {
                settings = _jsonSerializer.Deserialize<Settings>(jsonReader) ?? new Settings();
            }
        }

        public void Save()
        {
            _logger.LogInformation($"Saving settings to '{kSettingsPath}'");

            using (var writer = new StreamWriter(kSettingsPath))
            using (var jsonWriter = new JsonTextWriter(writer))
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
