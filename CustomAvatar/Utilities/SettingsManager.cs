using System;
using System.IO;
using Newtonsoft.Json;

namespace CustomAvatar.Utilities
{
	internal static class SettingsManager
	{
		public static readonly string SettingsPath = Path.Combine(Environment.CurrentDirectory, "UserData", "CustomAvatars.json");
		public static Settings Settings { get; private set; }

		public static void LoadSettings()
		{
			Plugin.Logger.Info("Loading settings from " + SettingsPath);

			if (!File.Exists(SettingsPath))
			{
				Plugin.Logger.Info("File does not exist, using default settings");

				Settings = new Settings();
				return;
			}

			using (var reader = new StreamReader(SettingsPath))
			using (var jsonReader = new JsonTextReader(reader))
			{
				var serializer = new JsonSerializer();
				Settings = serializer.Deserialize<Settings>(jsonReader);
			}
		}

		public static void SaveSettings()
		{
			Plugin.Logger.Info("Saving settings to " + SettingsPath);

			using (var writer = new StreamWriter(SettingsPath))
			using (var jsonWriter = new JsonTextWriter(writer))
			{
				var serializer = new JsonSerializer() { Formatting = Formatting.Indented };
				serializer.Serialize(jsonWriter, Settings);
				jsonWriter.Flush();
			}
		}
	}
}
