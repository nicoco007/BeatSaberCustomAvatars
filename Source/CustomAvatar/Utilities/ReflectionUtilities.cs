using System.Linq.Expressions;
using System.Reflection;
using System;

namespace CustomAvatar.Utilities
{
    internal static class ReflectionUtilities
    {
        internal static Func<object, T> CreateFieldGetter<T>(this Type type, string fieldName)
        {
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            ParameterExpression parameter = Expression.Parameter(typeof(object));
            return Expression.Lambda<Func<object, T>>(Expression.Field(Expression.ConvertChecked(parameter, type), fieldInfo), parameter).Compile();
        }

        internal static Func<object, T> CreatePropertyGetter<T>(this Type type, string propertyName)
        {
            PropertyInfo propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            ParameterExpression parameter = Expression.Parameter(typeof(object));
            return Expression.Lambda<Func<object, T>>(Expression.Property(Expression.ConvertChecked(parameter, type), propertyInfo), parameter).Compile();
        }
    }
}
