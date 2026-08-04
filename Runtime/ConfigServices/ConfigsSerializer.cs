using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GameLovers.GameData
{

	/// <summary>
	/// Class that represents the data to be serialized.
	/// </summary>
	internal class SerializedConfigs
	{
		public string Version;
		
		public Dictionary<Type, IEnumerable> Configs;
	}

	/// <summary>
	/// This attribute can be added to config structs so they are ignored by the serializer.
	/// This way they won't be sent to server when they are not needed.
	/// </summary>
	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
	public class IgnoreServerSerialization : Attribute { }

	/// <summary>
	/// Defines the security mode for serialization.
	/// </summary>
	/// <remarks>
	/// <see cref="Secure"/> is serialize-only: emitting no type metadata means the internal
	/// <c>Dictionary&lt;Type, IEnumerable&gt;</c> cannot be reconstructed, so it cannot round-trip.
	/// Use it for configs sent TO untrusted targets, and <see cref="TrustedOnly"/> for data read back.
	/// </remarks>
	public enum SerializationSecurityMode
	{
		TrustedOnly, // TypeNameHandling.Auto, with a SerializationBinder restricting allowed types.
		Secure       // TypeNameHandling.None: no type metadata emitted or processed.
	}

	/// <summary>
	/// Serializer for game configuration data with security protections.
	/// 
	/// Security features:
	/// - TrustedOnly mode uses a SerializationBinder to whitelist allowed types
	/// - Secure mode disables TypeNameHandling entirely (serialize-only)
	/// - MaxDepth is set to prevent stack overflow attacks from deeply nested JSON
	/// 
	/// These configs are designed to be shared between client and server.
	/// </summary>
	public class ConfigsSerializer : IConfigsSerializer
	{
		/// <summary>
		/// Maximum JSON nesting depth to prevent stack overflow attacks.
		/// </summary>
		public const int DefaultMaxDepth = 128;
		
		private readonly JsonSerializerSettings _settings;
		private readonly SerializationSecurityMode _securityMode;
		private ConfigTypesBinder _binder;

		/// <summary>
		/// Gets the current security mode.
		/// </summary>
		public SerializationSecurityMode SecurityMode => _securityMode;

		public ConfigsSerializer(SerializationSecurityMode mode = SerializationSecurityMode.TrustedOnly, int maxDepth = DefaultMaxDepth)
		{
			_securityMode = mode;
			_binder = new ConfigTypesBinder(null);
			
			_settings = new JsonSerializerSettings()
			{
				TypeNameHandling = mode == SerializationSecurityMode.Secure ? TypeNameHandling.None : TypeNameHandling.Auto,
				SerializationBinder = mode == SerializationSecurityMode.TrustedOnly ? _binder : null,
				MaxDepth = maxDepth,
				Converters = new List<JsonConverter>()
				{
					new StringEnumConverter(),
					new ColorJsonConverter(),
					new Vector2JsonConverter(),
					new Vector3JsonConverter(),
					new Vector4JsonConverter(),
					new QuaternionJsonConverter()
				}
			};
		}

		/// <inheritdoc />
		public string Serialize(IConfigsProvider cfg, string version)
		{
			var configs = cfg.GetAllConfigs();
			var serializedConfig = new SerializedConfigs()
			{
				Version = version,
				Configs = new Dictionary<Type, IEnumerable>()
			};
			
			foreach (var type in configs.Keys)
			{
				if (type.CustomAttributes.Any(c => c.AttributeType == typeof(IgnoreServerSerialization)))
				{
					continue;
				}
				if (!type.IsSerializable)
				{
					throw new Exception(@$"Config {type} could not be serialized.
						 If this is not used in game logic please add [IgnoreServerSerialization]");
				}

				serializedConfig.Configs[type] = configs[type];
				
				// Auto-register types for the binder (TrustedOnly mode)
				if (_securityMode == SerializationSecurityMode.TrustedOnly)
				{
					_binder.AddAllowedType(type);
					var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), type);
					_binder.AddAllowedType(dictType);
				}
			}
			
			return JsonConvert.SerializeObject(serializedConfig, _settings);
		}

		/// <summary>
		/// Registers additional types to be allowed during deserialization.
		/// Only effective in TrustedOnly mode.
		/// Call this before Deserialize if you need to allow types not present in the serialized data.
		/// </summary>
		/// <param name="types">The types to allow.</param>
		public void RegisterAllowedTypes(IEnumerable<Type> types)
		{
			if (_securityMode != SerializationSecurityMode.TrustedOnly || types == null)
			{
				return;
			}
			
			foreach (var type in types)
			{
				_binder.AddAllowedType(type);
				var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), type);
				_binder.AddAllowedType(dictType);
			}
		}

		/// <summary>
		/// Registers types from an existing provider to be allowed during deserialization.
		/// Only effective in TrustedOnly mode.
		/// </summary>
		/// <param name="provider">The provider containing the types to register.</param>
		public void RegisterAllowedTypesFromProvider(IConfigsProvider provider)
		{
			if (_securityMode != SerializationSecurityMode.TrustedOnly || provider == null)
			{
				return;
			}
			
			foreach (var type in provider.GetAllConfigs().Keys)
			{
				_binder.AddAllowedType(type);
				var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), type);
				_binder.AddAllowedType(dictType);
			}
		}

		/// <inheritdoc />
		public T Deserialize<T>(string serialized) where T : IConfigsAdder
		{
			var cfg = Activator.CreateInstance(typeof(T)) as IConfigsAdder;
			Deserialize(serialized, cfg);
			return (T)cfg;
		}
		
		/// <inheritdoc />
		public void Deserialize(string serialized, IConfigsAdder cfg)
		{
			var configs = JsonConvert.DeserializeObject<SerializedConfigs>(serialized, _settings);
			if (!ulong.TryParse(configs.Version, out var versionNumber))
			{
				versionNumber = 0;
			}
			cfg.UpdateTo(versionNumber, configs?.Configs);
		}
	}
}
