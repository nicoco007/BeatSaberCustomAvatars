using System;

namespace CustomAvatar.Exceptions
{
    internal class ShaderLoadException : Exception
    {
        public ShaderLoadException(string message) : base(message)
        {
        }
    }
}
