namespace CustomAvatar.Logging
{
    internal interface ILoggerFactory
    {
        ILogger CreateLogger<T>(string name = null);
    }
}
