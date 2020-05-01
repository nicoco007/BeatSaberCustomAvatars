using System;
using UnityEngine;
using UnityEngine.Events;

namespace CustomAvatar
{
    public class EventManager : MonoBehaviour
    {
        [Serializable]
        public class ComboChangedEvent : UnityEvent<int> { }

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
        public ComboChangedEvent OnComboChanged;
    }
}
