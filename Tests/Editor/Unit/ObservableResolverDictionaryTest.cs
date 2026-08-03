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
		// ADMIT: ObservableResolverDictionary<...>.TryGetOriginValue must map the resolved key back through
		// `_toOrignResolver` and hit the origin dictionary.
		// RCR: ObservableResolverDictionary.cs TryGetOriginValue — `value = default; return false;` → RED (IsTrue
		// fails). 2026-08-02
		public void TryGetOriginValue_KeyExists_ReturnsTrueAndOutValue()
		{
			Assert.IsTrue(_dictionary.TryGetOriginValue(_key, out var value));
		}

		[Test]
		// ADMIT: ObservableResolverDictionary<...>.TryGetOriginValue must report the origin dictionary's miss rather
		// than a blanket success.
		// RCR: ObservableResolverDictionary.cs TryGetOriginValue — append `|| true` to the return → RED (IsFalse
		// fails). 2026-08-02
		public void TryGetOriginValue_KeyDoesNotExist_ReturnsFalseAndOutDefault()
		{
			var result = _dictionary.TryGetOriginValue(999, out var value);

			Assert.IsFalse(result);
			Assert.IsNull(value);
		}

		[Test]
		// ADMIT: ObservableResolverDictionary<...>.AddOrigin must insert the origin-typed pair into the origin
		// dictionary, not only the resolved pair into the base.
		// RCR: ObservableResolverDictionary.cs AddOrigin — delete `_dictionary.Add(key, value);` → RED
		// (_originDictionary does not contain 99). 2026-08-02
		public void AddOrigin_AddsValueToOriginDictionary()
		{
			var newKey = 99;
			var newValue = "99";
			_dictionary.AddOrigin(newKey, newValue);

			Assert.IsTrue(_originDictionary.ContainsKey(newKey));
			Assert.AreEqual(newValue, _originDictionary[newKey]);
		}

		[Test]
		// ADMIT: ObservableResolverDictionary<...>.UpdateOrigin must write the origin-typed value straight into the
		// origin dictionary alongside the resolved indexer write.
		// RCR: ObservableResolverDictionary.cs UpdateOrigin — delete `_dictionary[key] = value;` → RED
		// (_originDictionary[_key] is still "1", not "42"). 2026-08-02
		public void UpdateOrigin_UpdatesValueInOriginDictionary()
		{
			var updatedValue = "42";
			_dictionary.UpdateOrigin(_key, updatedValue);

			Assert.AreEqual(updatedValue, _originDictionary[_key]);
		}

		[Test]
		// ADMIT: ObservableResolverDictionary<...>.RemoveOrigin must delete the entry from the origin dictionary,
		// not only from the resolved base dictionary.
		// RCR: ObservableResolverDictionary.cs RemoveOrigin — delete `_dictionary.Remove(key);` → RED
		// (_originDictionary still contains the key). 2026-08-02
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
		// ADMIT: ObservableResolverDictionary<...>.ClearOrigin must clear the origin dictionary, not just the
		// resolved base.
		// RCR: ObservableResolverDictionary.cs ClearOrigin — delete `_dictionary.Clear();` → RED
		// (_originDictionary.Count is 1, not 0). 2026-08-02
		public void ClearOrigin_ClearsOriginDictionary()
		{
			_dictionary.ClearOrigin();

			Assert.AreEqual(0, _originDictionary.Count);
		}

		[Test]
		// ADMIT: ObservableResolverDictionary<...>.Rebind must empty the resolved dictionary before rebuilding it
		// from the new origin, or stale keys survive the rebind.
		// RCR: ObservableResolverDictionary.cs Rebind — replace `Dictionary.Clear();` with `_ = Dictionary.Count;` →
		// RED (Count is 3, not 2, and the stale key is still present). 2026-08-02
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
		// ADMIT: ObservableResolverDictionary<...>.Rebind must leave the inherited observer lists untouched, so
		// handlers registered before the rebind still fire.
		// RCR: ObservableResolverDictionary.cs Rebind — add `StopObservingAll();` to the body → RED (observerCalls
		// is 0, not 1, after Add(300, 300)). 2026-08-02
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
		// ADMIT: ObservableResolverDictionary<...>'s constructor must store the *resolved value* produced by
		// `fromOrignResolver`, not just the resolved key.
		// RCR: ObservableResolverDictionary.cs ctor — change `Dictionary.Add(fromOrignResolver(pair));` to
		// `Dictionary.Add(fromOrignResolver(pair).Key, default);` → RED (value is 0, not the parsed 1). 2026-08-02
		public void TryGetValue_ReturnsTrue_WhenKeyExists()
		{
			// Key was added in Init() via origin dictionary with value "1"
			Assert.IsTrue(_dictionary.TryGetValue(_key, out var value));
			Assert.AreEqual(1, value); // parsed "1"
		}

		[Test]
		// ADMIT: ObservableResolverDictionary<...>'s constructor resolves every origin pair eagerly and must let a
		// resolver failure escape rather than skipping the bad row.
		// RCR: ObservableResolverDictionary.cs ctor — wrap the `Dictionary.Add(fromOrignResolver(pair));` call in
		// `try { ... } catch (FormatException) { }` → RED (no FormatException reaches the caller). 2026-08-02
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