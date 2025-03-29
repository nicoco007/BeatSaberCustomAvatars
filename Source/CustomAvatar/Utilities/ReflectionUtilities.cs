//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
