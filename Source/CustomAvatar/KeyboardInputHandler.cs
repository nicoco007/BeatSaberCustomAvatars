using CustomAvatar.Logging;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar
{
    internal class KeyboardInputHandler : MonoBehaviour
    {
        private Settings _settings;
        private PlayerAvatarManager _avatarManager;
        private ILogger _logger;

        [Inject]
        private void Inject(Settings settings, PlayerAvatarManager avatarManager, ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _avatarManager = avatarManager;
            _logger = loggerFactory.CreateLogger<KeyboardInputHandler>();
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
                _settings.isAvatarVisibleInFirstPerson = !_settings.isAvatarVisibleInFirstPerson;
                _logger.Info($"{(_settings.isAvatarVisibleInFirstPerson ? "Enabled" : "Disabled")} first person visibility");
            }
            else if (Input.GetKeyDown(KeyCode.End))
            {
                _settings.resizeMode = (AvatarResizeMode) (((int)_settings.resizeMode + 1) % 3);
                _logger.Info($"Set resize mode to {_settings.resizeMode}");
                _avatarManager.ResizeCurrentAvatar();
            }
            else if (Input.GetKeyDown(KeyCode.Insert))
            {
                _settings.enableFloorAdjust = !_settings.enableFloorAdjust;
                _logger.Info($"{(_settings.enableFloorAdjust ? "Enabled" : "Disabled")} floor adjust");
                _avatarManager.ResizeCurrentAvatar();
            }
        }
    }
}
