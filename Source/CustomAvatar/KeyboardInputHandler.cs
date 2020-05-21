using CustomAvatar.Utilities;
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
                SettingsManager.settings.isAvatarVisibleInFirstPerson = !SettingsManager.settings.isAvatarVisibleInFirstPerson;
                Plugin.logger.Info($"{(SettingsManager.settings.isAvatarVisibleInFirstPerson ? "Enabled" : "Disabled")} first person visibility");
                avatarManager.currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();
            }
            else if (Input.GetKeyDown(KeyCode.End))
            {
                SettingsManager.settings.resizeMode = (AvatarResizeMode) (((int)SettingsManager.settings.resizeMode + 1) % 3);
                Plugin.logger.Info($"Set resize mode to {SettingsManager.settings.resizeMode}");
                avatarManager.ResizeCurrentAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.Insert))
            {
                SettingsManager.settings.enableFloorAdjust = !SettingsManager.settings.enableFloorAdjust;
                Plugin.logger.Info($"{(SettingsManager.settings.enableFloorAdjust ? "Enabled" : "Disabled")} floor adjust");
                avatarManager.ResizeCurrentAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.RightControl))
            {
                SettingsManager.LoadSettings();
            }

        }
    }
}
