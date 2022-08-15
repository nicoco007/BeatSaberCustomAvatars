using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Player
{
    internal class ActivePlayerSpaceManager : ActiveObjectManager<Transform>
    {
        internal ActivePlayerSpaceManager(ILogger<ActivePlayerSpaceManager> logger) : base(logger)
        {
        }
    }
}
