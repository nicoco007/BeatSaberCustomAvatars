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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomAvatar.Logging;
using CustomAvatar.Player;
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
        private readonly AudioTimeSyncController _audioTimeSyncController;
        private readonly IReadonlyBeatmapData _beatmapData;
        private readonly GameScenesManager _gameScenesManager;

        private readonly List<NoteData> _noteDatas = new List<NoteData>();
        private readonly HashSet<BeatmapDataItem> _wasAlreadyCut = new HashSet<BeatmapDataItem>();
        private readonly HashSet<BeatmapDataItem> _wasAlreadyBadCut = new HashSet<BeatmapDataItem>();
        private readonly HashSet<BeatmapDataItem> _wasAlreadyMissed = new HashSet<BeatmapDataItem>();

        private EventManager _eventManager;

        private int _lastCombo = -1;
        private int _lastMultiplier = -1;

        #region Behaviour Lifecycle

        public AvatarGameplayEventsPlayer(
            ILogger<AvatarGameplayEventsPlayer> logger,
            PlayerAvatarManager avatarManager,
            ScoreController scoreController,
            ComboController comboController,
            BeatmapObjectManager beatmapObjectManager,
            [InjectOptional] ILevelEndActions levelEndActions,
            [InjectOptional] IMultiplayerLevelEndActionsPublisher multiplayerLevelEndActions,
            ObstacleSaberSparkleEffectManager sparkleEffectManager,
            AudioTimeSyncController audioTimeSyncController,
            IReadonlyBeatmapData beatmapData,
            GameScenesManager gameScenesManager)
        {
            _logger = logger;
            _avatarManager = avatarManager;
            _scoreController = scoreController;
            _comboController = comboController;
            _beatmapObjectManager = beatmapObjectManager;
            _levelEndActions = levelEndActions;
            _multiplayerLevelEndActions = multiplayerLevelEndActions;
            _sparkleEffectManager = sparkleEffectManager;
            _audioTimeSyncController = audioTimeSyncController;
            _beatmapData = beatmapData;
            _gameScenesManager = gameScenesManager;
        }

        public void Initialize()
        {
            _eventManager = _avatarManager.currentlySpawnedAvatar ? _avatarManager.currentlySpawnedAvatar.GetComponent<EventManager>() : null;

            if (_eventManager == null)
            {
                _logger.LogInformation("No EventManager found on current avatar; events will not be triggered");
                return;
            }

            _beatmapObjectManager.noteWasCutEvent += OnNoteWasCut;
            _beatmapObjectManager.noteWasMissedEvent += OnNoteWasMissed;

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

            PopulateSliderHeadNoteDatas();

            SharedCoroutineStarter.instance.StartCoroutine(TriggerOnLevelStart());
        }

        public void Dispose()
        {
            _beatmapObjectManager.noteWasCutEvent -= OnNoteWasCut;
            _beatmapObjectManager.noteWasMissedEvent -= OnNoteWasMissed;

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

        private void PopulateSliderHeadNoteDatas()
        {
            LinkedListNode<BeatmapDataItem> node = _beatmapData.allBeatmapDataItems.First;

            while (node != null)
            {
                if (node.Value is NoteData noteData && noteData.gameplayType == NoteData.GameplayType.BurstSliderHead)
                {
                    _noteDatas.Add(noteData);
                }

                node = node.Next;
            }
        }

        private IEnumerator TriggerOnLevelStart()
        {
            yield return _gameScenesManager.waitUntilSceneTransitionFinish;

            _eventManager.OnLevelStart?.Invoke();
        }

        private void OnNoteWasCut(NoteController noteController, in NoteCutInfo cutInfo)
        {
            // this is the same logic as MissedNoteEffectSpawner BadNoteCutEffectSpawner
            if (cutInfo.allIsOK && noteController.noteData.colorType != ColorType.None)
            {
                if (ParentWasAlreadyTriggered(noteController, _wasAlreadyCut))
                {
                    return;
                }

                _logger.LogTrace($"Invoke {nameof(_eventManager.OnSlice)}");
                _eventManager.OnSlice?.Invoke();
            }
            else if (noteController.noteData.time + 0.5f >= _audioTimeSyncController.songTime)
            {
                if (ParentWasAlreadyTriggered(noteController, _wasAlreadyBadCut))
                {
                    return;
                }

                _logger.LogTrace($"Invoke {nameof(_eventManager.OnBadCut)}");
                _eventManager.OnBadCut?.Invoke();
            }
        }

        private void OnNoteWasMissed(NoteController noteController)
        {
            // this is the same logic as MissedNoteEffectSpawner
            if (!noteController.hidden && noteController.noteData.time + 0.5f >= _audioTimeSyncController.songTime && noteController.noteData.colorType != ColorType.None)
            {
                if (ParentWasAlreadyTriggered(noteController, _wasAlreadyMissed))
                {
                    return;
                }

                _logger.LogTrace($"Invoke {nameof(_eventManager.OnMiss)}");
                _eventManager.OnMiss?.Invoke();
            }
        }

        private void OnMultiplierDidChange(int multiplier, float progress)
        {
            if (multiplier > _lastMultiplier)
            {
                _logger.LogTrace($"Invoke {nameof(_eventManager.MultiplierUp)}");
                _eventManager.MultiplierUp?.Invoke();
            }
            else if (multiplier < _lastMultiplier)
            {
                _logger.LogTrace($"Invoke {nameof(_eventManager.MultiplierDown)}");
                _eventManager.MultiplierDown?.Invoke();
            }

            _lastMultiplier = multiplier;
        }

        private void OnComboDidChange(int combo)
        {
            if (combo > _lastCombo)
            {
                _logger.LogTrace($"Invoke {nameof(_eventManager.OnComboUp)}");
                _eventManager.OnComboUp?.Invoke();
            }
            else if (combo < _lastCombo)
            {
                _logger.LogTrace($"Invoke {nameof(_eventManager.OnComboBreak)}");
                _eventManager.OnComboBreak?.Invoke();
            }

            _lastCombo = combo;
        }

        private void OnSparkleEffectDidStart(SaberType saberType)
        {
            _logger.LogTrace($"Invoke {nameof(_eventManager.SaberStartColliding)}");
            _eventManager.SaberStartColliding?.Invoke();
        }

        private void OnSparkleEffectDidEnd(SaberType saberType)
        {
            _logger.LogTrace($"Invoke {nameof(_eventManager.SaberStopColliding)}");
            _eventManager.SaberStopColliding?.Invoke();
        }

        private void OnLevelFinished()
        {
            _logger.LogTrace($"Invoke {nameof(_eventManager.OnLevelFinish)}");
            _eventManager.OnLevelFinish?.Invoke();
        }

        private void OnLevelFailed()
        {
            _logger.LogTrace($"Invoke {nameof(_eventManager.OnLevelFail)}");
            _eventManager.OnLevelFail?.Invoke();
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

        private bool ParentWasAlreadyTriggered(NoteController noteController, HashSet<BeatmapDataItem> alreadyTriggeredItems)
        {
            if (noteController.noteData.gameplayType != NoteData.GameplayType.BurstSliderHead && noteController.noteData.gameplayType != NoteData.GameplayType.BurstSliderElement && noteController.noteData.gameplayType != NoteData.GameplayType.BurstSliderElementFill)
            {
                return false;
            }

            return !alreadyTriggeredItems.Add(GetHeadNote(noteController));
        }

        private NoteData GetHeadNote(NoteController noteController)
        {
            NoteData noteData = noteController.noteData;
            // allBeatmapDataItems is backed by a sorted list so we can assume time is increasing as we iterate through the list
            return _noteDatas.First(nd => noteData.time >= nd.time && nd.colorType == noteData.colorType);
        }
    }
}
