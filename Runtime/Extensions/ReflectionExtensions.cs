using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

#nullable enable
// ReSharper disable once CheckNamespace

namespace GameLovers.GameData
{
	/// <summary>
	/// Container for extensions to use when using reflection in a code base
	/// </summary>
	/// <remarks>
	/// <author>Bruno Coimbra, https://github.com/coimbrastudios/framework/blob/master/Coimbra/Utilities/ReflectionUtility.cs</author>
	public static class ReflectionExtensions
	{
		private const string SignatureFormat = "{0}({1})";

		private const BindingFlags ConstructorBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private const BindingFlags DefaultBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

		private const BindingFlags PrivateBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		private static readonly Dictionary<Type, Dictionary<int, FieldInfo?>> _fieldsByNameFromType = new();

		private static readonly Dictionary<Type, Dictionary<int, MethodInfo?>> _methodsByNameFromType = new();

		private static readonly Dictionary<Type, Dictionary<int, MethodInfo?>> _methodsBySignatureFromType = new();

		private static readonly Dictionary<Type, Dictionary<int, MethodInfo?>> _setterByNameFromType = new();

		/// <summary>
		/// Create an instance of the given type by using either the <see cref="Activator"/> class or by any parameterless constructor on it.
		/// </summary>
		/// <seealso cref="TryCreateInstance{T}"/>
		public static object CreateInstance(this Type type)
		{
			try
			{
				return Activator.CreateInstance(type);
			}
			catch
			{
				return type.GetConstructor(ConstructorBindingFlags, null, Type.EmptyTypes, null)!.Invoke(null);
			}
		}

		/// <summary>
		/// Search for a field by its name. It will also search on base types for private fields if none is found in the target type.
		/// </summary>
		public static FieldInfo? FindFieldByName(this Type type, in string name)
		{
			int hash = name.GetHashCode();

			if (!_fieldsByNameFromType.TryGetValue(type, out Dictionary<int, FieldInfo?> fields))
			{
				fields = new Dictionary<int, FieldInfo?>();
				_fieldsByNameFromType.Add(type, fields);
			}
			else if (fields.TryGetValue(hash, out FieldInfo? result))
			{
				return result;
			}

			FieldInfo? fieldInfo = type.GetField(name, DefaultBindingFlags);

			if (fieldInfo != null)
			{
				fields.Add(hash, fieldInfo);

				return fieldInfo;
			}

			while (type.BaseType != null)
			{
				type = type.BaseType;
				fieldInfo = type.GetField(name, PrivateBindingFlags);

				if (fieldInfo != null)
				{
					break;
				}
			}

			fields.Add(hash, fieldInfo);

			return fieldInfo;
		}

		/// <summary>
		/// Search for a method by its name. It will also search on base types for private methods if none is found in the target type.
		/// </summary>
		public static MethodInfo? FindMethodByName(this Type type, in string name)
		{
			int hash = name.GetHashCode();

			if (!_methodsByNameFromType.TryGetValue(type, out Dictionary<int, MethodInfo?> methods))
			{
				methods = new Dictionary<int, MethodInfo?>();
				_methodsByNameFromType.Add(type, methods);
			}
			else if (methods.TryGetValue(hash, out MethodInfo? result))
			{
				return result;
			}

			MethodInfo? methodInfo = type.GetMethod(name, DefaultBindingFlags);

			if (methodInfo != null)
			{
				methods.Add(hash, methodInfo);

				return methodInfo;
			}

			while (type.BaseType != null)
			{
				type = type.BaseType;
				methodInfo = type.GetMethod(name, PrivateBindingFlags);

				if (methodInfo != null)
				{
					break;
				}
			}

			methods.Add(hash, methodInfo);

			return methodInfo;
		}

		/// <summary>
		/// Search for a method by its signature. It will also search on base types for private methods if none is found in the target type.
		/// </summary>
		public static MethodInfo? FindMethodBySignature(this Type type, in string name, params Type[] parameters)
		{
			int hash = GetSignature(name, parameters).GetHashCode();

			if (!_methodsBySignatureFromType.TryGetValue(type, out Dictionary<int, MethodInfo?> methods))
			{
				methods = new Dictionary<int, MethodInfo?>();
				_methodsBySignatureFromType.Add(type, methods);
			}
			else if (methods.TryGetValue(hash, out MethodInfo? result))
			{
				return result;
			}

			MethodInfo? methodInfo = type.GetMethod(name, DefaultBindingFlags, null, parameters, null);

			if (methodInfo != null)
			{
				methods.Add(hash, methodInfo);

				return methodInfo;
			}

			while (type.BaseType != null)
			{
				type = type.BaseType;
				methodInfo = type.GetMethod(name, PrivateBindingFlags, null, parameters, null);

				if (methodInfo != null)
				{
					break;
				}
			}

			methods.Add(hash, methodInfo);

			return methodInfo;
		}

		/// <summary>
		/// Search for a setter by its name. It will also search on base types for private setters if none is found in the target type.
		/// </summary>
		public static MethodInfo? FindSetterByName(this Type type, in string name)
		{
			int hash = name.GetHashCode();

			if (!_setterByNameFromType.TryGetValue(type, out Dictionary<int, MethodInfo?> methods))
			{
				methods = new Dictionary<int, MethodInfo?>();
				_setterByNameFromType.Add(type, methods);
			}
			else if (methods.TryGetValue(hash, out MethodInfo? result))
			{
				return result;
			}

			MethodInfo? methodInfo = type.GetProperty(name, DefaultBindingFlags)?.GetSetMethod(true);

			if (methodInfo != null)
			{
				methods.Add(hash, methodInfo);

				return methodInfo;
			}

			while (type.BaseType != null)
			{
				type = type.BaseType;
				methodInfo = type.GetProperty(name, PrivateBindingFlags)?.GetSetMethod(true);

				if (methodInfo != null)
				{
					break;
				}
			}

			methods.Add(hash, methodInfo);

			return methodInfo;
		}

		/// 1<summary>
		/// True if the type is a value type or contains a parameterless constructor.
		/// </summary>
		public static bool CanCreateInstance(this Type type)
		{
			return type.IsValueType || type.GetConstructor(ConstructorBindingFlags, null, Type.EmptyTypes, null) != null;
		}

		/// <summary>
		/// Tries to create an instance of the given type by using either the <see cref="Activator"/> class or by any parameterless constructor on it.
		/// </summary>
		/// <seealso cref="CreateInstance"/>
		public static bool TryCreateInstance<T>(this Type type, [NotNullWhen(true)] out T instance)
		{
			try
			{
				instance = (T)type.CreateInstance();
			}
			catch
			{
				instance = default!;
			}

			return typeof(T).IsValueType || instance != null;
		}

		private static string GetSignature(string name, IReadOnlyList<Type>? parameters)
		{
			if (parameters == null || parameters.Count == 0)
			{
				return string.Format(SignatureFormat, name, string.Empty);
			}

			var stringBuilder = new StringBuilder();

			stringBuilder.Append(parameters[0].FullName);

			for (int i = 1; i < parameters.Count; i++)
			{
				stringBuilder.Append(",");
				stringBuilder.Append(parameters[i].FullName);
			}

			return string.Format(SignatureFormat, name, stringBuilder);
		}
	}
}
