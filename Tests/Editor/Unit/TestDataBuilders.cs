using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLovers.GameData.Tests
{
	/// <summary>
	/// Builder pattern for creating test ConfigsProvider instances with common setups.
	/// Reduces boilerplate in test files and ensures consistent test data.
	/// </summary>
	public class ConfigsProviderBuilder
	{
		private readonly ConfigsProvider _provider = new ConfigsProvider();

		public ConfigsProviderBuilder WithSingleton<T>(T config)
		{
			_provider.AddSingletonConfig(config);
			return this;
		}

		public ConfigsProviderBuilder WithCollection<T>(Func<T, int> idResolver, IEnumerable<T> configs)
		{
			_provider.AddConfigs(idResolver, new List<T>(configs));
			return this;
		}

		public ConfigsProviderBuilder WithCollection<T>(Func<T, int> idResolver, params T[] configs)
		{
			_provider.AddConfigs(idResolver, new List<T>(configs));
			return this;
		}

		public ConfigsProviderBuilder WithVersion(ulong version)
		{
			_provider.UpdateTo(version, new Dictionary<Type, System.Collections.IEnumerable>());
			return this;
		}

		public ConfigsProvider Build() => _provider;

		/// <summary>
		/// Creates a default provider with common test configs pre-populated.
		/// </summary>
		public static ConfigsProviderBuilder Default()
		{
			return new ConfigsProviderBuilder()
				.WithSingleton(new MockSingletonConfig { Value = 42 })
				.WithCollection(c => c.Id, 
					new MockCollectionConfig { Id = 1, Name = "First" },
					new MockCollectionConfig { Id = 2, Name = "Second" });
		}
	}

	/// <summary>
	/// Builder for creating mock configs with validation attributes.
	/// </summary>
	public class MockValidatableConfigBuilder
	{
		private string _name = "DefaultName";
		private int _health = 100;
		private string _tag = "ABC";

		public MockValidatableConfigBuilder WithName(string name)
		{
			_name = name;
			return this;
		}

		public MockValidatableConfigBuilder WithHealth(int health)
		{
			_health = health;
			return this;
		}

		public MockValidatableConfigBuilder WithTag(string tag)
		{
			_tag = tag;
			return this;
		}

		public MockValidatableConfigBuilder Invalid()
		{
			_name = "";
			_health = 150; // Out of range 0-100
			_tag = "A";    // Too short, min 3
			return this;
		}

		public MockValidatableConfig Build() => new MockValidatableConfig
		{
			Name = _name,
			Health = _health,
			Tag = _tag
		};
	}

	/// <summary>
	/// Builder for Unity types config with sensible defaults.
	/// </summary>
	public class UnityTypesConfigBuilder
	{
		private Color _color = Color.white;
		private Vector2 _vec2 = Vector2.zero;
		private Vector3 _vec3 = Vector3.zero;
		private Vector4 _vec4 = Vector4.zero;
		private Quaternion _quat = Quaternion.identity;

		public UnityTypesConfigBuilder WithColor(Color color)
		{
			_color = color;
			return this;
		}

		public UnityTypesConfigBuilder WithVector2(Vector2 vec)
		{
			_vec2 = vec;
			return this;
		}

		public UnityTypesConfigBuilder WithVector3(Vector3 vec)
		{
			_vec3 = vec;
			return this;
		}

		public UnityTypesConfigBuilder WithVector4(Vector4 vec)
		{
			_vec4 = vec;
			return this;
		}

		public UnityTypesConfigBuilder WithQuaternion(Quaternion quat)
		{
			_quat = quat;
			return this;
		}

		public UnityTypesConfigBuilder WithRandomValues()
		{
			_color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
			_vec2 = new Vector2(1.1f, 2.2f);
			_vec3 = new Vector3(3.3f, 4.4f, 5.5f);
			_vec4 = new Vector4(6.6f, 7.7f, 8.8f, 9.9f);
			_quat = new Quaternion(0.1f, 0.2f, 0.3f, 0.4f);
			return this;
		}

		public MockUnityTypesConfig Build() => new MockUnityTypesConfig
		{
			Color = _color,
			Vec2 = _vec2,
			Vec3 = _vec3,
			Vec4 = _vec4,
			Quat = _quat
		};
	}

	// ════════════════════════════════════════════════════════════════════════
	// Common Test Config Types
	// ════════════════════════════════════════════════════════════════════════

	/// <summary>
	/// Simple singleton config for general testing.
	/// </summary>
	[Serializable]
	public struct MockSingletonConfig
	{
		public int Value;
	}

	/// <summary>
	/// Simple collection config with Id for testing keyed collections.
	/// </summary>
	[Serializable]
	public struct MockCollectionConfig
	{
		public int Id;
		public string Name;
	}

	/// <summary>
	/// Config with validation attributes for testing EditorConfigValidator.
	/// </summary>
	[Serializable]
	public class MockValidatableConfig
	{
		[Required]
		public string Name;
		[Range(0, 100)]
		public int Health;
		[MinLength(3)]
		public string Tag;
	}

	/// <summary>
	/// Sibling of <see cref="MockValidatableConfig"/> used when a single test needs to register both
	/// a singleton and a keyed collection of validatable configs through <see cref="ConfigsProvider"/>,
	/// which keys storage by type and therefore cannot hold both roles for the same <see cref="Type"/>.
	/// Shares the same attribute surface so validation assertions remain equivalent.
	/// </summary>
	[Serializable]
	public class MockValidatableConfigAlt
	{
		[Required]
		public string Name;
		[Range(0, 100)]
		public int Health;
		[MinLength(3)]
		public string Tag;
	}

	/// <summary>
	/// Config with Unity types for serialization testing.
	/// </summary>
	[Serializable]
	public struct MockUnityTypesConfig
	{
		public Color Color;
		public Vector2 Vec2;
		public Vector3 Vec3;
		public Vector4 Vec4;
		public Quaternion Quat;
	}

	/// <summary>
	/// Config marked to be ignored during server serialization.
	/// </summary>
	[Serializable]
	[IgnoreServerSerialization]
	public struct MockIgnoredConfig
	{
		public int Value;
	}

	/// <summary>
	/// Non-serializable config (missing [Serializable]) for error testing.
	/// </summary>
	public struct MockNonSerializableConfig
	{
		public int Value;
	}
}
