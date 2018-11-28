using System.Linq;
using UnityEngine;

namespace CustomAvatar
{
	public class AvatarEventsPlayer : MonoBehaviour
	{
		private EventManager _eventManager;

		private ScoreController _scoreController;
		private ObstacleSaberSparkleEffectManager _saberCollisionManager;
		private GameEnergyCounter _gameEnergyCounter;
		private BeatmapObjectCallbackController _beatmapObjectCallbackController;

		public void Restart()
		{
			CancelInvoke("_Restart");
			Invoke("_Restart", 0.5f);
		}

		private void _Restart()
		{
			OnDestroy();
			Start();
		}

		private void Start()
		{
			_eventManager = gameObject.GetComponent<EventManager>();
			if (_eventManager == null)
				_eventManager = gameObject.AddComponent<EventManager>();

			_scoreController = Resources.FindObjectsOfTypeAll<ScoreController>().FirstOrDefault();
			if (_scoreController == null) return;

			_eventManager.OnLevelStart?.Invoke();

			_saberCollisionManager = Resources.FindObjectsOfTypeAll<ObstacleSaberSparkleEffectManager>().FirstOrDefault();
			_gameEnergyCounter = Resources.FindObjectsOfTypeAll<GameEnergyCounter>().FirstOrDefault();
			_beatmapObjectCallbackController = Resources.FindObjectsOfTypeAll<BeatmapObjectCallbackController>().FirstOrDefault();

			_scoreController.noteWasCutEvent += SliceCallBack;
			_scoreController.noteWasMissedEvent += NoteMissCallBack;
			_scoreController.multiplierDidChangeEvent += MultiplierCallBack;
			_scoreController.comboDidChangeEvent += ComboChangeEvent;

			if (_saberCollisionManager != null)
			{
				_saberCollisionManager.sparkleEffectDidStartEvent += SaberStartCollide;
				_saberCollisionManager.sparkleEffectDidEndEvent += SaberEndCollide;
			}

			if (_gameEnergyCounter != null) _gameEnergyCounter.gameEnergyDidReach0Event += FailLevelCallBack;

			if (_beatmapObjectCallbackController != null)
				_beatmapObjectCallbackController.beatmapEventDidTriggerEvent += OnBeatmapEventDidTriggerEvent;
		}

		private void OnDestroy()
		{
			if (_scoreController == null) return;
			_scoreController.noteWasCutEvent -= SliceCallBack;
			_scoreController.noteWasMissedEvent -= NoteMissCallBack;
			_scoreController.multiplierDidChangeEvent -= MultiplierCallBack;
			_scoreController.comboDidChangeEvent -= ComboChangeEvent;

			_saberCollisionManager.sparkleEffectDidStartEvent -= SaberStartCollide;
			_saberCollisionManager.sparkleEffectDidEndEvent -= SaberEndCollide;

			_gameEnergyCounter.gameEnergyDidReach0Event -= FailLevelCallBack;
			

			_beatmapObjectCallbackController.beatmapEventDidTriggerEvent -= OnBeatmapEventDidTriggerEvent;
		}

		private void SliceCallBack(NoteData noteData, NoteCutInfo noteCutInfo, int multiplier)
		{
			if (!noteCutInfo.allIsOK)
			{
				_eventManager.OnComboBreak?.Invoke();
			}
			else
			{
				_eventManager.OnSlice?.Invoke();
			}
		}

		private void NoteMissCallBack(NoteData noteData, int multiplier)
		{
			if (noteData.noteType != NoteType.Bomb)
			{
				_eventManager.OnComboBreak?.Invoke();
			}
		}

		private void MultiplierCallBack(int multiplier, float progress)
		{
			if (multiplier > 1 && progress < 0.1f)
			{
				_eventManager.MultiplierUp?.Invoke();
			}
		}

		private void SaberStartCollide(Saber.SaberType saber)
		{
			_eventManager.SaberStartColliding?.Invoke();
		}

		private void SaberEndCollide(Saber.SaberType saber)
		{
			_eventManager.SaberStopColliding?.Invoke();
		}

		private void FailLevelCallBack()
		{
			_eventManager.OnLevelFail?.Invoke();
		}

		private void OnBeatmapEventDidTriggerEvent (BeatmapEventData beatmapEventData)
		{
			if ((int) beatmapEventData.type >= 5) return;
			
			if (beatmapEventData.value > 0 && beatmapEventData.value < 4)
			{
				_eventManager.OnBlueLightOn?.Invoke();
			}

			if (beatmapEventData.value > 4 && beatmapEventData.value < 8)
			{
				_eventManager.OnRedLightOn?.Invoke();
			}
		}

		private void ComboChangeEvent(int combo)
		{
			_eventManager.OnComboChanged?.Invoke(combo);
		}
	}
}