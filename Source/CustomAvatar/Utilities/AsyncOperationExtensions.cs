using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class AsyncOperationExtensions
    {
        public static AsyncOperationAwaiter<T> GetAwaiter<T>(this T asyncOperation) where T : AsyncOperation => new AsyncOperationAwaiter<T>(asyncOperation);
    }
}
