//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Rendering;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Replays
{
    internal class ScoreSaberReplayHandler : IInitializable
    {
        private readonly DiContainer _container;
        private readonly BeatSaberUtilities _beatSaberUtilities;

        protected ScoreSaberReplayHandler(DiContainer container, BeatSaberUtilities beatSaberUtilities)
        {
            _container = container;
            _beatSaberUtilities = beatSaberUtilities;
        }

        public void Initialize()
        {
            GameObject spectatorParent = GameObject.Find("/SpectatorParent");

            if (spectatorParent == null)
            {
                return;
            }

            Camera spectatorCamera = spectatorParent.GetComponentInChildren<Camera>(true);

            if (spectatorCamera == null)
            {
                return;
            }

            Transform origin = new GameObject("Origin").transform;
            Transform playerSpace = spectatorCamera.transform.parent;

            // assuming roomCenter and roomRotation won't change while spectating
            Quaternion inverseRotation = Quaternion.Inverse(_beatSaberUtilities.roomRotation);
            origin.SetLocalPositionAndRotation(inverseRotation * -_beatSaberUtilities.roomCenter, inverseRotation);
            origin.SetParent(playerSpace, false);

            SpectatorCamera spectatorCameraController = _container.InstantiateComponent<SpectatorCamera>(spectatorCamera.gameObject);
            spectatorCameraController.origin = origin;
            spectatorCameraController.playerSpace = playerSpace;

            _container.InstantiateComponent<CameraFlipper>(spectatorCamera.gameObject);
        }
    }
}
