using GameLovers.GameData;
using Newtonsoft.Json;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ConfigTypesBinderTest
	{
		[Test]
		// ADMIT: ConfigTypesBinder.AddAllowedType must whitelist types handed to it after construction, not only
		// those passed to the constructor.
		// RCR: ConfigTypesBinder.cs AddAllowedType — invert the null guard to `if (type != null) return;` → RED
		// (BindToType throws "not allowed for deserialization"). Broad: every whitelisting path shares this method.
		// 2026-08-02
		public void AddAllowedType_AfterConstruction_BindToTypeResolvesType()
		{
			var binder = new ConfigTypesBinder(null);
			var type = typeof(MockSingletonConfig);

			binder.AddAllowedType(type);

			var resolved = binder.BindToType(type.Assembly.FullName, type.FullName);

			Assert.AreEqual(type, resolved);
		}

		[Test]
		// ADMIT: ConfigTypesBinder.BindToType must reject a resolvable-but-unwhitelisted type (here FileStream) —
		// this is the type-injection guard.
		// RCR: ConfigTypesBinder.cs BindToType — replace the trailing rejection throw with `return type;` → RED (no
		// JsonSerializationException). Also reddens the two annotated binder-rejection tests in this folder.
		// 2026-08-02
		public void BindToType_UnregisteredType_Throws()
		{
			var binder = new ConfigTypesBinder(new[] { typeof(MockSingletonConfig) });
			var unregistered = typeof(System.IO.FileStream);

			Assert.Throws<JsonSerializationException>(
				() => binder.BindToType(unregistered.Assembly.FullName, unregistered.FullName));
		}

		[Test]
		// ADMIT: ConfigTypesBinder.BindToName must emit the FULL type name, or the $type strings it writes cannot be
		// resolved by its own BindToType on the way back.
		// RCR: ConfigTypesBinder.cs BindToName — change `typeName = serializedType.FullName;` to
		// `serializedType.Name` → RED (BindToType throws "could not be resolved"). 2026-08-02
		public void BindToName_WhitelistedType_RoundTripsViaBindToType()
		{
			var binder = new ConfigTypesBinder(new[] { typeof(MockSingletonConfig) });
			var original = typeof(MockSingletonConfig);

			binder.BindToName(original, out var assemblyName, out var typeName);
			var resolved = binder.BindToType(assemblyName, typeName);

			Assert.AreEqual(original, resolved);
		}

		[Test]
		// ADMIT: ConfigTypesBinder.FromProvider must hand the discovered types to the binder it returns, covering
		// both the singleton and the keyed-collection config types.
		// RCR: ConfigTypesBinder.cs FromProvider — `return new ConfigTypesBinder(null);` → RED (BindToType throws
		// "not allowed for deserialization"). Note: deleting either `types.Add` alone stays GREEN — AddAllowedType
		// re-adds the bare type from the Dictionary<int,T> generic arguments. 2026-08-02
		public void FromProvider_PopulatesFromEnumerableConfigs_AllowsAllProviderTypes()
		{
			var provider = new ConfigsProviderBuilder()
				.WithSingleton(new MockSingletonConfig { Value = 1 })
				.WithCollection(c => c.Id, new MockCollectionConfig { Id = 1, Name = "A" })
				.Build();

			var binder = ConfigTypesBinder.FromProvider(provider);
			var singletonType = typeof(MockSingletonConfig);
			var collectionType = typeof(MockCollectionConfig);

			Assert.AreEqual(singletonType, binder.BindToType(singletonType.Assembly.FullName, singletonType.FullName));
			Assert.AreEqual(collectionType, binder.BindToType(collectionType.Assembly.FullName, collectionType.FullName));
		}
	}
}
