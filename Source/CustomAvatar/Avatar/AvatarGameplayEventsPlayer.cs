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
using UnityEngine;
using Zenject;

namespace CustomAvatar.Avatar
{
    internal class AvatarGameplayEventsPlayer : MonoBehaviour
    {
        private ILogger<AvatarGameplayEventsPlayer> _logger;
        private ScoreController _scoreController;
        private ILevelEndActions _levelEndActions;
        private BeatmapObjectCallbackController _beatmapObjectCallbackController;
        private ObstacleSaberSparkleEffectManager _sparkleEffectManager;

        private EventManager _eventManager;

        #region Behaviour Lifecycle
        #pragma warning disable IDE0051

        [Inject]
        public void Inject(ILoggerProvider loggerProvider, LoadedAvatar avatar, ScoreController scoreController, BeatmapObjectCallbackController beatmapObjectCallbackController, ILevelEndActions levelEndActions)
        {
            _logger = loggerProvider.CreateLogger<AvatarGameplayEventsPlayer>(avatar.descriptor.name);
            _scoreController = scoreController;
            _levelEndActions = levelEndActions;
            _beatmapObjectCallbackController = beatmapObjectCallbackController;

            // unfortunately this is not bound through Zenject
            _sparkleEffectManager = FindObjectOfType<ObstacleSaberSparkleEffectManager>();
        }

        private void Start()
        {
            _eventManager = GetComponent<EventManager>();

            if (!_eventManager)
            {
                _logger.Error("No EventManager found!");
                Destroy(this);
                return;
            }

            _eventManager.OnLevelStart?.Invoke();

            _scoreController.noteWasCutEvent += OnNoteWasCut;
            _scoreController.multiplierDidChangeEvent += OnMultiplierDidChange;
            _scoreController.comboDidChangeEvent += OnComboDidChange;
            _scoreController.comboBreakingEventHappenedEvent += OnComboBreakingEventHappened;

            _sparkleEffectManager.sparkleEffectDidStartEvent += OnSparkleEffectDidStart;
            _sparkleEffectManager.sparkleEffectDidEndEvent += OnSparkleEffectDidEnd;

            _levelEndActions.levelFinishedEvent += OnLevelFinished;
            _levelEndActions.levelFailedEvent += OnLevelFailed;

            _beatmapObjectCallbackController.beatmapEventDidTriggerEvent += BeatmapEventDidTrigger;
        }

        private void OnDestroy()
        {
            _scoreController.noteWasCutEvent -= OnNoteWasCut;
            _scoreController.multiplierDidChangeEvent -= OnMultiplierDidChange;
            _scoreController.comboDidChangeEvent -= OnComboDidChange;
            _scoreController.comboBreakingEventHappenedEvent -= OnComboBreakingEventHappened;

            _sparkleEffectManager.sparkleEffectDidStartEvent -= OnSparkleEffectDidStart;
            _sparkleEffectManager.sparkleEffectDidEndEvent -= OnSparkleEffectDidEnd;

            _levelEndActions.levelFinishedEvent -= OnLevelFinished;
            _levelEndActions.levelFailedEvent -= OnLevelFailed;

            _beatmapObjectCallbackController.beatmapEventDidTriggerEvent -= BeatmapEventDidTrigger;
        }
        
        #pragma warning restore IDE0051
        #endregion

        private void OnNoteWasCut(NoteData data, NoteCutInfo cutInfo, int multiplier)
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
            _logger.Trace("Invoke OnComboChanged");
            _eventManager.OnComboChanged?.Invoke(combo);
        }

        private void OnComboBreakingEventHappened()
        {
            _logger.Trace("Invoke OnComboBreak");
            _eventManager.OnComboBreak?.Invoke();
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

        private void BeatmapEventDidTrigger(BeatmapEventData eventData)
        {
            // event 4 triggers lighting changes for the beat lines and general center of scene
            if (eventData == null || eventData.type != BeatmapEventType.Event4) return;

            // event values 1 through 3 are blue and 5 through 7 are red based on information in LightSwitchEventEffect
            if (eventData.value >= 1 && eventData.value <= 3)
            {
                _logger.Trace("Invoke OnBlueLightOn");
                _eventManager.OnBlueLightOn?.Invoke();
            }

            if (eventData.value >= 5 && eventData.value <= 7)
            {
                _logger.Trace("Invoke OnRedLightOn");
                _eventManager.OnRedLightOn?.Invoke();
            }
        }
    }
}
