using System;
using System.Reflection;

namespace CustomAvatar.Utilities
{
    internal static class ReflectionExtensions
    {
        internal static TResult GetPrivateField<TSubject, TResult>(this TSubject obj, string fieldName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            FieldInfo field = typeof(TSubject).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
            {
                throw new InvalidOperationException($"Field {fieldName} does not exist");
            }

            return (TResult) field.GetValue(obj);
        }

        internal static void SetPrivateField<TSubject>(this TSubject obj, string fieldName, object value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            FieldInfo field = typeof(TSubject).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
            {
                throw new InvalidOperationException($"Field {fieldName} does not exist");
            }

            field.SetValue(obj, value);
        }

        internal static void InvokePrivateMethod<TSubject>(this TSubject obj, string fieldName, params object[] args)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            MethodInfo method = typeof(TSubject).GetMethod(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
            {
                throw new InvalidOperationException($"Method {fieldName} does not exist");
            }

            method.Invoke(obj, args);
        }

        internal static TResult InvokePrivateMethod<TSubject, TResult>(this TSubject obj, string fieldName, params object[] args)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            MethodInfo method = typeof(TSubject).GetMethod(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
            {
                throw new InvalidOperationException($"Method {fieldName} does not exist");
            }

            return (TResult) method.Invoke(obj, args);
        }
    }
}
