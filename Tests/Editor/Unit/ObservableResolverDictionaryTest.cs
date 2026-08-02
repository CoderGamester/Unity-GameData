using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ObservableResolverDictionaryTest
	{
		private int _key = 0;
		private string _value = "1";
		private ObservableResolverDictionary<int, int, int, string> _dictionary;
		private IDictionary<int, string> _originDictionary;

		[SetUp]
		public void Init()
		{
			// Use a real dictionary that's iterable by the constructor
			_originDictionary = new Dictionary<int, string> { { _key, _value } };
			_dictionary = new ObservableResolverDictionary<int, int, int, string>(
				_originDictionary,
				origin => new KeyValuePair<int, int>(origin.Key, int.Parse(origin.Value)),
				(key, value) => new KeyValuePair<int, string>(key, value.ToString()));
		}

		[Test]
		public void TryGetOriginValue_KeyExists_ReturnsTrueAndOutValue()
		{
			Assert.IsTrue(_dictionary.TryGetOriginValue(_key, out var value));
		}

		[Test]
		public void TryGetOriginValue_KeyDoesNotExist_ReturnsFalseAndOutDefault()
		{
			var result = _dictionary.TryGetOriginValue(999, out var value);

			Assert.IsFalse(result);
			Assert.IsNull(value);
		}

		[Test]
		public void AddOrigin_AddsValueToOriginDictionary()
		{
			var newKey = 99;
			var newValue = "99";
			_dictionary.AddOrigin(newKey, newValue);

			Assert.IsTrue(_originDictionary.ContainsKey(newKey));
			Assert.AreEqual(newValue, _originDictionary[newKey]);
		}

		[Test]
		public void UpdateOrigin_UpdatesValueInOriginDictionary()
		{
			var updatedValue = "42";
			_dictionary.UpdateOrigin(_key, updatedValue);

			Assert.AreEqual(updatedValue, _originDictionary[_key]);
		}

		[Test]
		public void RemoveOrigin_RemovesValueFromOriginDictionary()
		{
			Assert.IsTrue(_dictionary.RemoveOrigin(_key));
			Assert.IsFalse(_originDictionary.ContainsKey(_key));
		}

		[Test]
		// ADMIT: ObservableResolverDictionary<...>.Remove must read the existing value via Dictionary.TryGetValue
		// before resolving `_toOrignResolver`, or it resolves against a default TValue.
		// RCR: ObservableResolverDictionary.cs Remove — delete
		// `if (!Dictionary.TryGetValue(key, out var value)) return false;` → RED (resolves against default,
		// removing the wrong origin key or throwing). 2026-08-01
		public void Remove_WhenKeyExists_RemovesFromOriginDictionaryAndNotifies()
		{
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.UpdateOnly;

			var notified = false;
			_dictionary.Observe((key, prev, curr, type) => notified = true);

			var result = _dictionary.Remove(_key);

			Assert.IsTrue(result);
			Assert.IsFalse(_originDictionary.ContainsKey(_key));
			Assert.IsTrue(notified);
		}

		[Test]
		// ADMIT: Same guard as Remove_WhenKeyExists_RemovesFromOriginDictionaryAndNotifies — the miss branch must
		// leave the origin dictionary untouched.
		// RCR: ObservableResolverDictionary.cs Remove — delete the same TryGetValue guard → RED (a missing key
		// still reaches `_dictionary.Remove(pair.Key)`, changing OriginDictionary.Count). 2026-08-01
		public void Remove_WhenKeyDoesNotExist_ReturnsFalseAndLeavesOriginDictionaryIntact()
		{
			var countBefore = _originDictionary.Count;

			var result = _dictionary.Remove(999);

			Assert.IsFalse(result);
			Assert.AreEqual(countBefore, _originDictionary.Count);
		}

		[Test]
		public void ClearOrigin_ClearsOriginDictionary()
		{
			_dictionary.ClearOrigin();

			Assert.AreEqual(0, _originDictionary.Count);
		}

		[Test]
		public void Rebind_ChangesOriginDictionary()
		{
			// Note: _key already exists in the dictionary from Init(), so we don't add it again

			// Create new dictionary and rebind
			var newDictionary = new Dictionary<int, string> { { 100, "100" }, { 200, "200" } };
			_dictionary.Rebind(
				newDictionary,
				origin => new KeyValuePair<int, int>(origin.Key, int.Parse(origin.Value)),
				(key, value) => new KeyValuePair<int, string>(key, value.ToString()));

			// Verify new dictionary is being used
			Assert.AreEqual(2, _dictionary.Count);
			Assert.IsTrue(_dictionary.ContainsKey(100));
			Assert.IsTrue(_dictionary.ContainsKey(200));
			Assert.AreEqual(100, _dictionary[100]);
			Assert.AreEqual(200, _dictionary[200]);

			// Verify old dictionary is no longer used
			Assert.IsFalse(_dictionary.ContainsKey(_key));
		}

		[Test]
		public void Rebind_KeepsObservers()
		{
			// Setup observer
			var observerCalls = 0;
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.UpdateOnly;
			_dictionary.Observe((key, prev, curr, type) => observerCalls++);

			// Create new dictionary and rebind
			var newDictionary = new Dictionary<int, string> { { 100, "100" } };
			_dictionary.Rebind(
				newDictionary,
				origin => new KeyValuePair<int, int>(origin.Key, int.Parse(origin.Value)),
				(key, value) => new KeyValuePair<int, string>(key, value.ToString()));

			// Trigger update and verify observer is still active
			_dictionary.Add(300, 300);
			Assert.AreEqual(1, observerCalls);
		}
		[Test]
		public void ContainsKey_ReturnsTrue_WhenKeyExists()
		{
			// Key was added in Init() via origin dictionary
			Assert.IsTrue(_dictionary.ContainsKey(_key));
		}

		[Test]
		public void TryGetValue_ReturnsTrue_WhenKeyExists()
		{
			// Key was added in Init() via origin dictionary with value "1"
			Assert.IsTrue(_dictionary.TryGetValue(_key, out var value));
			Assert.AreEqual(1, value); // parsed "1"
		}

		[Test]
		public void Add_InvalidFormat_ThrowsException()
		{
			// Create a dictionary with an invalid format value
			var invalidDictionary = new Dictionary<int, string> { { 99, "invalid" } };
			
			// The FormatException should be thrown during construction when parsing "invalid"
			Assert.Throws<FormatException>(() =>
			{
				_ = new ObservableResolverDictionary<int, int, int, string>(
					invalidDictionary,
					origin => new KeyValuePair<int, int>(origin.Key, int.Parse(origin.Value)),
					(key, value) => new KeyValuePair<int, string>(key, value.ToString()));
			});
		}
	}
}