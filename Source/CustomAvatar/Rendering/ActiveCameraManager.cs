using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using UnityEngine;

namespace CustomAvatar.Rendering
{
    internal class ActiveCameraManager : ActiveObjectManager<Camera>
    {
        internal ActiveCameraManager(ILogger<ActiveCameraManager> logger) : base(logger)
        {
        }
    }
}
