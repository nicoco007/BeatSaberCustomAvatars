namespace CustomAvatar.Logging
{
    internal interface ILoggerFactory
    {
        ILogger<T> CreateLogger<T>(string name = null);
    }
}
