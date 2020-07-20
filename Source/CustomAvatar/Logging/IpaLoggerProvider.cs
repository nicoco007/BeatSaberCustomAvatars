using Logger = IPA.Logging.Logger;

namespace CustomAvatar.Logging
{
    internal class IPALoggerProvider : ILoggerProvider
    {
        private readonly Logger _logger;

        private IPALoggerProvider(Logger logger)
        {
            _logger = logger;
        }
        
        public ILogger<T> CreateLogger<T>(string name = null)
        {
            return new IPALogger<T>(_logger, name);
        }
    }
}
