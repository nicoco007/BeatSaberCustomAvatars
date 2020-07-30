using CustomAvatar.Configuration;
using CustomAvatar.Logging;
using UnityEngine;
using Zenject;

namespace CustomAvatar
{
    internal class KeyboardInputHandler : ITickable
    {
        private readonly Settings _settings;
        private readonly PlayerAvatarManager _avatarManager;
        private readonly ILogger<KeyboardInputHandler> _logger;

        public KeyboardInputHandler(Settings settings, PlayerAvatarManager avatarManager, ILoggerProvider loggerProvider)
        {
            _settings = settings;
            _avatarManager = avatarManager;
            _logger = loggerProvider.CreateLogger<KeyboardInputHandler>();
        }

        public void Tick()
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
                _settings.resizeMode = (AvatarResizeMode)(((int)_settings.resizeMode + 1) % 3);
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
