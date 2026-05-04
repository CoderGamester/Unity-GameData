using GameLovers.GameData;
using Newtonsoft.Json;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ConfigTypesBinderTest
	{
		[Test]
		public void AddAllowedType_AfterConstruction_BindToTypeResolvesType()
		{
			var binder = new ConfigTypesBinder(null);
			var type = typeof(MockSingletonConfig);

			binder.AddAllowedType(type);

			var resolved = binder.BindToType(type.Assembly.FullName, type.FullName);

			Assert.AreEqual(type, resolved);
		}

		[Test]
		public void BindToType_UnregisteredType_Throws()
		{
			var binder = new ConfigTypesBinder(new[] { typeof(MockSingletonConfig) });
			var unregistered = typeof(System.IO.FileStream);

			Assert.Throws<JsonSerializationException>(
				() => binder.BindToType(unregistered.Assembly.FullName, unregistered.FullName));
		}

		[Test]
		public void BindToName_WhitelistedType_RoundTripsViaBindToType()
		{
			var binder = new ConfigTypesBinder(new[] { typeof(MockSingletonConfig) });
			var original = typeof(MockSingletonConfig);

			binder.BindToName(original, out var assemblyName, out var typeName);
			var resolved = binder.BindToType(assemblyName, typeName);

			Assert.AreEqual(original, resolved);
		}

		[Test]
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
