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

using CustomAvatar.Avatar;
using CustomAvatar.Player;

namespace CustomAvatar.UI
{
    internal class AvatarSpecificSettingsHost : ViewControllerHost
    {
        private readonly PlayerAvatarManager _avatarManager;

        private bool _isLoaderActive;

        protected AvatarSpecificSettingsHost(PlayerAvatarManager avatarManager)
        {
            _avatarManager = avatarManager;
        }

        protected bool areCurrentAvatarSettingsLoaded => _avatarManager.currentlySpawnedAvatar != null;

        protected bool isLoaderActive
        {
            get => _isLoaderActive;
            set
            {
                _isLoaderActive = value;
                NotifyPropertyChanged();
            }
        }

        protected bool ignoreExclusions
        {
            get => _avatarManager.ignoreFirstPersonExclusions;
            set
            {
                _avatarManager.ignoreFirstPersonExclusions = value;
                NotifyPropertyChanged();
            }
        }

        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            _avatarManager.avatarLoading += OnAvatarLoading;
            _avatarManager.avatarChanged += OnAvatarChanged;

            OnAvatarChanged(_avatarManager.currentlySpawnedAvatar);
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _avatarManager.avatarLoading -= OnAvatarLoading;
            _avatarManager.avatarChanged -= OnAvatarChanged;
        }

        private void OnAvatarLoading(string fullPath, string name)
        {
            isLoaderActive = true;

            NotifyPropertyChanged(nameof(ignoreExclusions));
            NotifyPropertyChanged(nameof(areCurrentAvatarSettingsLoaded));
        }

        private void OnAvatarChanged(SpawnedAvatar avatar)
        {
            isLoaderActive = false;

            NotifyPropertyChanged(nameof(ignoreExclusions));
            NotifyPropertyChanged(nameof(areCurrentAvatarSettingsLoaded));
        }
    }
}
