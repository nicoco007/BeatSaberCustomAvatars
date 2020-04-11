using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar
{
    internal class KeyboardInputHandler : MonoBehaviour
    {
        private readonly AvatarManager _avatarManager;

        private KeyboardInputHandler(AvatarManager avatarManager)
        {
            _avatarManager = avatarManager;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                _avatarManager.SwitchToNextAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.PageUp))
            {
                _avatarManager.SwitchToPreviousAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.Home))
            {
                SettingsManager.settings.isAvatarVisibleInFirstPerson = !SettingsManager.settings.isAvatarVisibleInFirstPerson;
                Plugin.logger.Info($"{(SettingsManager.settings.isAvatarVisibleInFirstPerson ? "Enabled" : "Disabled")} first person visibility");
                _avatarManager.currentlySpawnedAvatar?.OnFirstPersonEnabledChanged();
            }
            else if (Input.GetKeyDown(KeyCode.End))
            {
                SettingsManager.settings.resizeMode = (AvatarResizeMode) (((int)SettingsManager.settings.resizeMode + 1) % 3);
                Plugin.logger.Info($"Set resize mode to {SettingsManager.settings.resizeMode}");
                _avatarManager.ResizeCurrentAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.Insert))
            {
                SettingsManager.settings.enableFloorAdjust = !SettingsManager.settings.enableFloorAdjust;
                Plugin.logger.Info($"{(SettingsManager.settings.enableFloorAdjust ? "Enabled" : "Disabled")} floor adjust");
                _avatarManager.ResizeCurrentAvatar();
            }
        }
    }
}
