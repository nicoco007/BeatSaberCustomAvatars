using System;
using UnityEngine;
using UnityEngine.Events;

namespace CustomAvatar
{
	public class EventManager : MonoBehaviour
	{
		public UnityEvent OnSlice;
		public UnityEvent OnComboBreak;
		public UnityEvent MultiplierUp;
		public UnityEvent SaberStartColliding;
		public UnityEvent SaberStopColliding;
		public UnityEvent OnMenuEnter;
		public UnityEvent OnLevelStart;
		public UnityEvent OnLevelFail;
		public UnityEvent OnLevelFinish;
		public UnityEvent OnBlueLightOn;
		public UnityEvent OnRedLightOn;

		[Serializable]
		public class ComboChangedEvent : UnityEvent<int>
		{
		}

		public ComboChangedEvent OnComboChanged = new ComboChangedEvent();
	}
}
