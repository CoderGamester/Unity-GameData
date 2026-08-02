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
		public void TryGetValue_ReturnsFalse_WhenKeyDoesNotExist()
		{
			bool result = _dictionary.TryGetValue(1, out int value);

			Assert.IsFalse(result);
		}

		[Test]
		public void TryGetValue_ReturnsTrue_WhenKeyExists()
		{
			_dictionary.Add(1, 100);

			bool result = _dictionary.TryGetValue(1, out int value);

			Assert.IsTrue(result);
			Assert.AreEqual(100, value);
		}

		[Test]
		public void ContainsKey_ReturnsFalse_WhenKeyDoesNotExist()
		{
			Assert.IsFalse(_dictionary.ContainsKey(1));
		}

		[Test]
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
		public void Indexer_SetsValue_WhenKeyExists()
		{
			_dictionary.Add(1, 100);
			_dictionary[1] = 200;

			Assert.AreEqual(200, _dictionary[1]);
		}

		[Test]
		public void Add_AddsKeyValuePair_WhenKeyDoesNotExist()
		{
			_dictionary.Add(1, 100);

			Assert.AreEqual(100, _dictionary[1]);
		}

		[Test]
		public void Add_ThrowsException_WhenKeyAlreadyExists()
		{
			_dictionary.Add(1, 100);

			Assert.Throws<ArgumentException>(() => _dictionary.Add(1, 200));
		}

		[Test]
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
		public void Clear_RemovesAllKeyValuePairs()
		{
			_dictionary.Add(1, 100);
			_dictionary.Add(2, 200);
			_dictionary.Clear();

			Assert.AreEqual(0, _dictionary.Count);
		}

		[Test]
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
		public void InvokeObserveCheck()
		{
			_dictionary.Add(_key, 0);
			_dictionary.InvokeObserve(_key, _caller.Call);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Added);
			_caller.Received().Call(_key, 0, 0, ObservableUpdateType.Updated);
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeUpdate_MissingKey_ThrowsException()
		{
			Assert.Throws<KeyNotFoundException>(() => _dictionary.InvokeUpdate(_key));
		}

		[Test]
		public void InvokeObserve_MissingKey_ThrowsException()
		{
			Assert.Throws<KeyNotFoundException>(() => _dictionary.InvokeObserve(_key, _caller.Call));
		}

		[Test]
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
		public void ObservableUpdateFlag_KeyUpdateOnly_OnlyKeyObserversNotified()
		{
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.KeyUpdateOnly;
			_dictionary.Observe(1, _caller.Call);
			_dictionary.Observe(_caller.Call);

			_dictionary.Add(1, 100);

			_caller.Received(1).Call(1, 0, 100, ObservableUpdateType.Added);
		}

		[Test]
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
		public void ObservableUpdateFlag_Both_AllObserversNotified()
		{
			_dictionary.ObservableUpdateFlag = ObservableUpdateFlag.Both;
			_dictionary.Observe(1, _caller.Call);
			_dictionary.Observe(_caller.Call);

			_dictionary.Add(1, 100);

			_caller.Received(2).Call(1, 0, 100, ObservableUpdateType.Added);
		}

		[Test]
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