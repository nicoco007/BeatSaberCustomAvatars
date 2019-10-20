using System;

namespace CustomAvatar.Exceptions
{
	internal class AvatarLoadException : Exception
	{
		public AvatarLoadException(string message) : base(message) { }
	}
}
