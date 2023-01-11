//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using ModestTree;
using Zenject;

namespace CustomAvatar.Zenject.Internal
{
    internal class InstallerRegistrationOnContext : InstallerRegistrationOnTarget
    {
        private readonly string _sceneName;
        private readonly string _contextName;

        internal InstallerRegistrationOnContext(string sceneName, string contextName)
        {
            Assert.IsNotEmpty(sceneName);
            Assert.IsNotEmpty(contextName);

            _sceneName = sceneName;
            _contextName = contextName;
        }

        internal override bool ShouldInstall(Context context)
        {
            return context.name == _contextName && context.gameObject.scene.name == _sceneName;
        }
    }
}
