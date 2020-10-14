//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2020  Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Configuration;
using CustomAvatar.StereoRendering;
using UnityEngine;
using HMUI;
using Zenject;

namespace CustomAvatar.UI
{
    internal class MirrorViewController : ViewController
    {
        private GameObject _mirrorContainer;

        private MirrorHelper _mirrorHelper;
        private Settings _settings;

        [Inject]
        private void Inject(MirrorHelper mirrorHelper, Settings settings)
        {
            _mirrorHelper = mirrorHelper;
            _settings = settings;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            if (addedToHierarchy)
            {
                _mirrorContainer = new GameObject();
                Vector2 mirrorSize = _settings.mirror.size;
                _mirrorHelper.CreateMirror(new Vector3(0, mirrorSize.y / 2, 1.5f), Quaternion.Euler(-90f, 0, 0), mirrorSize, _mirrorContainer.transform);
            }
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);

            Destroy(_mirrorContainer);
        }
    }
}
