using UnityEngine.Events;
using UnityEngine.Serialization;

namespace CustomAvatar
{
	public class EveryNthComboFilter : EventFilterBehaviour
	{
		public int ComboStep = 50;
		public UnityEvent NthComboReached;

		private void OnEnable()
		{
			EventManager.OnComboChanged.AddListener(OnComboStep);
		}

		private void OnDisable()
		{
			EventManager.OnComboChanged.RemoveListener(OnComboStep);
		}

		private void OnComboStep(int combo)
		{
			if (combo % ComboStep == 0 && combo != 0)
			{
				NthComboReached.Invoke();
			}
		}
	}

	public class ComboReachedEvent : EventFilterBehaviour
	{
		public int ComboTarget = 50;
		[FormerlySerializedAs("NthComboReached")]
		public UnityEvent ComboReached;

		private void OnEnable()
		{
			EventManager.OnComboChanged.AddListener(OnComboReached);
		}

		private void OnDisable()
		{
			EventManager.OnComboChanged.RemoveListener(OnComboReached);
		}

		private void OnComboReached(int combo)
		{
			if (combo == ComboTarget)
			{
				ComboReached.Invoke();
			}
		}
	}
}