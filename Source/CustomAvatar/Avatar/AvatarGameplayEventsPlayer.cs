using CustomAvatar.Logging;
using UnityEngine;
using Zenject;
using ILogger = CustomAvatar.Logging.ILogger;

namespace CustomAvatar.Avatar
{
    internal class AvatarGameplayEventsPlayer : MonoBehaviour
    {
        private ILogger _logger;
        private ScoreController _scoreController;
        private StandardLevelGameplayManager _gameplayManager;
        private BeatmapObjectCallbackController _beatmapObjectCallbackController;

        private EventManager _eventManager;
        
        #region Behaviour Lifecycle
        #pragma warning disable IDE0051
        // ReSharper disable UnusedMember.Local

        [Inject]
        public void Inject(ILoggerProvider loggerProvider, LoadedAvatar avatar, ScoreController scoreController, StandardLevelGameplayManager gameplayManager, BeatmapObjectCallbackController beatmapObjectCallbackController)
        {
            _logger = loggerProvider.CreateLogger<AvatarGameplayEventsPlayer>(avatar.descriptor.name);
            _scoreController = scoreController;
            _gameplayManager = gameplayManager;
            _beatmapObjectCallbackController = beatmapObjectCallbackController;
        }

        private void Start()
        {
            _eventManager = GetComponent<EventManager>();

            if (!_eventManager)
            {
                _logger.Error("No EventManager found!");
                Destroy(this);
            }

            _eventManager.OnLevelStart?.Invoke();

            _scoreController.noteWasCutEvent += OnNoteWasCut;
            _scoreController.multiplierDidChangeEvent += OnMultiplierDidChange;
            _scoreController.comboDidChangeEvent += OnComboDidChange;
            _scoreController.comboBreakingEventHappenedEvent += OnComboBreakingEventHappened;

            _gameplayManager.levelFinishedEvent += OnLevelFinished;
            _gameplayManager.levelFailedEvent += OnLevelFailed;

            _beatmapObjectCallbackController.beatmapEventDidTriggerEvent += BeatmapEventDidTrigger;
        }

        private void OnDestroy()
        {
            _scoreController.noteWasCutEvent -= OnNoteWasCut;
            _scoreController.multiplierDidChangeEvent -= OnMultiplierDidChange;
            _scoreController.comboDidChangeEvent -= OnComboDidChange;
            _scoreController.comboBreakingEventHappenedEvent -= OnComboBreakingEventHappened;

            _gameplayManager.levelFinishedEvent -= OnLevelFinished;
            _gameplayManager.levelFailedEvent -= OnLevelFailed;

            _beatmapObjectCallbackController.beatmapEventDidTriggerEvent -= BeatmapEventDidTrigger;
        }
        

        // ReSharper restore UnusedMember.Local
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

        private void OnSparkleEventDidStart(SaberType saberType)
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

            // events 1 through 3 are blue (based on information in LightSwitchEventEffect)
            if (eventData.value >= 1 && eventData.value <= 3)
            {
                _logger.Trace("Invoke OnBlueLightOn");
                _eventManager.OnBlueLightOn?.Invoke();
            }

            // events 5 through 7 are red
            if (eventData.value >= 5 && eventData.value <= 7)
            {
                _logger.Trace("Invoke OnRedLightOn");
                _eventManager.OnRedLightOn?.Invoke();
            }
        }
    }
}
