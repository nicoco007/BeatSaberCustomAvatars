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

            FieldInfo field = typeof(TSubject).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static);

            if (field == null)
            {
                throw new InvalidOperationException($"Field '{fieldName}' does not exist on {typeof(TSubject).FullName}");
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
                throw new InvalidOperationException($"Field '{fieldName}' does not exist on {typeof(TSubject).FullName}");
            }

            field.SetValue(obj, value);
        }

        internal static void InvokePrivateMethod<TSubject>(this TSubject obj, string methodName, params object[] args)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            MethodInfo method = typeof(TSubject).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
            {
                throw new InvalidOperationException($"Method '{methodName}' does not exist on {typeof(TSubject).FullName}");
            }

            method.Invoke(obj, args);
        }

        internal static TDelegate CreatePrivateMethodDelegate<TDelegate>(this Type type, string methodName) where TDelegate : Delegate
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
            {
                throw new InvalidOperationException($"Method '{methodName}' does not exist on {type.FullName}");
            }

            return (TDelegate) Delegate.CreateDelegate(typeof(TDelegate), method);
        }
    }
}
