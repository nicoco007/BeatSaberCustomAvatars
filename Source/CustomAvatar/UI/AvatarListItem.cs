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

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CustomAvatar.Avatar;
using UnityEngine;

namespace CustomAvatar.UI
{
    internal class AvatarListItem : INotifyPropertyChanged
    {
        private bool _isLoaded;

        public AvatarListItem(AvatarInfo avatar, bool isLoaded, Sprite fallbackIcon)
        {
            _isLoaded = isLoaded;
            this.name = avatar.name;
            this.author = avatar.author;
            this.icon = avatar.icon ? avatar.icon : fallbackIcon;
            this.fileName = avatar.fileName;
            this.loadProgress = 0;
            this.loadException = null;
        }

        public AvatarListItem(string name, Sprite icon, string fileName, bool isLoaded)
        {
            _isLoaded = isLoaded;
            this.name = name;
            this.author = null;
            this.icon = icon;
            this.fileName = fileName;
            this.loadProgress = 0;
            this.loadException = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool isLoaded
        {
            get => _isLoaded;
            set
            {
                _isLoaded = value;
                NotifyPropertyChanged();
            }
        }

        public string name { get; private set; }

        public string author { get; private set; }

        public Sprite icon { get; private set; }

        public string fileName { get; private set; }

        public float loadProgress { get; private set; }

        public Exception loadException { get; private set; }

        public void UpdateProgress(float progress)
        {
            loadProgress = progress;

            NotifyPropertyChanged(nameof(loadProgress));
        }

        public void SetLoadedInfo(AvatarInfo avatarInfo, Sprite fallbackIcon)
        {
            name = avatarInfo.name;
            author = avatarInfo.author;
            icon = avatarInfo.icon ? avatarInfo.icon : fallbackIcon;
            fileName = avatarInfo.fileName;
            isLoaded = true;

            NotifyPropertyChanged(string.Empty);
        }

        public void SetException(Exception exception, Sprite icon)
        {
            this.loadException = exception;
            this.icon = icon;

            NotifyPropertyChanged(string.Empty);
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
