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

using CustomAvatar.Logging;
using CustomAvatar.Player;
using System;
using Zenject;

namespace CustomAvatar.Avatar
{
    internal class AvatarGameplayEventsPlayer : IInitializable, IDisposable
    {
        private readonly ILogger<AvatarGameplayEventsPlayer> _logger;
        private readonly PlayerAvatarManager _avatarManager;
        private readonly ScoreController _scoreController;
        private readonly ILevelEndActions _levelEndActions;
        private readonly IMultiplayerLevelEndActionsPublisher _multiplayerLevelEndActions;
        private readonly BeatmapObjectCallbackController _beatmapObjectCallbackController;
        private readonly ObstacleSaberSparkleEffectManager _sparkleEffectManager;

        private EventManager _eventManager;
        private ColorType _lastColor = ColorType.None;

        #region Behaviour Lifecycle

        public AvatarGameplayEventsPlayer(ILogger<AvatarGameplayEventsPlayer> logger, PlayerAvatarManager avatarManager, ScoreController scoreController, [InjectOptional] ILevelEndActions levelEndActions, [InjectOptional] IMultiplayerLevelEndActionsPublisher multiplayerLevelEndActions, BeatmapObjectCallbackController beatmapObjectCallbackController, ObstacleSaberSparkleEffectManager sparkleEffectManager)
        {
            _logger = logger;
            _avatarManager = avatarManager;
            _scoreController = scoreController;
            _levelEndActions = levelEndActions;
            _multiplayerLevelEndActions = multiplayerLevelEndActions;
            _beatmapObjectCallbackController = beatmapObjectCallbackController;
            _sparkleEffectManager = sparkleEffectManager;
        }

        public void Initialize()
        {
            _eventManager = _avatarManager.currentlySpawnedAvatar ? _avatarManager.currentlySpawnedAvatar.GetComponent<EventManager>() : null;

            if (!_eventManager)
            {
                _logger.Info("No EventManager found on current avatar; events will not be triggered");
                return;
            }

            _eventManager.OnLevelStart?.Invoke();

            _scoreController.noteWasCutEvent += OnNoteWasCut;
            _scoreController.multiplierDidChangeEvent += OnMultiplierDidChange;
            _scoreController.comboDidChangeEvent += OnComboDidChange;

            _sparkleEffectManager.sparkleEffectDidStartEvent += OnSparkleEffectDidStart;
            _sparkleEffectManager.sparkleEffectDidEndEvent += OnSparkleEffectDidEnd;

            if (_levelEndActions != null)
            {
                _levelEndActions.levelFinishedEvent += OnLevelFinished;
                _levelEndActions.levelFailedEvent += OnLevelFailed;
            }

            if (_multiplayerLevelEndActions != null)
            {
                _multiplayerLevelEndActions.playerDidFinishEvent += OnPlayerDidFinish;
            }

            _beatmapObjectCallbackController.beatmapEventDidTriggerEvent += BeatmapEventDidTrigger;
        }

        public void Dispose()
        {
            _scoreController.noteWasCutEvent -= OnNoteWasCut;
            _scoreController.multiplierDidChangeEvent -= OnMultiplierDidChange;
            _scoreController.comboDidChangeEvent -= OnComboDidChange;

            _sparkleEffectManager.sparkleEffectDidStartEvent -= OnSparkleEffectDidStart;
            _sparkleEffectManager.sparkleEffectDidEndEvent -= OnSparkleEffectDidEnd;

            if (_levelEndActions != null)
            {
                _levelEndActions.levelFinishedEvent -= OnLevelFinished;
                _levelEndActions.levelFailedEvent -= OnLevelFailed;
            }

            if (_multiplayerLevelEndActions != null)
            {
                _multiplayerLevelEndActions.playerDidFinishEvent -= OnPlayerDidFinish;
            }

            _beatmapObjectCallbackController.beatmapEventDidTriggerEvent -= BeatmapEventDidTrigger;
        }

        #endregion

        private void OnNoteWasCut(NoteData noteData, in NoteCutInfo cutInfo, int multiplier)
        {
            if (cutInfo.allIsOK)
            {
                _logger.Trace("Invoke OnSlice");
                _eventManager.OnSlice?.Invoke();
            }
        }

        private void OnMultiplierDidChange(int multiplier, float progress)
        {
            if (multiplier > 1 && progress < 0.1f)
            {
                _logger.Trace("Invoke MultiplierUp");
                _eventManager.MultiplierUp?.Invoke();
            }
        }

        private void OnComboDidChange(int combo)
        {
            if (combo > 0)
            {
                _logger.Trace("Invoke OnComboChanged");
                _eventManager.OnComboChanged?.Invoke(combo);
            }
            else
            {
                _logger.Trace("Invoke OnComboBreak");
                _eventManager.OnComboBreak?.Invoke();
            }
        }

        private void OnSparkleEffectDidStart(SaberType saberType)
        {
            _logger.Trace("Invoke SaberStartColliding");
            _eventManager.SaberStartColliding?.Invoke();
        }

        private void OnSparkleEffectDidEnd(SaberType saberType)
        {
            _logger.Trace("Invoke SaberStopColliding");
            _eventManager.SaberStopColliding?.Invoke();
        }

        private void OnLevelFinished()
        {
            _logger.Trace("Invoke OnLevelFinish");
            _eventManager.OnLevelFinish?.Invoke();
        }

        private void OnLevelFailed()
        {
            _logger.Trace("Invoke OnLevelFail");
            _eventManager.OnLevelFail?.Invoke();
        }

        private void OnPlayerDidFinish(MultiplayerLevelCompletionResults results)
        {
            switch (results.levelEndState)
            {
                case MultiplayerLevelCompletionResults.MultiplayerLevelEndState.Cleared:
                    _logger.Trace("Invoke OnLevelFinish");
                    _eventManager.OnLevelFinish?.Invoke();
                    break;

                case MultiplayerLevelCompletionResults.MultiplayerLevelEndState.Failed:
                    _logger.Trace("Invoke OnLevelFail");
                    _eventManager.OnLevelFail?.Invoke();
                    break;
            }
        }

        private void BeatmapEventDidTrigger(BeatmapEventData eventData)
        {
            // lighting events seem to be 0 through 4
            if (eventData == null || eventData.type < BeatmapEventType.Event0 || eventData.type > BeatmapEventType.Event4) return;

            // event values 1 through 3 are "color b" (default blue) and 5 through 7 are "color a" (default red) based on information in LightSwitchEventEffect
            if (eventData.value >= 1 && eventData.value <= 3 && _lastColor != ColorType.ColorB)
            {
                _logger.Trace("Invoke OnBlueLightOn");
                _eventManager.OnBlueLightOn?.Invoke();

                _lastColor = ColorType.ColorB;
            }

            if (eventData.value >= 5 && eventData.value <= 7 && _lastColor != ColorType.ColorA)
            {
                _logger.Trace("Invoke OnRedLightOn");
                _eventManager.OnRedLightOn?.Invoke();

                _lastColor = ColorType.ColorA;
            }
        }
    }
}
