using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace GameLovers.GameData
{
	/// <summary>
	/// A custom serialization binder that restricts deserialization to a whitelist of allowed types.
	/// This prevents type injection attacks when using TypeNameHandling.Auto with Newtonsoft.Json.
	/// 
	/// The binder auto-discovers allowed types from the configs being serialized/deserialized,
	/// plus allows common .NET collection types needed for internal structure.
	/// </summary>
	public class ConfigTypesBinder : ISerializationBinder
	{
		private readonly HashSet<Type> _allowedTypes;
		private readonly HashSet<string> _allowedTypeNames;
		
		// Built-in collection types that are safe and required for internal structure
		private static readonly HashSet<Type> _builtInAllowedTypes = new HashSet<Type>
		{
			typeof(Dictionary<,>),
			typeof(List<>),
			typeof(HashSet<>),
			typeof(int),
			typeof(string),
			typeof(float),
			typeof(double),
			typeof(bool),
			typeof(long),
			typeof(ulong),
			typeof(short),
			typeof(ushort),
			typeof(byte),
			typeof(sbyte),
			typeof(decimal),
			typeof(char),
			typeof(DateTime),
			typeof(TimeSpan),
			typeof(Guid),
			typeof(object[]),
			typeof(int[]),
			typeof(string[]),
			typeof(float[]),
			typeof(double[]),
		};

		public ConfigTypesBinder(IEnumerable<Type> allowedConfigTypes)
		{
			_allowedTypes = new HashSet<Type>(_builtInAllowedTypes);
			_allowedTypeNames = new HashSet<string>();
			
			if (allowedConfigTypes != null)
			{
				foreach (var type in allowedConfigTypes)
				{
					AddAllowedType(type);
				}
			}
		}

		/// <summary>
		/// Creates a ConfigTypesBinder that auto-discovers types from a ConfigsProvider.
		/// </summary>
		/// <param name="provider">The provider to discover types from.</param>
		/// <returns>A new ConfigTypesBinder with the discovered types.</returns>
		public static ConfigTypesBinder FromProvider(IConfigsProvider provider)
		{
			if (provider == null)
			{
				return new ConfigTypesBinder(null);
			}

			var configs = provider.GetAllConfigs();
			var types = new List<Type>();
			
			foreach (var kvp in configs)
			{
				types.Add(kvp.Key);
				
				// Also allow the Dictionary<int, T> concrete type for this config
				var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), kvp.Key);
				types.Add(dictType);
			}
			
			return new ConfigTypesBinder(types);
		}

		/// <summary>
		/// Adds a type to the allowed types list.
		/// </summary>
		public void AddAllowedType(Type type)
		{
			if (type == null) return;
			
			_allowedTypes.Add(type);
			_allowedTypeNames.Add(type.FullName);
			_allowedTypeNames.Add(type.AssemblyQualifiedName);
			
			// For generic types, also allow the generic type definition
			if (type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				_allowedTypes.Add(type.GetGenericTypeDefinition());
				
				// Allow generic arguments as well
				foreach (var arg in type.GetGenericArguments())
				{
					AddAllowedType(arg);
				}
			}
		}

		/// <inheritdoc />
		public Type BindToType(string assemblyName, string typeName)
		{
			// Construct full type name
			var fullTypeName = string.IsNullOrEmpty(assemblyName) 
				? typeName 
				: $"{typeName}, {assemblyName}";
			
			// Try to resolve the type
			var type = Type.GetType(fullTypeName);
			
			if (type == null)
			{
				// Try without assembly for common types
				type = Type.GetType(typeName);
			}
			
			if (type == null)
			{
				throw new Newtonsoft.Json.JsonSerializationException(
					$"Type '{fullTypeName}' could not be resolved. Ensure the type exists and is accessible.");
			}
			
			// Check if type is allowed
			if (IsTypeAllowed(type))
			{
				return type;
			}
			
			throw new Newtonsoft.Json.JsonSerializationException(
				$"Type '{type.FullName}' is not allowed for deserialization. " +
				"Only whitelisted config types are permitted for security reasons.");
		}

		/// <inheritdoc />
		public void BindToName(Type serializedType, out string assemblyName, out string typeName)
		{
			// Use standard naming for serialization
			assemblyName = serializedType.Assembly.FullName;
			typeName = serializedType.FullName;
		}

		private bool IsTypeAllowed(Type type)
		{
			// Direct match
			if (_allowedTypes.Contains(type))
			{
				return true;
			}
			
			// Check by name (handles cross-assembly scenarios)
			if (_allowedTypeNames.Contains(type.FullName) || 
			    _allowedTypeNames.Contains(type.AssemblyQualifiedName))
			{
				return true;
			}
			
			// For generic types, check if the generic definition is allowed
			// and all type arguments are allowed
			if (type.IsGenericType)
			{
				var genericDef = type.GetGenericTypeDefinition();
				if (_allowedTypes.Contains(genericDef) || _builtInAllowedTypes.Contains(genericDef))
				{
					// Check all generic arguments
					foreach (var arg in type.GetGenericArguments())
					{
						if (!IsTypeAllowed(arg))
						{
							return false;
						}
					}
					return true;
				}
			}
			
			// Check built-in allowed types
			if (_builtInAllowedTypes.Contains(type))
			{
				return true;
			}
			
			// Allow arrays of allowed types
			if (type.IsArray)
			{
				return IsTypeAllowed(type.GetElementType());
			}
			
			return false;
		}
	}
}
