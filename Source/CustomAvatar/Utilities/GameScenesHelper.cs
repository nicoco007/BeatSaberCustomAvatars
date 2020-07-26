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
