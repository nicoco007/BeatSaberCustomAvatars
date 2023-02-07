using IPA.Logging;

namespace CustomAvatar.Logging
{
    internal class IPALoggerFactory : ILoggerFactory
    {
        private readonly Logger _baseLogger;

        internal IPALoggerFactory(Logger logger)
        {
            _baseLogger = logger;
        }

        public ILogger<T> CreateLogger<T>(string name = null)
        {
            return new IPALogger<T>(_baseLogger, name);
        }
    }
}
