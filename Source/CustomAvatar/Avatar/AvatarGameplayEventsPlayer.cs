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
        private readonly ComboController _comboController;
        private readonly BeatmapObjectManager _beatmapObjectManager;
        private readonly ILevelEndActions _levelEndActions;
        private readonly IMultiplayerLevelEndActionsPublisher _multiplayerLevelEndActions;
        private readonly ObstacleSaberSparkleEffectManager _sparkleEffectManager;

        private EventManager _eventManager;

        #region Behaviour Lifecycle

        public AvatarGameplayEventsPlayer(ILogger<AvatarGameplayEventsPlayer> logger, PlayerAvatarManager avatarManager, ScoreController scoreController, ComboController comboController, BeatmapObjectManager beatmapObjectManager, [InjectOptional] ILevelEndActions levelEndActions, [InjectOptional] IMultiplayerLevelEndActionsPublisher multiplayerLevelEndActions, ObstacleSaberSparkleEffectManager sparkleEffectManager)
        {
            _logger = logger;
            _avatarManager = avatarManager;
            _scoreController = scoreController;
            _comboController = comboController;
            _beatmapObjectManager = beatmapObjectManager;
            _levelEndActions = levelEndActions;
            _multiplayerLevelEndActions = multiplayerLevelEndActions;
            _sparkleEffectManager = sparkleEffectManager;
        }

        public void Initialize()
        {
            _eventManager = _avatarManager.currentlySpawnedAvatar ? _avatarManager.currentlySpawnedAvatar.GetComponent<EventManager>() : null;

            if (!_eventManager)
            {
                _logger.LogInformation("No EventManager found on current avatar; events will not be triggered");
                return;
            }

            _eventManager.OnLevelStart?.Invoke();

            _beatmapObjectManager.noteWasCutEvent += OnNoteWasCut;

            _scoreController.multiplierDidChangeEvent += OnMultiplierDidChange;

            _comboController.comboDidChangeEvent += OnComboDidChange;

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
        }

        public void Dispose()
        {
            _beatmapObjectManager.noteWasCutEvent -= OnNoteWasCut;

            _scoreController.multiplierDidChangeEvent -= OnMultiplierDidChange;

            _comboController.comboDidChangeEvent -= OnComboDidChange;

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
        }

        #endregion

        private void OnNoteWasCut(NoteController noteController, in NoteCutInfo cutInfo)
        {
            if (cutInfo.allIsOK)
            {
                _logger.LogTrace("Invoke OnSlice");
                _eventManager.OnSlice?.Invoke();
            }
        }

        private void OnMultiplierDidChange(int multiplier, float progress)
        {
            if (multiplier > 1 && progress < 0.1f)
            {
                _logger.LogTrace("Invoke MultiplierUp");
                _eventManager.MultiplierUp?.Invoke();
            }
        }

        private void OnComboDidChange(int combo)
        {
            if (combo > 0)
            {
                _logger.LogTrace("Invoke OnComboChanged");
                _eventManager.OnComboChanged?.Invoke(combo);
            }
            else
            {
                _logger.LogTrace("Invoke OnComboBreak");
                _eventManager.OnComboBreak?.Invoke();
            }
        }

        private void OnSparkleEffectDidStart(SaberType saberType)
        {
            _logger.LogTrace("Invoke SaberStartColliding");
            _eventManager.SaberStartColliding?.Invoke();
        }

        private void OnSparkleEffectDidEnd(SaberType saberType)
        {
            _logger.LogTrace("Invoke SaberStopColliding");
            _eventManager.SaberStopColliding?.Invoke();
        }

        private void OnLevelFinished()
        {
            _logger.LogTrace("Invoke OnLevelFinish");
            _eventManager.OnLevelFinish?.Invoke();
        }

        private void OnLevelFailed()
        {
            _logger.LogTrace("Invoke OnLevelFail");
            _eventManager.OnLevelFail?.Invoke();
        }

        private void OnPlayerDidFinish(MultiplayerLevelCompletionResults results)
        {
            switch (results.playerLevelEndState)
            {
                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndState.SongFinished:
                    _logger.LogTrace("Invoke OnLevelFinish");
                    _eventManager.OnLevelFinish?.Invoke();
                    break;

                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndState.NotFinished:
                    _logger.LogTrace("Invoke OnLevelFail");
                    _eventManager.OnLevelFail?.Invoke();
                    break;
            }
        }
    }
}
