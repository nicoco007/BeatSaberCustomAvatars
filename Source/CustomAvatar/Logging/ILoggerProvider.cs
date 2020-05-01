namespace CustomAvatar.Logging
{
    internal interface ILoggerProvider
    {
        ILogger CreateLogger<T>(string name = null);
    }
}
