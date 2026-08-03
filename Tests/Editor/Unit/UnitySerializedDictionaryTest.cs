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
		public void Indexer_Set_NewKey_AddsEntry()
		{
			_dictionary["new"] = 500;
			Assert.AreEqual(500, _dictionary["new"]);
		}

		[Test]
		// ADMIT: UnitySerializedDictionary<TKey,TValue>.OnAfterDeserialize folds the backing lists in with the
		// INDEXER, so a duplicate key serialized by Unity resolves last-one-wins instead of throwing.
		// RCR: UnitySerializedDictionary.cs OnAfterDeserialize — guard the assignment with `if (!ContainsKey(...))`
		// → RED (the value stays 1 instead of 2). 2026-08-02
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
		// ADMIT: UnitySerializedDictionary<TKey,TValue>.OnBeforeSerialize must flatten the live dictionary into the
		// `_keyData`/`_valueData` backing lists Unity actually writes to YAML.
		// RCR: UnitySerializedDictionary.cs OnBeforeSerialize — delete `_keyData.Add(item.Key);` → RED
		// (KeyDataInternal.Count is 0, not 2). 2026-08-02
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
