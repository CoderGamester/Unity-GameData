using System;
using System.Collections.Generic;

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

		public ConfigsProvider Build() => _provider;
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

}
