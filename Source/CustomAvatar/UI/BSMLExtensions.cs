using BeatSaberMarkupLanguage.Components.Settings;
using UnityEngine;

namespace CustomAvatar.UI
{
    internal static class BSMLExtensions
    {
        public static void SetInteractable(this IncDecSetting setting, bool enable)
        {
            setting.incButton.interactable = enable;
            setting.decButton.interactable = enable;
            setting.text.color = enable ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.3f);
        }
    }
}
