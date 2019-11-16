using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomAvatar
{
    public class AvatarEventsPlayer : MonoBehaviour
    {
        private EventManager _eventManager;

        private ScoreController _scoreController;
        private ObstacleSaberSparkleEffectManager _saberCollisionManager;
        private GameEnergyCounter _gameEnergyCounter;
        private BeatmapObjectCallbackController _beatmapObjectCallbackController;
        private BeatmapDataModel _beatmapDataModel;
        private int _lastNoteId = -1;

        public void Restart()
        {
            CancelInvoke("_Restart");
            Invoke("_Restart", 0.5f);
        }

        private void _Restart()
        {
            CleanUp();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;

            _eventManager = gameObject.GetComponent<EventManager>();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;
        }

        private void OnDestroy()
        {
            CleanUp();
        }

        private void CleanUp()
        {
            if (_scoreController)
            {
                _scoreController.noteWasCutEvent -= SliceCallBack;
                _scoreController.noteWasMissedEvent -= NoteMissCallBack;
                _scoreController.multiplierDidChangeEvent -= MultiplierCallBack;
                _scoreController.comboDidChangeEvent -= ComboChangeEvent;
            }
            
            if (_saberCollisionManager)
            {
                _saberCollisionManager.sparkleEffectDidStartEvent -= SaberStartCollide;
                _saberCollisionManager.sparkleEffectDidEndEvent -= SaberEndCollide;
            }
            
            if (_gameEnergyCounter)
                _gameEnergyCounter.gameEnergyDidReach0Event -= FailLevelCallBack;

            if (_beatmapObjectCallbackController)
                _beatmapObjectCallbackController.beatmapEventDidTriggerEvent -= OnBeatmapEventDidTriggerEvent;

            if (_beatmapDataModel)
                _beatmapDataModel.beatmapDataDidChangeEvent -= BeatmapDataChangedCallback;
        }

        private void SceneManagerOnSceneLoaded(Scene newScene, LoadSceneMode mode)
        {
            _eventManager = gameObject.GetComponent<EventManager>();
            if (_eventManager == null)
                _eventManager = gameObject.AddComponent<EventManager>();

            _scoreController = Resources.FindObjectsOfTypeAll<ScoreController>().FirstOrDefault();
            if (_scoreController == null) return;

            //_eventManager.OnLevelStart?.Invoke(); // replaced by LevelStartedEvent()

            _saberCollisionManager =
                Resources.FindObjectsOfTypeAll<ObstacleSaberSparkleEffectManager>().FirstOrDefault();
            _gameEnergyCounter = Resources.FindObjectsOfTypeAll<GameEnergyCounter>().FirstOrDefault();
            _beatmapObjectCallbackController = Resources.FindObjectsOfTypeAll<BeatmapObjectCallbackController>().FirstOrDefault();
            _beatmapDataModel = Resources.FindObjectsOfTypeAll<BeatmapDataModel>().FirstOrDefault();

            _scoreController.noteWasCutEvent += SliceCallBack;
            _scoreController.noteWasMissedEvent += NoteMissCallBack;
            _scoreController.multiplierDidChangeEvent += MultiplierCallBack;
            _scoreController.comboDidChangeEvent += ComboChangeEvent;

            if (_saberCollisionManager)
            {
                _saberCollisionManager.sparkleEffectDidStartEvent += SaberStartCollide;
                _saberCollisionManager.sparkleEffectDidEndEvent += SaberEndCollide;
            }

            if (_gameEnergyCounter) _gameEnergyCounter.gameEnergyDidReach0Event += FailLevelCallBack;

            if (_beatmapObjectCallbackController)
                _beatmapObjectCallbackController.beatmapEventDidTriggerEvent += OnBeatmapEventDidTriggerEvent;

            _lastNoteId = -1;
            if (_beatmapDataModel)
            {
                _beatmapDataModel.beatmapDataDidChangeEvent += BeatmapDataChangedCallback;
                BeatmapDataChangedCallback();
            }
        }



        private void BeatmapDataChangedCallback()
        {
            if (_beatmapDataModel.beatmapData == null) return;
            _lastNoteId = _beatmapDataModel.beatmapData.beatmapLinesData.Aggregate(new Tuple<float, int>(0, -1), (maxLine, lineData) => {
                return lineData.beatmapObjectsData
                    .Where(obj => obj.beatmapObjectType == BeatmapObjectType.Note && (((NoteData)obj).noteType == NoteType.NoteA || ((NoteData)obj).noteType == NoteType.NoteB))
                    .Aggregate(maxLine, (maxNote, note) => maxNote.Item1 < note.time ? new Tuple<float, int>(note.time, note.id) : maxNote);
            }).Item2;
        }

        private void SliceCallBack(NoteData noteData, NoteCutInfo noteCutInfo, int multiplier)
        {
            if (!noteCutInfo.allIsOK)
            {
                _eventManager?.OnComboBreak?.Invoke();
            }
            else
            {
                _eventManager?.OnSlice?.Invoke();
            }

            if (noteData.id == _lastNoteId)
            {
                _eventManager?.OnLevelFinish?.Invoke();
            }
        }

        private void NoteMissCallBack(NoteData noteData, int multiplier)
        {
            if (noteData.noteType != NoteType.Bomb)
            {
                _eventManager?.OnComboBreak?.Invoke();
            }
        }

        private void MultiplierCallBack(int multiplier, float progress)
        {
            if (multiplier > 1 && progress < 0.1f)
            {
                _eventManager?.MultiplierUp?.Invoke();
            }
        }

        private void SaberStartCollide(Saber.SaberType saber)
        {
            _eventManager?.SaberStartColliding?.Invoke();
        }

        private void SaberEndCollide(Saber.SaberType saber)
        {
            _eventManager?.SaberStopColliding?.Invoke();
        }

        private void FailLevelCallBack()
        {
            _eventManager?.OnLevelFail?.Invoke();
        }

        private void OnBeatmapEventDidTriggerEvent(BeatmapEventData beatmapEventData)
        {
            if (beatmapEventData == null || (int) beatmapEventData.type >= 5) return;
            
            if (beatmapEventData.value > 0 && beatmapEventData.value < 4)
            {
                _eventManager?.OnBlueLightOn?.Invoke();
            }

            if (beatmapEventData.value > 4 && beatmapEventData.value < 8)
            {
                _eventManager?.OnRedLightOn?.Invoke();
            }
        }

        private void ComboChangeEvent(int combo)
        {
            _eventManager?.OnComboChanged?.Invoke(combo);
        }

        public void MenuEnteredEvent()
        {
            _eventManager?.OnMenuEnter?.Invoke();
        }
        public void LevelStartedEvent()
        {
            _eventManager?.OnLevelStart?.Invoke();
        }
    }
}
