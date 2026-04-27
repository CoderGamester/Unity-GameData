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
		public void AddSingletonConfig_Success()
		{
			var config = new MockSingletonConfig { Value = 42 };
			_provider.AddSingletonConfig(config);

			Assert.AreEqual(42, _provider.GetConfig<MockSingletonConfig>().Value);
		}

		[Test]
		public void AddSingletonConfig_DuplicateType_ThrowsArgumentException()
		{
			_provider.AddSingletonConfig(new MockSingletonConfig());
			Assert.Throws<ArgumentException>(() => _provider.AddSingletonConfig(new MockSingletonConfig()));
		}

		[Test]
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
		public void AddConfigs_DuplicateType_ThrowsArgumentException()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig>());
			Assert.Throws<ArgumentException>(() => _provider.AddConfigs(c => c.Id, new List<MockCollectionConfig>()));
		}

		[Test]
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
		public void GetConfig_MissingId_ThrowsKeyNotFoundException()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig>());
			Assert.Throws<KeyNotFoundException>(() => _provider.GetConfig<MockCollectionConfig>(99));
		}

		[Test]
		public void GetConfig_SingletonOnCollection_ThrowsInvalidOperationException()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig> { new MockCollectionConfig { Id = 1 } });
			Assert.Throws<InvalidOperationException>(() => _provider.GetConfig<MockCollectionConfig>());
		}

		[Test]
		public void TryGetConfig_Singleton_Exists_ReturnsTrue()
		{
			_provider.AddSingletonConfig(new MockSingletonConfig());
			Assert.IsTrue(_provider.TryGetConfig<MockSingletonConfig>(out _));
		}

		[Test]
		public void TryGetConfig_Singleton_NotExists_ReturnsFalse()
		{
			Assert.IsFalse(_provider.TryGetConfig<MockSingletonConfig>(out _));
		}

		[Test]
		public void TryGetConfig_ById_Exists_ReturnsTrue()
		{
			_provider.AddConfigs(c => c.Id, new List<MockCollectionConfig> { new MockCollectionConfig { Id = 1 } });
			Assert.IsTrue(_provider.TryGetConfig<MockCollectionConfig>(1, out _));
		}

		[Test]
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
		public void GetConfigsDictionary_TypeNotAdded_ThrowsKeyNotFoundException()
		{
			Assert.Throws<KeyNotFoundException>(() => _provider.GetConfigsDictionary<MockSingletonConfig>());
		}

		[Test]
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
