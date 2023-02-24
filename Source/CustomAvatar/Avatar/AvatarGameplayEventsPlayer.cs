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
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    [DisallowMultipleComponent]
    internal class AvatarGameplayEventsPlayer : IInitializable, IDisposable
    {
        private readonly ILogger<AvatarGameplayEventsPlayer> _logger;
        private readonly PlayerAvatarManager _avatarManager;
        private readonly ScoreController _scoreController;
        private readonly ComboController _comboController;
        private readonly BeatmapObjectEventFilter _beatmapObjectEventFilter;
        private readonly ILevelEndActions _levelEndActions;
        private readonly IMultiplayerLevelEndActionsPublisher _multiplayerLevelEndActions;
        private readonly ObstacleSaberSparkleEffectManager _sparkleEffectManager;
        private readonly GameScenesManager _gameScenesManager;
        private readonly ICoroutineStarter _coroutineStarter;

        private EventManager _eventManager;

        private int _lastCombo = -1;
        private int _lastMultiplier = -1;

        public AvatarGameplayEventsPlayer(
            ILogger<AvatarGameplayEventsPlayer> logger,
            PlayerAvatarManager avatarManager,
            ScoreController scoreController,
            ComboController comboController,
            BeatmapObjectEventFilter beatmapObjectEventFilter,
            [InjectOptional] ILevelEndActions levelEndActions,
            [InjectOptional] IMultiplayerLevelEndActionsPublisher multiplayerLevelEndActions,
            ObstacleSaberSparkleEffectManager sparkleEffectManager,
            GameScenesManager gameScenesManager,
            ICoroutineStarter coroutineStarter)
        {
            _logger = logger;
            _avatarManager = avatarManager;
            _scoreController = scoreController;
            _comboController = comboController;
            _beatmapObjectEventFilter = beatmapObjectEventFilter;
            _levelEndActions = levelEndActions;
            _multiplayerLevelEndActions = multiplayerLevelEndActions;
            _sparkleEffectManager = sparkleEffectManager;
            _gameScenesManager = gameScenesManager;
            _coroutineStarter = coroutineStarter;
        }

        public void Initialize()
        {
            _eventManager = _avatarManager.currentlySpawnedAvatar != null ? _avatarManager.currentlySpawnedAvatar.eventManager : null;

            if (_eventManager == null)
            {
                _logger.LogInformation("No EventManager found on current avatar; events will not be triggered");
                return;
            }

            _beatmapObjectEventFilter.noteGoodCut += OnNoteGoodCut;
            _beatmapObjectEventFilter.noteBadCut += OnNoteBadCut;
            _beatmapObjectEventFilter.noteMissed += OnNoteMissed;

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

            _coroutineStarter.StartCoroutine(TriggerOnLevelStart());
        }

        public void Dispose()
        {
            _beatmapObjectEventFilter.noteGoodCut -= OnNoteGoodCut;
            _beatmapObjectEventFilter.noteBadCut -= OnNoteBadCut;
            _beatmapObjectEventFilter.noteMissed -= OnNoteMissed;

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

        private IEnumerator TriggerOnLevelStart()
        {
            yield return _gameScenesManager.waitUntilSceneTransitionFinish;

            _eventManager.levelStarted.Invoke();
        }

        private void OnNoteGoodCut(NoteController noteController, in NoteCutInfo noteCutInfo)
        {
            if (IsLeftSaber(noteCutInfo.saberType))
            {
                _eventManager.leftGoodCut.Invoke();
            }
            else
            {
                _eventManager.rightGoodCut.Invoke();
            }
        }

        private void OnNoteBadCut(NoteController noteController, in NoteCutInfo noteCutInfo)
        {
            if (IsLeftSaber(noteCutInfo.saberType))
            {
                _eventManager.leftBadCut.Invoke();
            }
            else
            {
                _eventManager.rightBadCut.Invoke();
            }
        }

        private void OnNoteMissed(NoteController noteController)
        {
            if (IsLeftColor(noteController.noteData.colorType))
            {
                _eventManager.leftNoteMissed.Invoke();
            }
            else
            {
                _eventManager.rightNoteMissed.Invoke();
            }
        }

        private void OnMultiplierDidChange(int multiplier, float progress)
        {
            if (multiplier > _lastMultiplier)
            {
                _eventManager.multiplierIncreased.Invoke(multiplier);
            }
            else if (multiplier < _lastMultiplier)
            {
                _eventManager.multiplierDecreased.Invoke(multiplier);
            }

            _lastMultiplier = multiplier;
        }

        private void OnComboDidChange(int combo)
        {
            if (combo > _lastCombo)
            {
                _eventManager.comboIncreased.Invoke(combo);
            }
            else if (combo == 0)
            {
                _eventManager.comboBroken.Invoke();
            }

            _lastCombo = combo;
        }

        private void OnSparkleEffectDidStart(SaberType saberType)
        {
            if (IsLeftSaber(saberType))
            {
                _eventManager.leftSaberStartedColliding.Invoke();
            }
            else
            {
                _eventManager.rightSaberStartedColliding.Invoke();
            }
        }

        private void OnSparkleEffectDidEnd(SaberType saberType)
        {
            if (IsLeftSaber(saberType))
            {
                _eventManager.leftSaberStoppedColliding.Invoke();
            }
            else
            {
                _eventManager.rightSaberStoppedColliding.Invoke();
            }
        }

        private void OnLevelFinished()
        {
            _eventManager.levelFinished.Invoke();
        }

        private void OnLevelFailed()
        {
            _eventManager.levelFailed.Invoke();
        }

        private void OnPlayerDidFinish(MultiplayerLevelCompletionResults results)
        {
            switch (results.playerLevelEndState)
            {
                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndState.SongFinished:
                    OnLevelFinished();
                    break;

                case MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndState.NotFinished:
                    OnLevelFailed();
                    break;
            }
        }

        private bool IsLeftSaber(SaberType saberType) => saberType == SaberType.SaberA;

        private bool IsLeftColor(ColorType colorType) => colorType == ColorType.ColorA;
    }
}
