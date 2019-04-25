using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
namespace CustomAvatar
{

	public static class ReflectionUtil
	{
		public static void SetPrivateField(this object obj, string fieldName, object value)
		{
			obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(obj, value);
		}

		public static T GetPrivateField<T>(this object obj, string fieldName)
		{
			return (T)((object)obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj));
		}

		public static void SetPrivateProperty(this object obj, string propertyName, object value)
		{
			obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(obj, value, null);
		}

		public static void InvokePrivateMethod(this object obj, string methodName, object[] methodParams)
		{
			obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, methodParams);
		}
		public static Component CopyComponent(Component original, Type originalType, Type overridingType, GameObject destination)
		{
			var copy = destination.AddComponent(overridingType);

			Type type = originalType;
			while (type != typeof(MonoBehaviour))
			{
				CopyForType(type, original, copy);
				type = type.BaseType;
			}

			return copy;
		}

		private static void CopyForType(Type type, Component source, Component destination)
		{
			FieldInfo[] myObjectFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField);

			foreach (FieldInfo fi in myObjectFields)
			{
				fi.SetValue(destination, fi.GetValue(source));
			}
		}
	}
}
