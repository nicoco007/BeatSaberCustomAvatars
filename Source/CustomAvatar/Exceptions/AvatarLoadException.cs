using System;

namespace CustomAvatar.Exceptions
{
    public class AvatarLoadException : Exception
    {
        internal AvatarLoadException(string message) : base(message) { }
    }
}
