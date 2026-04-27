using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class UnitySerializedDictionaryTest
	{
		[Serializable]
		public class StringIntDictionary : UnitySerializedDictionary<string, int> { }

		private StringIntDictionary _dictionary;

		[SetUp]
		public void Setup()
		{
			_dictionary = new StringIntDictionary();
		}

		[Test]
		public void Add_StoresKeyValue()
		{
			_dictionary.Add("test", 100);
			Assert.AreEqual(100, _dictionary["test"]);
		}

		[Test]
		public void Add_DuplicateKey_ThrowsArgumentException()
		{
			_dictionary.Add("test", 100);
			Assert.Throws<ArgumentException>(() => _dictionary.Add("test", 200));
		}

		[Test]
		public void Remove_ExistingKey_ReturnsTrue()
		{
			_dictionary.Add("test", 100);
			Assert.IsTrue(_dictionary.Remove("test"));
			Assert.AreEqual(0, _dictionary.Count);
		}

		[Test]
		public void TryGetValue_Exists_ReturnsTrue()
		{
			_dictionary.Add("test", 100);
			Assert.IsTrue(_dictionary.TryGetValue("test", out var val));
			Assert.AreEqual(100, val);
		}

		[Test]
		public void TryGetValue_NotExists_ReturnsFalse()
		{
			Assert.IsFalse(_dictionary.TryGetValue("missing", out _));
		}

		[Test]
		public void Indexer_Set_NewKey_AddsEntry()
		{
			_dictionary["new"] = 500;
			Assert.AreEqual(500, _dictionary["new"]);
		}

		[Test]
		public void OnAfterDeserialize_OverwritesDuplicateKeys()
		{
			// Simulate Unity deserialization (the YAML serializer populates the backing lists,
			// then ISerializationCallbackReceiver.OnAfterDeserialize folds them into the dict).
			_dictionary.SetSerializedLists(new List<string> { "key", "key" }, new List<int> { 1, 2 });

			((ISerializationCallbackReceiver)_dictionary).OnAfterDeserialize();

			Assert.AreEqual(1, _dictionary.Count);
			Assert.AreEqual(2, _dictionary["key"]); // Last one wins
		}

		[Test]
		public void OnBeforeSerialize_PopulatesLists()
		{
			_dictionary.Add("key1", 10);
			_dictionary.Add("key2", 20);

			((ISerializationCallbackReceiver)_dictionary).OnBeforeSerialize();

			var keys = _dictionary.KeyDataInternal;
			var values = _dictionary.ValueDataInternal;

			Assert.AreEqual(2, keys.Count);
			Assert.Contains("key1", keys);
			Assert.Contains("key2", keys);
			Assert.AreEqual(2, values.Count);
			Assert.Contains(10, values);
			Assert.Contains(20, values);
		}
	}
}
