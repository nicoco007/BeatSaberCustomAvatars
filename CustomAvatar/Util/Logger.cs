using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomAvatar.Util
{
	static class Logger
	{
		public static IPA.Logging.Logger logger;
		public enum LogLevel { Debug, Warning, Notice, Error, Critical };

		public static void Log(string m)
		{
			Log(m, LogLevel.Debug);
		}

		public static void Log(string m, LogLevel l)
		{
			Log(m, l, null);
		}

		public static void Log(string m, LogLevel l, string suggestedAction)
		{
			IPA.Logging.Logger.Level level = IPA.Logging.Logger.Level.Debug;
			switch (l)
			{
				case LogLevel.Debug: level = IPA.Logging.Logger.Level.Debug; break;
				case LogLevel.Notice: level = IPA.Logging.Logger.Level.Notice; break;
				case LogLevel.Warning: level = IPA.Logging.Logger.Level.Warning; break;
				case LogLevel.Error: level = IPA.Logging.Logger.Level.Error; break;
				case LogLevel.Critical: level = IPA.Logging.Logger.Level.Critical; break;
			}
			logger.Log(level, m);
			if (suggestedAction != null)
				logger.Log(level, $"Suggested Action: {suggestedAction}");
		}
	}
}
