using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#nullable enable
// ReSharper disable once CheckNamespace

namespace GameLovers.GameData
{
	/// <summary>
	/// Utility methods for both Unity <see cref="Object"/> and C# <see cref="object"/> types.
	/// </summary>
	/// <author>Bruno Coimbra, https://github.com/coimbrastudios/framework/blob/master/Coimbra/Utilities/</author>
	public static class ObjectExtensions
	{
		private static readonly Dictionary<Type, string> Cache = new();
		private const string PersistentSceneName = "DontDestroyOnLoad";

		/// <summary>
		/// Defines the possible results when calling <see cref="ObjectUtility.Dispose"/>, <see cref="GameObjectUtility.Dispose"/> or <see cref="Actor.Dispose"/>.
		/// </summary>
		public enum ObjectDisposeResult
		{
			None = 0,
			Pooled = 1,
			Destroyed = 2,
		}

		/// <summary>
		/// Destroys the <see cref="GameObject"/> correctly by checking if it isn't already an <see cref="Actor"/> first.
		/// </summary>
		/// <seealso cref="Actor.Dispose"/>
		public static ObjectDisposeResult Dispose(this GameObject? gameObject, bool forceDestroy)
		{
			if (!gameObject.TryGetValid(out gameObject))
			{
				return ObjectDisposeResult.None;
			}

			if (Application.isPlaying)
			{
				Object.Destroy(gameObject);
			}
			else
			{
				Object.DestroyImmediate(gameObject);
			}

			return ObjectDisposeResult.Destroyed;
		}

		/// <summary>
		/// Gets or adds the given component type.
		/// </summary>
		public static T GetOrAddComponent<T>(this GameObject gameObject)
			where T : Component
		{
			return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
		}

		/// <summary>
		/// Gets or adds the given component type.
		/// </summary>
		public static Component GetOrAddComponent(this GameObject gameObject, SerializableType<Component> type)
		{
			return gameObject.TryGetComponent(type.Value, out Component component) ? component : gameObject.AddComponent(type.Value);
		}

		/// <summary>
		/// Checks if the game object have the given component type.
		/// </summary>
		public static bool HasComponent<T>(this GameObject gameObject)
		{
#pragma warning disable UNT0014
			return gameObject.TryGetComponent<T>(out _);
#pragma warning restore UNT0014
		}

		/// <summary>
		/// Checks if the game object have the given component type.
		/// </summary>
		public static bool HasComponent(this GameObject gameObject, Type type)
		{
			return gameObject.TryGetComponent(type, out _);
		}

		/// <summary>
		/// Check if object is currently persistent.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPersistent(this GameObject gameObject)
		{
			Scene scene = gameObject.scene;

			return scene is { buildIndex: -1, name: PersistentSceneName };
		}

		/// <summary>
		/// Destroys the <see cref="Object"/> correctly by checking if it isn't already an <see cref="Actor"/> first.
		/// </summary>
		/// <seealso cref="Actor.Dispose"/>
		public static ObjectDisposeResult Dispose(this Object o, bool forceDestroy)
		{
			if (!o.TryGetValid(out o))
			{
				return ObjectDisposeResult.None;
			}

			if (o is GameObject gameObject)
			{
				return gameObject.Dispose(forceDestroy);
			}

			if (o is IDisposable disposable)
			{
				disposable.Dispose();
			}

			if (Application.isPlaying)
			{
				Object.Destroy(o);
			}
			else
			{
				Object.DestroyImmediate(o);
			}

			return ObjectDisposeResult.Destroyed;
		}

		/// <summary>
		/// Gets a valid object to be used with ?. and ?? operators.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetValid<T>(this T o)
		{
			if (o is Object obj)
			{
#pragma warning disable CS8603 // Possible null reference return.
				return obj != null ? o : default;
#pragma warning restore CS8603 // Possible null reference return.
			}

			return o;
		}

		/// <summary>
		/// Safe way to check if an object is valid even if the object is an Unity <see cref="Object"/> and got destroyed already.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsValid(this object o)
		{
			if (o is Object obj)
			{
				return obj != null;
			}

			return o != null;
		}

		/// <summary>
		/// Safe way to check if an object is valid even if the object is an Unity <see cref="Object"/> and got destroyed already, getting a valid object to be used with ?. and ?? operators too.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryGetValid<T>(this T o, [NotNullWhen(true)] out T valid)
		{
			valid = GetValid(o);

			return valid != null;
		}

		/// <summary>
		/// Gets the <see cref="string"/> for the specified <see cref="Type"/>..
		/// </summary>
		public static string GetDisplayString(this Type type)
		{
			if (Cache.TryGetValue(type, out string value))
			{
				return value;
			}

			static void appendGenericParameters(StringBuilder stringBuilder, Type type)
			{
				Type[] types = type.GenericTypeArguments;

				if (!type.IsGenericType || types.Length == 0)
				{
					return;
				}

				stringBuilder.Append("<");
				stringBuilder.Append(types[0].Name);
				appendGenericParameters(stringBuilder, types[0]);

				for (int i = 1; i < type.GenericTypeArguments.Length; i++)
				{
					stringBuilder.Append(", ");
					stringBuilder.Append(types[i].Name);
					appendGenericParameters(stringBuilder, types[i]);
				}

				stringBuilder.Append(">");
			}

			var stringBuilder = new StringBuilder();

			stringBuilder.Append(type.Name);
			appendGenericParameters(stringBuilder, type);

			if (!string.IsNullOrWhiteSpace(type.Namespace))
			{
				stringBuilder.Append(" (").Append(type.Namespace).Append(")");
			}

			value = stringBuilder.ToString();

			Cache.Add(type, value);

			return value;
		}

		/// <summary>Resolves a type by name across the loaded assemblies, returning null when nothing matches.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Type GetTypeFromString(in string fullTypeName)
		{
			int index = fullTypeName.IndexOf(' ');
			string assemblyName = fullTypeName.Substring(0, index);
			string typeName = fullTypeName.Substring(index + 1);

			return Assembly.Load(assemblyName).GetType(typeName);
		}
	}
}
