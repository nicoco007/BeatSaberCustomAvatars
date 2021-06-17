//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using CustomAvatar.Configuration;

namespace CustomAvatar.UI
{
    internal class InterfaceSettingsHost
    {
        private readonly Settings _settings;

        internal InterfaceSettingsHost(Settings settings)
        {
            _settings = settings;
        }


        [UIValue("mirror-render-scale-value")]
        internal float renderScale
        {
            get => _settings.mirror.renderScale;
            set => _settings.mirror.renderScale.value = value;
        }


        [UIValue("mirror-anti-aliasing-level-value")]
        internal int antiAliasingLevel
        {
            get => _settings.mirror.antiAliasingLevel;
            set => _settings.mirror.antiAliasingLevel.value = value;
        }

        [UIValue("mirror-anti-aliasing-level-options")]
        internal List<object> antiAliasingLevelOptions = new List<object>(new object[] { 1, 2, 4, 8 });

        [UIAction("mirror-anti-aliasing-level-formatter")]
        internal string MirrorAntiAliasingLevelFormatter(int value)
        {
            return $"{value}x";
        }
    }
}
