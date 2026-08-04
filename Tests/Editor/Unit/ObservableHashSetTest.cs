using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NSubstitute;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ObservableHashSetTest
	{
		private ObservableHashSet<int> _set;
		private Action<int, ObservableUpdateType> _mockObserver;

		[SetUp]
		public void Setup()
		{
			_set = new ObservableHashSet<int>();
			_mockObserver = Substitute.For<Action<int, ObservableUpdateType>>();
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Add must report true on the branch where the underlying HashSet accepted the
		// item.
		// RCR: ObservableHashSet.cs Add — change the in-branch `return true;` to `return false;` → RED (IsTrue
		// fails). 2026-08-02
		public void Add_NewItem_ReturnsTrue()
		{
			Assert.IsTrue(_set.Add(1));
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Add must notify with ObservableUpdateType.Added, not another update type.
		// RCR: ObservableHashSet.cs Add — change `InvokeUpdate(item, Added)` to `Updated` → RED (Received(1)(1,
		// Added) sees zero calls). 2026-08-02
		public void Add_NewItem_NotifiesAdded()
		{
			_set.Observe(_mockObserver);
			_set.Add(1);
			_mockObserver.Received(1)(1, ObservableUpdateType.Added);
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Add must report false when the item was already present.
		// RCR: ObservableHashSet.cs Add — change the trailing `return false;` to `return true;` → RED (IsFalse
		// fails). 2026-08-02
		public void Add_ExistingItem_ReturnsFalse()
		{
			_set.Add(1);
			Assert.IsFalse(_set.Add(1));
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Add notifies only inside the `_hashSet.Add(item)` success branch — a duplicate
		// add must stay silent.
		// RCR: ObservableHashSet.cs Add — insert an unconditional `InvokeUpdate(item, Added);` before the trailing
		// `return false;` → RED (DidNotReceive fails). 2026-08-02
		public void Add_ExistingItem_NoNotification()
		{
			_set.Add(1);
			_set.Observe(_mockObserver);
			_set.Add(1);
			_mockObserver.DidNotReceive()(Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Remove must report true on the branch where the underlying HashSet removed the
		// item.
		// RCR: ObservableHashSet.cs Remove — change the in-branch `return true;` to `return false;` → RED (IsTrue
		// fails). 2026-08-02
		public void Remove_ExistingItem_ReturnsTrue()
		{
			_set.Add(1);
			Assert.IsTrue(_set.Remove(1));
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Remove must notify with ObservableUpdateType.Removed.
		// RCR: ObservableHashSet.cs Remove — change `InvokeUpdate(item, Removed)` to `Updated` → RED (Received(1)(1,
		// Removed) sees zero calls). 2026-08-02
		public void Remove_ExistingItem_NotifiesRemoved()
		{
			_set.Add(1);
			_set.Observe(_mockObserver);
			_set.Remove(1);
			_mockObserver.Received(1)(1, ObservableUpdateType.Removed);
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Remove must report false when the item was never present.
		// RCR: ObservableHashSet.cs Remove — change the trailing `return false;` to `return true;` → RED (IsFalse
		// fails). 2026-08-02
		public void Remove_MissingItem_ReturnsFalse()
		{
			Assert.IsFalse(_set.Remove(1));
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Remove notifies only inside the `_hashSet.Remove(item)` success branch — a
		// missing item must stay silent.
		// RCR: ObservableHashSet.cs Remove — insert an unconditional `InvokeUpdate(item, Removed);` before the
		// trailing `return false;` → RED (DidNotReceive fails). 2026-08-02
		public void Remove_MissingItem_NoNotification()
		{
			_set.Observe(_mockObserver);
			_set.Remove(1);
			_mockObserver.DidNotReceive()(Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Contains must delegate to the backing HashSet for the hit case.
		// RCR: ObservableHashSet.cs Contains — `return false;` → RED (IsTrue fails). Also reddens
		// Constructor_WithCollection_PopulatesSet and Constructor_WithComparer_UsesComparer. 2026-08-02
		public void Contains_ExistingItem_ReturnsTrue()
		{
			_set.Add(10);
			Assert.IsTrue(_set.Contains(10));
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Contains must delegate to the backing HashSet for the miss case.
		// RCR: ObservableHashSet.cs Contains — `return true;` → RED (IsFalse fails). 2026-08-02
		public void Contains_MissingItem_ReturnsFalse()
		{
			Assert.IsFalse(_set.Contains(10));
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Clear must emit ObservableUpdateType.Removed for every member before wiping
		// the set.
		// RCR: ObservableHashSet.cs Clear — change `action(item, Removed)` to `action(item, Updated)` → RED
		// (Received(1)(1, Removed) sees zero calls). 2026-08-02
		public void Clear_NotifiesRemovedForEachItem()
		{
			_set.Add(1);
			_set.Add(2);
			_set.Observe(_mockObserver);
			_set.Clear();
			_mockObserver.Received(1)(1, ObservableUpdateType.Removed);
			_mockObserver.Received(1)(2, ObservableUpdateType.Removed);
			Assert.AreEqual(0, _set.Count);
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Clear drives its notify loop from the set's members, so an empty set produces
		// no callbacks at all.
		// RCR: ObservableHashSet.cs Clear — change `foreach (var item in _hashSet)` to `_hashSet.DefaultIfEmpty()` →
		// RED (a phantom Removed(0) callback arrives; DidNotReceive fails). 2026-08-02
		public void Clear_EmptySet_NoNotifications()
		{
			_set.Observe(_mockObserver);
			_set.Clear();
			_mockObserver.DidNotReceive()(Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Count must report the backing HashSet's live size.
		// RCR: ObservableHashSet.cs Count.get — `return 0;` → RED (AreEqual(2, Count) fails). Also reddens
		// Count_TracksComputedDependency and Constructor_WithCollection_PopulatesSet. 2026-08-02
		public void Count_ReturnsCorrectValue()
		{
			Assert.AreEqual(0, _set.Count);
			_set.Add(1);
			_set.Add(2);
			Assert.AreEqual(2, _set.Count);
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.Count.get must call ComputedTracker.OnRead so a ComputedField reading Count is
		// invalidated when the set changes.
		// RCR: ObservableHashSet.cs Count.get — delete `ComputedTracker.OnRead(this);` → RED (computed.Value stays
		// cached at 0 after Add). 2026-08-02
		public void Count_TracksComputedDependency()
		{
			var computed = new ComputedField<int>(() => _set.Count);
			Assert.AreEqual(0, computed.Value);

			_set.Add(1);
			Assert.AreEqual(1, computed.Value);
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.StopObserving must actually detach the delegate from `_updateActions`.
		// RCR: ObservableHashSet.cs StopObserving — replace `_updateActions.Remove(onUpdate);` with a read-only
		// Contains → RED (the observer still receives Added). 2026-08-02
		public void StopObserving_StopsNotifications()
		{
			_set.Observe(_mockObserver);
			_set.StopObserving(_mockObserver);
			_set.Add(1);
			_mockObserver.DidNotReceive()(Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.StopObservingAll(null) takes the wholesale-clear branch, dropping every
		// registered observer.
		// RCR: ObservableHashSet.cs StopObservingAll — delete `_updateActions.Clear();` from the
		// `subscriber == null` branch → RED (both observers receive Added). 2026-08-02
		public void StopObservingAll_ClearsAllObservers()
		{
			var observer2 = Substitute.For<Action<int, ObservableUpdateType>>();
			_set.Observe(_mockObserver);
			_set.Observe(observer2);
			_set.StopObservingAll();
			_set.Add(1);
			_mockObserver.DidNotReceive()(Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
			observer2.DidNotReceive()(Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: ObservableHashSet<T>'s IBatchable.ResumeNotifications replays the whole set as Added when the batch
		// closes.
		// RCR: ObservableHashSet.cs IBatchable.ResumeNotifications — change the replayed
		// `InvokeUpdate(item, Added)` to `Updated` → RED (Received(1)(1, Added) sees zero calls). 2026-08-02
		public void BeginBatch_SuppressesNotifications()
		{
			_set.Observe(_mockObserver);
			using (_set.BeginBatch())
			{
				_set.Add(1);
				_set.Add(2);
			}
			// In current implementation, ResumeNotifications notifies for ALL current items as Added
			_mockObserver.Received(1)(1, ObservableUpdateType.Added);
			_mockObserver.Received(1)(2, ObservableUpdateType.Added);
		}

		[Test]
		// ADMIT: ObservableHashSet<T>(IEnumerable<T>) must seed the backing HashSet from the collection.
		// RCR: ObservableHashSet.cs ctor(IEnumerable<T>) — change `new HashSet<T>(collection)` to `new HashSet<T>()`
		// → RED (Count is 0, not 3). 2026-08-02
		public void Constructor_WithCollection_PopulatesSet()
		{
			var list = new List<int> { 1, 2, 3 };
			var set = new ObservableHashSet<int>(list);
			Assert.AreEqual(3, set.Count);
			Assert.IsTrue(set.Contains(1));
			Assert.IsTrue(set.Contains(2));
			Assert.IsTrue(set.Contains(3));
		}

		[Test]
		// ADMIT: ObservableHashSet<T>(IEqualityComparer<T>) must hand the comparer to the backing HashSet, so
		// OrdinalIgnoreCase lookups hit.
		// RCR: ObservableHashSet.cs ctor(IEqualityComparer<T>) — change `new HashSet<T>(comparer)` to
		// `new HashSet<T>()` → RED (Contains("test") is false). 2026-08-02
		public void Constructor_WithComparer_UsesComparer()
		{
			var set = new ObservableHashSet<string>(StringComparer.OrdinalIgnoreCase);
			set.Add("Test");
			Assert.IsTrue(set.Contains("test"));
		}

		[Test]
		// ADMIT: ObservableHashSet<T>.GetEnumerator must expose the backing HashSet's members, not an empty
		// sequence.
		// RCR: ObservableHashSet.cs GetEnumerator — return `new HashSet<T>().GetEnumerator()` → RED (items.Count is
		// 0, not 2). 2026-08-02
		public void Enumerable_Works()
		{
			_set.Add(1);
			_set.Add(2);
			var items = new List<int>();
			foreach (var item in _set)
			{
				items.Add(item);
			}
			Assert.AreEqual(2, items.Count);
			Assert.Contains(1, items);
			Assert.Contains(2, items);
		}
	}
}
