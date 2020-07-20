namespace CustomAvatar.Logging
{
    internal interface ILoggerProvider
    {
        ILogger<T> CreateLogger<T>(string name = null);
    }
}
