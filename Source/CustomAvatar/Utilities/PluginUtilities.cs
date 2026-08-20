using Hive.Versioning;
using IPA.Loader;

namespace CustomAvatar.Utilities
{
    internal static class PluginUtilities
    {
        internal static bool IsPluginLoadedAndMatchesVersion(string id, VersionRange versionRange)
        {
            PluginMetadata plugin = PluginManager.GetPluginFromId(id);
            return plugin != null && versionRange.Matches(plugin.HVersion);
        }
    }
}
