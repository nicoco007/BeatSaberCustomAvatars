namespace CustomAvatar.Logging
{
    internal class UnityDebugLogger<T> : ILogger<T>
    {
        private readonly string _name;

        public UnityDebugLogger(string name = null)
        {
            _name = name;
        }

        public void Trace(object message) { }

        public void Debug(object message)
        {
            UnityEngine.Debug.Log(FormatMessage("DEBUG", message));
        }

        public void Notice(object message)
        {
            UnityEngine.Debug.Log(FormatMessage("NOTICE", message));
        }

        public void Info(object message)
        {
            UnityEngine.Debug.Log(FormatMessage("INFO", message));
        }

        public void Warning(object message)
        {
            UnityEngine.Debug.LogWarning(FormatMessage("WARNING", message));
        }

        public void Error(object message)
        {
            UnityEngine.Debug.LogError(FormatMessage("ERROR", message));
        }

        public void Critical(object message)
        {
            UnityEngine.Debug.LogError(FormatMessage("CRITICAL", message));
        }

        private string FormatMessage(string level, object message)
        {
            if (string.IsNullOrEmpty(_name))
            {
                return $"{level} | [{typeof(T).Name}] {message}";
            }

            return $"{level} | [{typeof(T).Name}({_name})] {message}";
        }
    }
}
