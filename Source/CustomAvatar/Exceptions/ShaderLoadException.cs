using System;

namespace CustomAvatar.Utilities
{
    internal class ShaderLoadException : Exception
    {
        public ShaderLoadException(string message) : base(message)
        {
        }
    }
}
