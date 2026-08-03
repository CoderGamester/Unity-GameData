using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NSubstitute;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ObservableDictionaryTest
	{
		private const int _key = 0;

		/// <summary>
		/// Mocking interface to check method calls received
		/// </summary>
		public interface IMockCaller<in TKey, in TValue>
		{
			void Call(TKey key, TValue previousValue, TValue newValue, ObservableUpdateType updateType);
		}

		private ObservableDictionary<int, int> _dictionary;
		private IDictionary<int, int> _mockDictionary;
		private IMockCaller<int, int> _caller;

		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller<int, int>>();
			_mockDictionary = new Dictionary<int, int>();
			_dictionary = new ObservableDictionary<int, int>(_mockDictionary);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.TryGetValue must report the backing dictionary's miss, not a
		// blanket success.
		// RCR: ObservableDictionary.cs TryGetValue — replace the delegation with `value = default; return true;` →
		// RED (IsFalse(result) fails). Also reddens TryGetValue_ReturnsTrue_WhenKeyExists. 2026-08-02
		public void TryGetValue_ReturnsFalse_WhenKeyDoesNotExist()
		{
			bool result = _dictionary.TryGetValue(1, out int value);

			Assert.IsFalse(result);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.TryGetValue must hand back the stored value through `out`, not
		// just the hit/miss flag.
		// RCR: ObservableDictionary.cs TryGetValue — keep the return but reset `value = default` before returning →
		// RED (AreEqual(100, value) sees 0). 2026-08-02
		public void TryGetValue_ReturnsTrue_WhenKeyExists()
		{
			_dictionary.Add(1, 100);

			bool result = _dictionary.TryGetValue(1, out int value);

			Assert.IsTrue(result);
			Assert.AreEqual(100, value);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.ContainsKey must delegate to the backing dictionary for the miss
		// case.
		// RCR: ObservableDictionary.cs ContainsKey — `return true;` → RED (IsFalse fails). Also reddens
		// RebindCheck_BaseClass, which asserts the stale keys are gone. 2026-08-02
		public void ContainsKey_ReturnsFalse_WhenKeyDoesNotExist()
		{
			Assert.IsFalse(_dictionary.ContainsKey(1));
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.ContainsKey must delegate to the backing dictionary for the hit
		// case.
		// RCR: ObservableDictionary.cs ContainsKey — `return false;` → RED (IsTrue fails). Also reddens
		// RebindCheck_BaseClass, which asserts the rebound keys are present. 2026-08-02
		public void ContainsKey_ReturnsTrue_WhenKeyExists()
		{
			_dictionary.Add(1, 100);

			Assert.IsTrue(_dictionary.ContainsKey(1));
		}

		[Test]
		public void Indexer_ReturnsValue_WhenKeyExists()
		{
			_dictionary.Add(1, 100);

			Assert.AreEqual(100, _dictionary[1]);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>'s indexer setter must write the incoming value into the backing
		// dictionary before notifying.
		// RCR: ObservableDictionary.cs this[TKey].set — change `Dictionary[key] = value;` to `= previousValue;` →
		// RED (reads back 100 instead of 200). Also reddens BeginBatch_MultipleOperations. 2026-08-02
		public void Indexer_SetsValue_WhenKeyExists()
		{
			_dictionary.Add(1, 100);
			_dictionary[1] = 200;

			Assert.AreEqual(200, _dictionary[1]);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.Add must store the supplied value, not just the key.
		// RCR: ObservableDictionary.cs Add — change `Dictionary.Add(key, value);` to `Dictionary.Add(key, default);`
		// → RED (reads back 0 instead of 100). Also reddens TryGetValue_ReturnsTrue and
		// Clear_NotifiesRemovedForEachKey, which read stored values. 2026-08-02
		public void Add_AddsKeyValuePair_WhenKeyDoesNotExist()
		{
			_dictionary.Add(1, 100);

			Assert.AreEqual(100, _dictionary[1]);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.Add uses Dictionary.Add (not the indexer), so a duplicate id is a
		// hard error rather than a silent overwrite — the package's documented one-role-per-key rule.
		// RCR: ObservableDictionary.cs Add — change `Dictionary.Add(key, value);` to `Dictionary[key] = value;` →
		// RED (no ArgumentException is thrown). 2026-08-02
		public void Add_ThrowsException_WhenKeyAlreadyExists()
		{
			_dictionary.Add(1, 100);

			Assert.Throws<ArgumentException>(() => _dictionary.Add(1, 200));
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.Remove must report success after the entry is gone.
		// RCR: ObservableDictionary.cs Remove — change the trailing `return true;` (after the dependency loop) to
		// `return false;` → RED (IsTrue(Remove(1)) fails). 2026-08-02
		public void Remove_RemovesKeyValuePair_WhenKeyExists()
		{
			_dictionary.Add(1, 100);

			Assert.IsTrue(_dictionary.Remove(1));
			Assert.AreEqual(0, _dictionary.Count);
		}

		[Test]
		public void Remove_ReturnsFalse_WhenKeyDoesNotExist()
		{
			Assert.IsFalse(_dictionary.Remove(1));
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.Remove must short-circuit on the TryGetValue/Remove guard
		// before reaching the notify blocks when the key is missing.
		// RCR: ObservableDictionary.cs Remove — delete the early return in the TryGetValue/Remove guard → RED
		// (the observer gets a Removed callback for a never-added key, then throws on `Dictionary[key]`). 2026-08-01
		public void Remove_WhenKeyDoesNotExist_DoesNotNotifyObservers()
		{
			// Both, not the default KeyUpdateOnly, so a broken guard would actually reach the global notify block.
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.Both;
			_dictionary.Observe(_caller.Call);

			var result = _dictionary.Remove(1);

			Assert.IsFalse(result);
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.Clear must empty the backing dictionary after notifying, not only
		// fire the Removed callbacks.
		// RCR: ObservableDictionary.cs Clear — replace `Dictionary.Clear();` with `_ = Dictionary.Count;` → RED
		// (Count is 2, not 0). Also reddens Clear_NotifiesRemovedForEachKey's Count assertion. 2026-08-02
		public void Clear_RemovesAllKeyValuePairs()
		{
			_dictionary.Add(1, 100);
			_dictionary.Add(2, 200);
			_dictionary.Clear();

			Assert.AreEqual(0, _dictionary.Count);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>'s constructor adopts the caller's dictionary by reference — it
		// must not copy, or writes never reach the caller's instance.
		// RCR: ObservableDictionary.cs ctor — change `Dictionary = dictionary;` to
		// `= new Dictionary<TKey, TValue>(dictionary);` → RED (_mockDictionary[_key] stays 5). 2026-08-02
		public void ValueSetCheck()
		{
			const int valueCheck1 = 5;
			const int valueCheck2 = 6;

			_mockDictionary.Add(_key, valueCheck1);
			_dictionary[_key] = valueCheck2;

			Assert.AreNotEqual(valueCheck1, _mockDictionary[_key]);
			Assert.AreEqual(valueCheck2, _dictionary[_key]);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.Remove must pass the removed value as the *previous* argument to
		// key observers.
		// RCR: ObservableDictionary.cs Remove — in the `_keyUpdateActions` loop change
		// `action(key, value, default, Removed)` to `action(key, default, default, Removed)` → RED (the expected
		// Call(_key, newValue, 0, Removed) is never received). 2026-08-02
		public void ObserveCheck()
		{
			var startValue = 0;
			var newValue = 1;

			_dictionary.Observe(_key, _caller.Call);
			_dictionary.Observe(_caller.Call);

			_dictionary.Add(_key, startValue);

			_dictionary[_key] = newValue;

			_dictionary.Remove(_key);

			_caller.Received().Call(_key, 0, startValue, ObservableUpdateType.Added);
			_caller.Received().Call(_key, startValue, newValue, ObservableUpdateType.Updated);
			_caller.Received().Call(_key, newValue, 0, ObservableUpdateType.Removed);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.InvokeObserve must register the handler BEFORE invoking, or the
		// caller misses its own priming callback.
		// RCR: ObservableDictionary.cs InvokeObserve — swap to `InvokeUpdate(key); Observe(key, onUpdate);` → RED
		// (no Updated call is received). 2026-08-02
		public void InvokeObserveCheck()
		{
			_dictionary.Add(_key, 0);
			_dictionary.InvokeObserve(_key, _caller.Call);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Added);
			_caller.Received().Call(_key, 0, 0, ObservableUpdateType.Updated);
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Removed);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.InvokeUpdate on an unknown key must surface
		// KeyNotFoundException rather than silently no-op.
		// RCR: none exists — the key is read twice, by `InvokeUpdate(key, Dictionary[key])` and again by
		// `var value = Dictionary[key];` inside the protected overload; guarding either leaves the other throwing
		// (verified). Double-covered, not single-line falsifiable. 2026-08-02
		public void InvokeUpdate_MissingKey_ThrowsException()
		{
			Assert.Throws<KeyNotFoundException>(() => _dictionary.InvokeUpdate(_key));
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.InvokeObserve on an unknown key must throw rather than register
		// an observer that can never fire.
		// RCR: none exists — same double read as InvokeUpdate_MissingKey_ThrowsException (`InvokeUpdate(TKey)` and
		// the protected `InvokeUpdate(TKey,TValue)` both index `Dictionary[key]`); guarding either leaves the other
		// throwing (verified). Double-covered, not single-line falsifiable. 2026-08-02
		public void InvokeObserve_MissingKey_ThrowsException()
		{
			Assert.Throws<KeyNotFoundException>(() => _dictionary.InvokeObserve(_key, _caller.Call));
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.InvokeUpdate must report ObservableUpdateType.Updated to key
		// observers — Added/Removed are reserved for real membership changes.
		// RCR: ObservableDictionary.cs InvokeUpdate(TKey,TValue) — change the key-loop's `Updated` to `Added` → RED
		// (DidNotReceive(...Added) fails). Also reddens InvokeObserveCheck. 2026-08-02
		public void InvokeUpdateCheck()
		{
			_dictionary.Add(_key, 0);
			_dictionary.Observe(_key, _caller.Call);
			_dictionary.Observe(_caller.Call);

			_dictionary.InvokeUpdate(_key);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Added);
			_caller.Received().Call(_key, 0, 0, ObservableUpdateType.Updated);
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeUpdate_NotObserving_DoesNothing()
		{
			_dictionary.Add(_key, 0);
			_dictionary.InvokeUpdate(_key);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObserveCheck()
		{
			_dictionary.Observe(_caller.Call);
			_dictionary.StopObserving(_caller.Call);

			_dictionary.Add(_key, 0);
			_dictionary[_key] = 0;
			_dictionary.Remove(_key);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.StopObserving removes only the FIRST matching global observer
		// instance, mirroring the ObservableList 0.6.5 fix.
		// RCR: ObservableDictionary.cs StopObserving — delete the `break;` after `_updateActions.RemoveAt(i);` →
		// RED (both subscribed instances are removed, so the caller receives zero calls instead of one). 2026-08-01
		public void StopObserve_WhenCalledOnce_RemovesOnlyOneObserverInstance()
		{
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.Both;
			_dictionary.Observe(_caller.Call);
			_dictionary.Observe(_caller.Call);
			_dictionary.StopObserving(_caller.Call);

			_dictionary.Add(_key, 0);

			_caller.Received(1).Call(_key, 0, 0, ObservableUpdateType.Added);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.Clear notifies global observers from a `_updateActions.ToList()`
		// snapshot, so an observer that unsubscribes itself mid-notification cannot truncate the loop for the rest.
		// RCR: ObservableDictionary.cs Clear — change `var listCopy = _updateActions.ToList();` to
		// `= _updateActions;` → RED (observer B is never invoked: the live Count shrinks below B's index). 2026-08-01
		public void Observe_WhenObserverUnsubscribesItselfDuringNotification_DoesNotThrowOrSkipOtherObservers()
		{
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.Both;
			_dictionary.Add(_key, 0);

			var bInvoked = false;
			Action<int, int, int, ObservableUpdateType> observerA = null;
			observerA = (key, prev, next, type) => _dictionary.StopObserving(observerA);
			void ObserverB(int key, int prev, int next, ObservableUpdateType type) => bInvoked = true;

			_dictionary.Observe(observerA);
			_dictionary.Observe(ObserverB);

			Assert.DoesNotThrow(() => _dictionary.Clear());
			Assert.IsTrue(bInvoked);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.StopObserving(TKey) must drop the whole per-key handler list.
		// RCR: ObservableDictionary.cs StopObserving(TKey) — replace `_keyUpdateActions.Remove(key);` with a
		// read-only ContainsKey → RED (the key observer still receives Added/Updated/Removed). 2026-08-02
		public void StopObserve_KeyCheck()
		{
			_dictionary.Observe(_key, _caller.Call);
			_dictionary.StopObserving(_key);

			_dictionary.Add(_key, 0);
			_dictionary[_key] = 0;
			_dictionary.Remove(_key);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAllCheck()
		{
			_dictionary.Observe(_caller.Call);
			_dictionary.StopObservingAll(_caller);

			_dictionary.Add(_key, 0);
			_dictionary[_key] = 0;
			_dictionary.Remove(_key);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_MultipleCalls_Check()
		{
			_dictionary.Observe(_caller.Call);
			_dictionary.Observe(_caller.Call);
			_dictionary.StopObservingAll(_caller);

			_dictionary.Add(_key, 0);
			_dictionary[_key] = 0;
			_dictionary.Remove(_key);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_Everything_Check()
		{
			_dictionary.Observe(_caller.Call);
			_dictionary.StopObservingAll();

			_dictionary.Add(_key, 0);
			_dictionary[_key] = 0;
			_dictionary.Remove(_key);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_NotObserving_DoesNothing()
		{
			_dictionary.StopObservingAll(_caller);

			_dictionary.Add(_key, 0);
			_dictionary[_key] = 0;
			_dictionary.Remove(_key);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: under ObservableUpdateFlag.KeyUpdateOnly, ObservableDictionary<TKey,TValue>.Add must skip the
		// global `_updateActions` fan-out entirely.
		// RCR: ObservableDictionary.cs Add — change the global guard `!= KeyUpdateOnly` to `true` → RED
		// (Received(1) sees 2 calls). 2026-08-02
		public void ObservableUpdateFlag_KeyUpdateOnly_OnlyKeyObserversNotified()
		{
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.KeyUpdateOnly;
			_dictionary.Observe(1, _caller.Call);
			_dictionary.Observe(_caller.Call);

			_dictionary.Add(1, 100);

			_caller.Received(1).Call(1, 0, 100, ObservableUpdateType.Added);
		}

		[Test]
		// ADMIT: under ObservableUpdateFlag.UpdateOnly, ObservableDictionary<TKey,TValue>.Add must skip the per-key
		// `_keyUpdateActions` fan-out entirely.
		// RCR: ObservableDictionary.cs Add — drop the `!= UpdateOnly` flag test from the key guard, leaving only
		// `_keyUpdateActions.TryGetValue(...)` → RED (Received(1) sees 2 calls). 2026-08-02
		public void ObservableUpdateFlag_UpdateOnly_OnlyGlobalObserversNotified()
		{
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.UpdateOnly;
			_dictionary.Observe(1, _caller.Call);
			_dictionary.Observe(_caller.Call);

			_dictionary.Add(1, 100);

			_caller.Received(1).Call(1, 0, 100, ObservableUpdateType.Added);
			// Global observer receives it, key observer does not
		}

		[Test]
		// ADMIT: under ObservableUpdateFlag.Both, ObservableDictionary<TKey,TValue>.Add must run BOTH fan-outs —
		// the global guard is `!= KeyUpdateOnly`, not `== UpdateOnly`.
		// RCR: ObservableDictionary.cs Add — change the global guard to `== UpdateOnly` → RED (Received(2) sees
		// only the key observer's 1 call). 2026-08-02
		public void ObservableUpdateFlag_Both_AllObserversNotified()
		{
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.Both;
			_dictionary.Observe(1, _caller.Call);
			_dictionary.Observe(_caller.Call);

			_dictionary.Add(1, 100);

			_caller.Received(2).Call(1, 0, 100, ObservableUpdateType.Added);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>'s IBatchable.SuppressNotifications must set `_isBatching` so
		// mutations inside the batch are silent and replayed as Updated on resume.
		// RCR: ObservableDictionary.cs IBatchable.SuppressNotifications — change `_isBatching = true;` to `false` →
		// RED (the live Call(1,100,150,Updated) replaces the expected Call(1,0,150,Updated)). 2026-08-02
		public void BeginBatch_MultipleOperations_SingleNotification()
		{
			_dictionary.Add(1, 100);
			// Enable global observers (default is KeyUpdateOnly which skips global observers)
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.Both;
			_dictionary.Observe(_caller.Call);

			using (_dictionary.BeginBatch())
			{
				_dictionary.Add(2, 200);
				_dictionary[1] = 150;
			}

			// Current implementation notifies for ALL items in dictionary on ResumeNotifications
			_caller.Received(1).Call(1, 0, 150, ObservableUpdateType.Updated);
			_caller.Received(1).Call(2, 0, 200, ObservableUpdateType.Updated);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.Clear must pass each entry's value as the *previous* argument to
		// global observers before wiping the dictionary.
		// RCR: ObservableDictionary.cs Clear — in the global loop change `listCopy[i](data.Key, data.Value, ...)` to
		// `(data.Key, default, ...)` → RED (Call(1,100,0,Removed) is never received). 2026-08-02
		public void Clear_NotifiesRemovedForEachKey()
		{
			_dictionary.Add(1, 100);
			_dictionary.Add(2, 200);
			// Enable global observers (default is KeyUpdateOnly which skips global observers)
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.Both;
			_dictionary.Observe(_caller.Call);

			_dictionary.Clear();

			_caller.Received().Call(1, 100, 0, ObservableUpdateType.Removed);
			_caller.Received().Call(2, 200, 0, ObservableUpdateType.Removed);
			Assert.AreEqual(0, _dictionary.Count);
		}

		[Test]
		// ADMIT: ObservableDictionary<TKey,TValue>.Rebind must swap the backing dictionary while keeping the
		// registered key observers alive.
		// RCR: ObservableDictionary.cs Rebind — empty the body (drop `Dictionary = dictionary;`) → RED (Count is 2,
		// not 3, and the new keys are absent). 2026-08-02
		public void RebindCheck_BaseClass()
		{
			// Add initial data
			_dictionary.Add(1, 100);
			_dictionary.Add(2, 200);

			// Setup key-specific observer (this works with default KeyUpdateOnly flag)
			_dictionary.Observe(40, _caller.Call);

			// Create new dictionary and rebind
			var newDictionary = new Dictionary<int, int> { { 10, 1000 }, { 20, 2000 }, { 30, 3000 } };
			_dictionary.Rebind(newDictionary);

			// Verify new dictionary is being used
			Assert.AreEqual(3, _dictionary.Count);
			Assert.IsTrue(_dictionary.ContainsKey(10));
			Assert.IsTrue(_dictionary.ContainsKey(20));
			Assert.IsTrue(_dictionary.ContainsKey(30));
			Assert.AreEqual(1000, _dictionary[10]);
			Assert.AreEqual(2000, _dictionary[20]);
			Assert.AreEqual(3000, _dictionary[30]);

			// Verify old keys are no longer present
			Assert.IsFalse(_dictionary.ContainsKey(1));
			Assert.IsFalse(_dictionary.ContainsKey(2));

			// Verify observer still works after rebind
			_dictionary.Add(40, 4000);
			_caller.Received(1).Call(40, 0, 4000, ObservableUpdateType.Added);
		}
	}
}