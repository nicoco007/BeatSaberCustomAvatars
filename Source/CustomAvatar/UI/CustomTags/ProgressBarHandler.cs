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

using System;
using System.Collections.Generic;
using System.Globalization;
using BeatSaberMarkupLanguage.TypeHandlers;

namespace CustomAvatar.UI.CustomTags
{
    [ComponentHandler(typeof(ProgressBar))]
    internal class ProgressBarHandler : TypeHandler<ProgressBar>
    {
        public override Dictionary<string, Action<ProgressBar, string>> Setters { get; } = new()
        {
            { "progress", (bar, text) => bar.progress = float.Parse(text, CultureInfo.InvariantCulture) },
            { "title", (bar, text) => bar.text = text },
        };

        public override Dictionary<string, string[]> Props { get; } = new()
        {
            { "progress", ["progress"] },
            { "title", ["title"] },
        };
    }
}
