using IPA.Logging;

namespace CustomAvatar.Logging
{
    internal class IPALogger<T> : ILogger
    {
        private string _name;
        private Logger _logger;

        public IPALogger(Logger logger, string name = null)
        {
            _logger = logger;
            _name = name;
        }

        public void Trace(object message)
        {
            _logger.Trace(FormatMessage(message));
        }

        public void Debug(object message)
        {
            _logger.Debug(FormatMessage(message));
        }

        public void Notice(object message)
        {
            _logger.Notice(FormatMessage(message));
        }

        public void Info(object message)
        {
            _logger.Info(FormatMessage(message));
        }

        public void Warning(object message)
        {
            _logger.Warn(FormatMessage(message));
        }

        public void Error(object message)
        {
            _logger.Error(FormatMessage(message));
        }

        public void Critical(object message)
        {
            _logger.Critical(FormatMessage(message));
        }

        private string FormatMessage(object message)
        {
            if (string.IsNullOrEmpty(_name))
            {
                return $"[{typeof(T).Name}] {message}";
            }

            return $"[{typeof(T).Name}({_name})] {message}";
        }
    }
}
