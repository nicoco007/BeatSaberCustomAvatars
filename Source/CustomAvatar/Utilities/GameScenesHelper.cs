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

using System;
using Zenject;

namespace CustomAvatar.Utilities
{
    internal class GameScenesHelper
    {
        public event Action<BeatSaberScene, DiContainer> transitionDidFinish;

        private GameScenesManager _gameScenesManager;

        public GameScenesHelper(GameScenesManager gameScenesManager)
        {
            _gameScenesManager = gameScenesManager;

            _gameScenesManager.transitionDidFinishEvent += OnTransitionDidFinish;
        }

        public BeatSaberScene GetCurrentScene()
        {
            if (_gameScenesManager.IsSceneInStack("GameCore"))      return BeatSaberScene.Game;
            if (_gameScenesManager.IsSceneInStack("BeatmapEditor")) return BeatSaberScene.BeatmapEditor;
            if (_gameScenesManager.IsSceneInStack("MainMenu"))      return BeatSaberScene.MainMenu;
            if (_gameScenesManager.IsSceneInStack("HealthWarning")) return BeatSaberScene.HealthWarning;

            return BeatSaberScene.Unknown;
        }

        private void OnTransitionDidFinish(ScenesTransitionSetupDataSO setupData, DiContainer container)
        {
            transitionDidFinish?.Invoke(GetCurrentScene(), container);
        }
    }
}
