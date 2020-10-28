namespace CustomAvatar.Logging
{
    internal class EditorLoggerProvider : ILoggerProvider
    {
        public ILogger<T> CreateLogger<T>(string name = null)
        {
            return new UnityDebugLogger<T>(name);
        }
    }
}
