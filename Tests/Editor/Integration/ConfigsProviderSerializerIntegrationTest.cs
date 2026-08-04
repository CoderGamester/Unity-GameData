using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.GameData.Tests.Integration
{
	[TestFixture]
	public class ConfigsProviderSerializerIntegrationTest
	{
		[Serializable]
		public struct HeroConfig
		{
			public int Id;
			public string Name;
			public Color Theme;
		}

		[Serializable]
		[IgnoreServerSerialization]
		public struct LocalConfig
		{
			public bool IsDebug;
		}

		private ConfigsProvider _provider;
		private ConfigsSerializer _serializer;

		[SetUp]
		public void Setup()
		{
			_provider = new ConfigsProvider();
			_serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
		}

		[Test]
		// ADMIT: ColorJsonConverter.WriteJson must prefix the hex payload with '#', or ColorUtility cannot parse it
		// back and every colour field silently deserializes to white.
		// RCR: ColorJsonConverter.cs WriteJson — drop the `"#" +` prefix → RED (Theme comes back white, not blue).
		// Also reddens ConfigsSerializerTest.RoundTrip_UnityTypes_PreservesValues. 2026-08-02
		public void FullWorkflow_AddSerializeDeserializeAccess()
		{
			var heroes = new List<HeroConfig>
			{
				new HeroConfig { Id = 1, Name = "Warrior", Theme = Color.red },
				new HeroConfig { Id = 2, Name = "Mage", Theme = Color.blue }
			};
			_provider.AddConfigs(h => h.Id, heroes);
			_provider.AddSingletonConfig(new LocalConfig { IsDebug = true });

			var json = _serializer.Serialize(_provider, "1.0.0");
			
			// Verify that LocalConfig is NOT in json
			Assert.IsFalse(json.Contains("LocalConfig"));
			
			var newProvider = new ConfigsProvider();
			_serializer.Deserialize(json, newProvider);

			Assert.AreEqual(heroes.Count, newProvider.GetConfigsList<HeroConfig>().Count);
			Assert.AreEqual("Warrior", newProvider.GetConfig<HeroConfig>(1).Name);
			Assert.AreEqual(Color.blue, newProvider.GetConfig<HeroConfig>(2).Theme);
			// LocalConfig should be missing in newProvider (GetConfig throws InvalidOperationException when type is not registered)
			Assert.Throws<InvalidOperationException>(() => newProvider.GetConfig<LocalConfig>());
		}

		[Test]
		// ADMIT: ConfigsProvider.SetVersion overwrites unconditionally — Deserialize applies whatever version the
		// payload carries, including an older one; ordering is the caller's job, not the provider's.
		// RCR: ConfigsProvider.cs SetVersion — change `_version = version;` to `Math.Max(_version, version)` → RED
		// (the v5 payload leaves Version at 10). 2026-08-02
		public void BackendSync_VersionComparison()
		{
			_provider.UpdateTo(10, new Dictionary<Type, System.Collections.IEnumerable>());
			var jsonV5 = "{\"Version\":\"5\",\"Configs\":{}}";
			var jsonV15 = "{\"Version\":\"15\",\"Configs\":{}}";

			// V5 is older, but ConfigsSerializer.Deserialize currently calls UpdateTo directly.
			// The caller should normally check version.
			// Let's verify that Deserialize DOES set the version regardless.
			_serializer.Deserialize(jsonV5, _provider);
			Assert.AreEqual(5, (int)_provider.Version);

			_serializer.Deserialize(jsonV15, _provider);
			Assert.AreEqual(15, (int)_provider.Version);
		}
	}
}
