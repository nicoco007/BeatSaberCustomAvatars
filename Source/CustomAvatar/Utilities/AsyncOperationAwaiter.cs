//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using System.Runtime.CompilerServices;
using UnityEngine;

#pragma warning disable IDE1006
namespace CustomAvatar.Utilities
{
    internal struct AsyncOperationAwaiter<T> : ICriticalNotifyCompletion where T : AsyncOperation
    {
        private T asyncOperation;
        private Action continuationAction;

        public AsyncOperationAwaiter(T asyncOperation)
        {
            this.asyncOperation = asyncOperation;
            this.continuationAction = null;
        }

        public bool IsCompleted => asyncOperation.isDone;

        public T GetResult()
        {
            if (continuationAction != null)
            {
                asyncOperation.completed -= Continue;
                continuationAction = null;
            }

            T op = asyncOperation;
            asyncOperation = null;
            return op;
        }

        public void OnCompleted(Action continuation)
        {
            UnsafeOnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (continuationAction != null)
            {
                throw new InvalidOperationException("Continuation is already registered");
            }

            continuationAction = continuation;
            asyncOperation.completed += Continue;
        }

        private void Continue(AsyncOperation _)
        {
            continuationAction?.Invoke();
        }
    }
}
