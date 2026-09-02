//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

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
