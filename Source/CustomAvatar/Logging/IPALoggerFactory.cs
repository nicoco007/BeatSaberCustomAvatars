using UnityEngine;
using Logger = IPA.Logging.Logger;

namespace CustomAvatar.Logging
{
    internal class IPALoggerFactory : ILoggerFactory
    {
        private Logger _logger;

        private IPALoggerFactory(Logger logger)
        {
            _logger = logger;
        }
        
        public ILogger CreateLogger<T>(string name = null)
        {
            return new IPALogger<T>(_logger, name);
        }
    }
}
