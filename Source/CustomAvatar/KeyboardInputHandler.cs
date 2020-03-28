using UnityEngine;

namespace CustomAvatar
{
    internal class KeyboardInputHandler : MonoBehaviour
    {
        private void Update()
        {
            AvatarManager avatarManager = AvatarManager.instance;

            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                avatarManager.SwitchToNextAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.PageUp))
            {
                avatarManager.SwitchToPreviousAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.Home))
            {
                Plugin.settings.isAvatarVisibleInFirstPerson = !Plugin.settings.isAvatarVisibleInFirstPerson;
                Plugin.logger.Info($"{(Plugin.settings.isAvatarVisibleInFirstPerson ? "Enabled" : "Disabled")} first person visibility");
                avatarManager.currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();
            }
            else if (Input.GetKeyDown(KeyCode.End))
            {
                Plugin.settings.resizeMode = (AvatarResizeMode) (((int)Plugin.settings.resizeMode + 1) % 3);
                Plugin.logger.Info($"Set resize mode to {Plugin.settings.resizeMode}");
                avatarManager.ResizeCurrentAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.Insert))
            {
                Plugin.settings.enableFloorAdjust = !Plugin.settings.enableFloorAdjust;
                Plugin.logger.Info($"{(Plugin.settings.enableFloorAdjust ? "Enabled" : "Disabled")} floor adjust");
                avatarManager.ResizeCurrentAvatar();
            }
        }
    }
}
