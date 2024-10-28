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

using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

#if DEBUG
using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using JetBrains.Annotations;
using Zenject;
#endif

namespace CustomAvatar
{
    public partial class EventManager
    {
        private static readonly FieldInfo kPersistentCallsField = typeof(UnityEventBase).GetField("m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);

        #region Legacy
#pragma warning disable IDE1006

        // these are only present because FormerlySerializedAs does not work in the game

        [SerializeField]
        private UnityEvent OnSlice;

        [SerializeField]
        private UnityEvent OnComboBreak;

        [SerializeField]
        private UnityEvent MultiplierUp;

        [SerializeField]
        private UnityEvent SaberStartColliding;

        [SerializeField]
        private UnityEvent SaberStopColliding;

        [SerializeField]
        private UnityEvent OnMenuEnter;

        [SerializeField]
        private UnityEvent OnLevelStart;

        [SerializeField]
        private UnityEvent OnLevelFail;

        [SerializeField]
        private UnityEvent OnLevelFinish;

        [SerializeField]
        private UnityEvent OnComboChanged;

#pragma warning restore IDE1006
        #endregion

        [SerializeField]
        private UnityEvent _comboIncreased;

        [SerializeField]
        private UnityEvent _multiplierIncreased;

        [SerializeField]
        private UnityEvent _multiplierDecreased;

        public IntUnityEvent comboIncreased { get; private set; }

        public IntUnityEvent multiplierIncreased { get; private set; }

        public IntUnityEvent multiplierDecreased { get; private set; }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            // find first event with serialized persistent calls since FormerlySerializedAs doesn't work
            // can't do ?? since events are never deserialized as null
            _leftGoodCut = FirstWithPersistentCalls(_leftGoodCut, OnSlice);
            _rightGoodCut = FirstWithPersistentCalls(_rightGoodCut, OnSlice);
            _comboBroken = FirstWithPersistentCalls(_comboBroken, OnComboBreak);
            _multiplierIncreased = FirstWithPersistentCalls(_multiplierIncreased, MultiplierUp);
            _leftSaberStartedColliding = FirstWithPersistentCalls(_leftSaberStartedColliding, SaberStartColliding);
            _rightSaberStartedColliding = FirstWithPersistentCalls(_rightSaberStartedColliding, SaberStartColliding);
            _leftSaberStoppedColliding = FirstWithPersistentCalls(_leftSaberStoppedColliding, SaberStopColliding);
            _rightSaberStoppedColliding = FirstWithPersistentCalls(_rightSaberStoppedColliding, SaberStopColliding);
            _menuEntered = FirstWithPersistentCalls(_menuEntered, OnMenuEnter);
            _levelStarted = FirstWithPersistentCalls(_levelStarted, OnLevelStart);
            _levelFailed = FirstWithPersistentCalls(_levelFailed, OnLevelFail);
            _levelFinished = FirstWithPersistentCalls(_levelFinished, OnLevelFinish);
            _comboIncreased = FirstWithPersistentCalls(_comboIncreased, OnComboChanged);

            comboIncreased = DeserializeGenericEvent<IntUnityEvent>(_comboIncreased);
            multiplierIncreased = DeserializeGenericEvent<IntUnityEvent>(_multiplierIncreased);
            multiplierDecreased = DeserializeGenericEvent<IntUnityEvent>(_multiplierDecreased);
        }

        // The game won't deserialize generic events since they require a custom serializable class and Unity can't see those if they're outside the game's main assemblies.
        // However, no matter the number of type parameters all UnityEvents inherit from UnityEventBase which contains all the serialized fields.
        // Unity will deserialize any event into a UnityEvent so we can simply extract the calls from the UnityEvent and assign them to the proper class with type parameters.
        private static T DeserializeGenericEvent<T>(UnityEvent evt) where T : UnityEventBase, new()
        {
            var newEvent = new T();
            kPersistentCallsField.SetValue(newEvent, kPersistentCallsField.GetValue(evt));
            ((ISerializationCallbackReceiver)newEvent).OnAfterDeserialize();
            return newEvent;
        }

        private static UnityEvent FirstWithPersistentCalls(params UnityEvent[] events)
        {
            foreach (UnityEvent evt in events)
            {
                if (evt.GetPersistentEventCount() > 0)
                {
                    return evt;
                }
            }

            return events[0];
        }

#if DEBUG
        [Inject]
        [UsedImplicitly]
        private void Construct(ILoggerFactory loggerProvider, SpawnedAvatar avatar)
        {
            ILogger<EventManager> logger = loggerProvider.CreateLogger<EventManager>(avatar.prefab.descriptor.name);
            leftGoodCut.AddListener(() => logger.LogTrace($"{nameof(leftGoodCut)} invoked"));
            rightGoodCut.AddListener(() => logger.LogTrace($"{nameof(rightGoodCut)} invoked"));
            leftBadCut.AddListener(() => logger.LogTrace($"{nameof(leftBadCut)} invoked"));
            rightBadCut.AddListener(() => logger.LogTrace($"{nameof(rightBadCut)} invoked"));
            leftNoteMissed.AddListener(() => logger.LogTrace($"{nameof(leftNoteMissed)} invoked"));
            rightNoteMissed.AddListener(() => logger.LogTrace($"{nameof(rightNoteMissed)} invoked"));
            leftSaberStartedColliding.AddListener(() => logger.LogTrace($"{nameof(leftSaberStartedColliding)} invoked"));
            rightSaberStartedColliding.AddListener(() => logger.LogTrace($"{nameof(rightSaberStartedColliding)} invoked"));
            leftSaberStoppedColliding.AddListener(() => logger.LogTrace($"{nameof(leftSaberStoppedColliding)} invoked"));
            rightSaberStoppedColliding.AddListener(() => logger.LogTrace($"{nameof(rightSaberStoppedColliding)} invoked"));
            comboIncreased.AddListener((_) => logger.LogTrace($"{nameof(comboIncreased)} invoked"));
            comboBroken.AddListener(() => logger.LogTrace($"{nameof(comboBroken)} invoked"));
            multiplierIncreased.AddListener((_) => logger.LogTrace($"{nameof(multiplierIncreased)} invoked"));
            multiplierDecreased.AddListener((_) => logger.LogTrace($"{nameof(multiplierDecreased)} invoked"));
            levelStarted.AddListener(() => logger.LogTrace($"{nameof(levelStarted)} invoked"));
            levelFinished.AddListener(() => logger.LogTrace($"{nameof(levelFinished)} invoked"));
            levelFailed.AddListener(() => logger.LogTrace($"{nameof(levelFailed)} invoked"));
            menuEntered.AddListener(() => logger.LogTrace($"{nameof(menuEntered)} invoked"));
            multiplayerLobbyEntered.AddListener(() => logger.LogTrace($"{nameof(multiplayerLobbyEntered)} invoked"));
        }
#endif // DEBUG
    }
}
