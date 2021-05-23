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
