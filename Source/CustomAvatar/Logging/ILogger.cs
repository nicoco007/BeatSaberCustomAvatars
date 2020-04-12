namespace CustomAvatar.Logging
{
    internal interface ILogger
    {
        void Trace(object message);
        void Debug(object message);
        void Notice(object message);
        void Info(object message);
        void Warning(object message);
        void Error(object message);
        void Critical(object message);
    }
}
