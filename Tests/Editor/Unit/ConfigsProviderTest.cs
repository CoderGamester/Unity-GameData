using System;
using System.Collections.Generic;
using System.Linq;
using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ConfigsProviderTest
	{
		private ConfigsProvider _provider;

		[Serializable]
		public struct MockSingletonConfig
		{
			public int Value;
		}

		[Serializable]
		public struct MockCollectionConfig
		{
			public int Id;
			public string Name;
		}

		[SetUp]
		public void Setup()
		{
			_provider = new ConfigsProvider();
		}

		[Test]
		// ADMIT: ConfigsProvider.AddSingletonConfig must store the caller's instance under the singleton id, not a
		// default-constructed placeholder.
		// RCR: ConfigsProvider.cs AddSingletonConfig — store `default(T)` instead of `config` → RED (Value is 0, not
		// 42). Also reddens GetConfig_Singleton_ReturnsCorrect and the serializer round-trip fixtures. 2026-08-02
		public void AddSingletonConfig_Success()
		{
			var config = new MockSingletonConfig { Value = 42 };
			_provider.AddSingletonConfig(config);

			Assert.AreEqual(42, _provider.GetConfig<MockSingletonConfig>().Value);
		}

		[Test]
		// ADMIT: ConfigsProvider.AddSingletonConfig uses Dictionary.Add, so registering the same type twice is a hard
		// error — the package's documented one-role-per-type rule.
		// RCR: ConfigsProvider.cs AddSingletonConfig — change `_configs.Add(typeof(T), ...)` to the indexer
		// `_configs[typeof(T)] = ...` → RED (no ArgumentException on the second call). 2026-08-02
		public void AddSingletonConfig_DuplicateType_ThrowsArgumentException()
		{
			_provider.AddSingletonConfig(new MockSingletonConfig());
			Assert.Throws<ArgumentException>(() => _provider.AddSingletonConfig(new MockSingletonConfig()));
		}

		[Test]
		// ADMIT: ConfigsProvider.AddConfigs must key each entry by `referenceIdResolver(config)`, not by list
		// position.
		// RCR: ConfigsProvider.cs AddConfigs — change `dictionary.Add(referenceIdResolver(configList[i]), ...)` to
		// `dictionary.Add(i, ...)` → RED (GetConfig(1).Name is "Two"). Also reddens the id-keyed lookup siblings.
		// 2026-08-02
		public void AddConfigs_WithIdResolver_StoresCorrectly()
		{
			var configs = new List<MockCollectionConfig>
			{
				new MockCollectionConfig { Id = 1, Name = "One" },
				new MockCollectionConfig { Id = 2, Name = "Two" }
			};

			_provider.AddConfigs(c => c.Id, configs);

			Assert.AreEqual("One", _provider.GetConfig<MockCollectionConfig>(1).Name);
			Assert.AreEqual("Two", _provider.GetConfig<MockCollectionConfig>(2).Name);
			Assert.AreEqual(2, _provider.GetConfigsList<MockCollectionConfig>().Count);
		}

		[Test]
		// ADMIT: ConfigsProvider.AddConfigs uses Dictionary.Add for the type slot, so a second registration of the
		// same type throws rather than silently replacing the container.
		// RCR: ConfigsProvider.cs AddConfigs — change `_configs.Add(typeof(T), dictionary);` to the indexer
		// `_configs[typeof(T)] = dictionary;` → RED (no ArgumentException on the second call). 2026-08-02
		public void AddConfigs_DuplicateType_ThrowsArgumentException()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig>());
			Assert.Throws<ArgumentException>(() => _provider.AddConfigs(c => c.Id, new List<MockCollectionConfig>()));
		}

		[Test]
		// ADMIT: ConfigsProvider.AddConfigs must reject a null configList with ArgumentNullException before it
		// dereferences `configList.Count`.
		// RCR: ConfigsProvider.cs AddConfigs — disable the `configList == null` guard → RED (NullReferenceException
		// arrives instead of the expected ArgumentNullException). 2026-08-02
		public void AddConfigs_NullList_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => _provider.AddConfigs<MockCollectionConfig>(c => c.Id, null));
		}

		[Test]
		public void GetConfig_Singleton_ReturnsCorrect()
		{
			_provider.AddSingletonConfig(new MockSingletonConfig { Value = 10 });
			var config = _provider.GetConfig<MockSingletonConfig>();
			Assert.AreEqual(10, config.Value);
		}

		[Test]
		public void GetConfig_ById_ReturnsCorrect()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig> { new MockCollectionConfig { Id = 5, Name = "Test" } });
			var config = _provider.GetConfig<MockCollectionConfig>(5);
			Assert.AreEqual("Test", config.Name);
		}

		[Test]
		// ADMIT: ConfigsProvider.GetConfig<T>(int) indexes the container directly, so an unknown id is a
		// KeyNotFoundException rather than a silent default.
		// RCR: ConfigsProvider.cs GetConfig<T>(int) — replace the indexer with a TryGetValue-or-default → RED (no
		// exception is thrown). 2026-08-02
		public void GetConfig_MissingId_ThrowsKeyNotFoundException()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig>());
			Assert.Throws<KeyNotFoundException>(() => _provider.GetConfig<MockCollectionConfig>(99));
		}

		[Test]
		// ADMIT: ConfigsProvider.GetConfig<T>() must refuse an id-keyed container with InvalidOperationException
		// instead of handing back a default — the singleton-vs-keyed distinction is the API's main trap.
		// RCR: ConfigsProvider.cs GetConfig<T>() — append `&& false` to the `!TryGetConfig<T>(out var config)`
		// condition → RED (returns default, no exception). 2026-08-02
		public void GetConfig_SingletonOnCollection_ThrowsInvalidOperationException()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig> { new MockCollectionConfig { Id = 1 } });
			Assert.Throws<InvalidOperationException>(() => _provider.GetConfig<MockCollectionConfig>());
		}

		[Test]
		// ADMIT: ConfigsProvider.TryGetConfig<T>(out T) must recognise a registered singleton container and report
		// the hit.
		// RCR: ConfigsProvider.cs TryGetConfig<T>(out T) — replace the final `is IReadOnlyDictionary<int,T> ... &&
		// TryGetValue` expression with `return false;` → RED (IsTrue fails). Broad: every GetConfig<T>() caller in
		// this and the serializer fixtures also fails. 2026-08-02
		public void TryGetConfig_Singleton_Exists_ReturnsTrue()
		{
			_provider.AddSingletonConfig(new MockSingletonConfig());
			Assert.IsTrue(_provider.TryGetConfig<MockSingletonConfig>(out _));
		}

		[Test]
		// ADMIT: ConfigsProvider.TryGetConfig<T>(out T) must short-circuit to false when the type was never
		// registered, before touching the container.
		// RCR: ConfigsProvider.cs TryGetConfig<T>(out T) — change the unregistered-type `return false;` to
		// `return true;` → RED (IsFalse fails). 2026-08-02
		public void TryGetConfig_Singleton_NotExists_ReturnsFalse()
		{
			Assert.IsFalse(_provider.TryGetConfig<MockSingletonConfig>(out _));
		}

		[Test]
		// ADMIT: ConfigsProvider.TryGetConfig<T>(int, out T) must delegate to the keyed container's TryGetValue and
		// report the hit.
		// RCR: ConfigsProvider.cs TryGetConfig<T>(int, out T) — replace the body with `config = default;
		// return false;` → RED (IsTrue fails). 2026-08-02
		public void TryGetConfig_ById_Exists_ReturnsTrue()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig> { new MockCollectionConfig { Id = 1 } });
			Assert.IsTrue(_provider.TryGetConfig<MockCollectionConfig>(1, out _));
		}

		[Test]
		// ADMIT: ConfigsProvider.TryGetConfig<T>(int, out T) must report a miss for an id absent from the keyed
		// container.
		// RCR: ConfigsProvider.cs TryGetConfig<T>(int, out T) — append `|| true` to the TryGetValue return → RED
		// (IsFalse fails). 2026-08-02
		public void TryGetConfig_ById_NotExists_ReturnsFalse()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig>());
			Assert.IsFalse(_provider.TryGetConfig<MockCollectionConfig>(1, out _));
		}

		[Test]
		public void GetConfigsList_ReturnsNewListInstance()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig> { new MockCollectionConfig { Id = 1 } });
			var list1 = _provider.GetConfigsList<MockCollectionConfig>();
			var list2 = _provider.GetConfigsList<MockCollectionConfig>();
			
			Assert.AreNotSame(list1, list2);
			Assert.AreEqual(1, list1.Count);
		}

		[Test]
		public void GetConfigsDictionary_ReturnsReadOnlyDictionary()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig> { new MockCollectionConfig { Id = 1, Name = "Test" } });
			var dict = _provider.GetConfigsDictionary<MockCollectionConfig>();
			
			Assert.AreEqual(1, dict.Count);
			Assert.AreEqual("Test", dict[1].Name);
		}

		[Test]
		// ADMIT: ConfigsProvider.GetConfigsDictionary<T> indexes `_configs` directly, so an unregistered type throws
		// KeyNotFoundException rather than returning null — every other accessor relies on that.
		// RCR: ConfigsProvider.cs GetConfigsDictionary<T> — replace the indexer with a TryGetValue-or-null → RED (no
		// exception is thrown). 2026-08-02
		public void GetConfigsDictionary_TypeNotAdded_ThrowsKeyNotFoundException()
		{
			Assert.Throws<KeyNotFoundException>(() => _provider.GetConfigsDictionary<MockSingletonConfig>());
		}

		[Test]
		// ADMIT: ConfigsProvider.EnumerateConfigs<T> must stream the container's live Values.
		// RCR: ConfigsProvider.cs EnumerateConfigs<T> — `return new List<T>();` → RED (the foreach counts 0, not 2).
		// 2026-08-02
		public void EnumerateConfigs_ReturnsAllValues()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig> 
			{ 
				new MockCollectionConfig { Id = 1 }, 
				new MockCollectionConfig { Id = 2 } 
			});
			
			var count = 0;
			foreach (var config in _provider.EnumerateConfigs<MockCollectionConfig>())
			{
				count++;
			}
			Assert.AreEqual(2, count);
		}

		[Test]
		// ADMIT: ConfigsProvider.EnumerateConfigsWithIds<T> must yield the container's real id/value pairs, so the
		// resolver-assigned id survives.
		// RCR: ConfigsProvider.cs EnumerateConfigsWithIds<T> — return a fresh `{ { 0, default(T) } }` dictionary →
		// RED (pair.Key is 0, not 10). 2026-08-02
		public void EnumerateConfigsWithIds_ReturnsKeyValuePairs()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig> 
			{ 
				new MockCollectionConfig { Id = 10, Name = "Ten" } 
			});
			
			var pair = _provider.EnumerateConfigsWithIds<MockCollectionConfig>().First();
			Assert.AreEqual(10, pair.Key);
			Assert.AreEqual("Ten", pair.Value.Name);
		}

		[Test]
		// ADMIT: ConfigsProvider.UpdateTo must set the version as well as merge the payload — the two are meant to
		// be atomic, which is why SetVersion is internal.
		// RCR: ConfigsProvider.cs UpdateTo — delete the `SetVersion(version);` call → RED (Version stays 0). Also
		// reddens ConfigsSerializerTest.Deserialize_ValidJson_IntoExistingProvider. 2026-08-02
		public void UpdateTo_SetsVersionAndMergesConfigs()
		{
			var newConfigs = new Dictionary<Type, System.Collections.IEnumerable>
			{
				{ typeof(MockSingletonConfig), new Dictionary<int, MockSingletonConfig> { { 0, new MockSingletonConfig { Value = 100 } } } }
			};

			_provider.UpdateTo(5, newConfigs);

			Assert.AreEqual(5, _provider.Version);
			Assert.AreEqual(100, _provider.GetConfig<MockSingletonConfig>().Value);
		}

		[Test]
		// ADMIT: ConfigsProvider.GetAllConfigs must expose the live `_configs` map — it is what the serializer, the
		// binder and the Config Browser all enumerate.
		// RCR: ConfigsProvider.cs GetAllConfigs — return a fresh empty dictionary → RED (Count is 0, not 2). Broad:
		// also reddens the serializer and ConfigBrowser fixtures. 2026-08-02
		public void GetAllConfigs_ReturnsAllRegisteredTypes()
		{
			_provider.AddSingletonConfig(new MockSingletonConfig());
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig>());

			var all = _provider.GetAllConfigs();
			Assert.AreEqual(2, all.Count);
			Assert.IsTrue(all.ContainsKey(typeof(MockSingletonConfig)));
			Assert.IsTrue(all.ContainsKey(typeof(MockCollectionConfig)));
		}

		[Test]
		public void AddAllConfigs_BulkPayload_RegistersAllTypes()
		{
			var singletonContainer = new Dictionary<int, MockSingletonConfig>
			{
				{ 0, new MockSingletonConfig { Value = 7 } }
			};
			var collectionContainer = new Dictionary<int, MockCollectionConfig>
			{
				{ 1, new MockCollectionConfig { Id = 1, Name = "One" } },
				{ 2, new MockCollectionConfig { Id = 2, Name = "Two" } }
			};
			var payload = new Dictionary<Type, System.Collections.IEnumerable>
			{
				{ typeof(MockSingletonConfig), singletonContainer },
				{ typeof(MockCollectionConfig), collectionContainer }
			};

			_provider.AddAllConfigs(payload);

			Assert.AreEqual(7, _provider.GetConfig<MockSingletonConfig>().Value);
			Assert.AreEqual("One", _provider.GetConfig<MockCollectionConfig>(1).Name);
			Assert.AreEqual("Two", _provider.GetConfig<MockCollectionConfig>(2).Name);
			Assert.AreEqual(2, _provider.GetConfigsList<MockCollectionConfig>().Count);

			var all = _provider.GetAllConfigs();
			Assert.AreEqual(2, all.Count);
			Assert.IsTrue(all.ContainsKey(typeof(MockSingletonConfig)));
			Assert.IsTrue(all.ContainsKey(typeof(MockCollectionConfig)));
		}
	}
}
