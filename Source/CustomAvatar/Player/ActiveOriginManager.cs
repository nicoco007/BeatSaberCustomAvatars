using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Player
{
    internal class ActiveOriginManager : ActiveObjectManager<Transform>
    {
        internal ActiveOriginManager(ILogger<ActiveOriginManager> logger) : base(logger)
        {
        }
    }
}
