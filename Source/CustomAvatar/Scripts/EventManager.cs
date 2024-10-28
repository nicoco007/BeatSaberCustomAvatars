//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
//  You should have received a copy of the GNU Lesser General Public
//  License along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using UnityEngine;
using UnityEngine.Events;

// keeping root namespace for compatibility
namespace CustomAvatar
{
    public partial class EventManager : MonoBehaviour
#if !UNITY_EDITOR
        , ISerializationCallbackReceiver
#endif
    {
        [SerializeField]
        private UnityEvent _leftGoodCut;

        [SerializeField]
        private UnityEvent _rightGoodCut;

        [SerializeField]
        private UnityEvent _leftBadCut;

        [SerializeField]
        private UnityEvent _rightBadCut;

        [SerializeField]
        private UnityEvent _leftNoteMissed;

        [SerializeField]
        private UnityEvent _rightNoteMissed;

        [SerializeField]
        private UnityEvent _leftSaberStartedColliding;

        [SerializeField]
        private UnityEvent _rightSaberStartedColliding;

        [SerializeField]
        private UnityEvent _leftSaberStoppedColliding;

        [SerializeField]
        private UnityEvent _rightSaberStoppedColliding;

        [SerializeField]
        private UnityEvent _comboBroken;

        [SerializeField]
        private UnityEvent _levelStarted;

        [SerializeField]
        private UnityEvent _levelFinished;

        [SerializeField]
        private UnityEvent _levelFailed;

        [SerializeField]
        private UnityEvent _menuEntered;

        [SerializeField]
        private UnityEvent _multiplayerLobbyEntered;

        public UnityEvent leftGoodCut => _leftGoodCut;

        public UnityEvent rightGoodCut => _rightGoodCut;

        public UnityEvent leftBadCut => _leftBadCut;

        public UnityEvent rightBadCut => _rightBadCut;

        public UnityEvent leftNoteMissed => _leftNoteMissed;

        public UnityEvent rightNoteMissed => _rightNoteMissed;

        public UnityEvent leftSaberStartedColliding => _leftSaberStartedColliding;

        public UnityEvent rightSaberStartedColliding => _rightSaberStartedColliding;

        public UnityEvent leftSaberStoppedColliding => _leftSaberStoppedColliding;

        public UnityEvent rightSaberStoppedColliding => _rightSaberStoppedColliding;

        public UnityEvent comboBroken => _comboBroken;

        public UnityEvent levelStarted => _levelStarted;

        public UnityEvent levelFinished => _levelFinished;

        public UnityEvent levelFailed => _levelFailed;

        public UnityEvent menuEntered => _menuEntered;

        public UnityEvent multiplayerLobbyEntered => _multiplayerLobbyEntered;

        [Serializable]
        public class IntUnityEvent : UnityEvent<int> { }
    }
}
