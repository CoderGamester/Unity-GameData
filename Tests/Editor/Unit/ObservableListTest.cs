using System.Collections.Generic;
using GameLovers.GameData;
using NSubstitute;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ObservableListTest
	{
		/// <summary>
		/// Mocking interface to check method calls received
		/// </summary>
		public interface IMockCaller<in T>
		{
			void Call(int index, T value, T valueChange, ObservableUpdateType updateType);
		}

		private const int _index = 0;
		private const int _previousValue = 5;
		private const int _newValue = 10;

		private ObservableList<int> _list;
		private IList<int> _mockList;
		private IMockCaller<int> _caller;

		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller<int>>();
			_mockList = Substitute.For<IList<int>>();
			_list = new ObservableList<int>(_mockList);
		}

		[Test]
		public void AddValue_AddsValueToList()
		{
			_list.Add(_previousValue);

			Assert.AreEqual(_previousValue, _list[_index]);
		}

		[Test]
		// ADMIT: ObservableList<T>'s indexer setter must write the incoming value into the backing list before
		// notifying observers.
		// RCR: ObservableList.cs this[int].set — change `List[index] = value;` to `= previousValue;` → RED (reads
		// back 5 instead of 6). Also reddens ObserveCheck and BeginBatch_MultipleOperations. 2026-08-02
		public void SetValue_UpdatesValue()
		{
			const int valueCheck1 = 5;
			const int valueCheck2 = 6;

			_list.Add(valueCheck1);

			Assert.AreEqual(valueCheck1, _list[_index]);

			_list[_index] = valueCheck2;

			Assert.AreEqual(valueCheck2, _list[_index]);
		}

		[Test]
		// ADMIT: ObservableList<T>.RemoveAt must pass the removed element as the *previous* argument of the Removed
		// callback.
		// RCR: ObservableList.cs RemoveAt — change `action(index, data, default, Removed)` to
		// `action(index, default, default, Removed)` → RED (Call(_index,_newValue,0,Removed) never received). Also
		// reddens StopObserve_WhenCalledOnce_RemovesOnlyOneObserverInstance. 2026-08-02
		public void ObserveCheck()
		{
			_list.Observe(_caller.Call);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());

			_list.Add(_previousValue);

			_list[_index] = _newValue;

			_list.RemoveAt(_index);

			_caller.Received().Call(Arg.Any<int>(), Arg.Is(0), Arg.Is(_previousValue), ObservableUpdateType.Added);
			_caller.Received().Call(_index, _previousValue, _newValue, ObservableUpdateType.Updated);
			_caller.Received().Call(_index, _newValue, 0, ObservableUpdateType.Removed);
		}

		[Test]
		// ADMIT: ObservableList<T>.InvokeObserve must register the handler BEFORE invoking, or the caller misses its
		// own priming callback.
		// RCR: ObservableList.cs InvokeObserve — swap to `InvokeUpdate(index); Observe(onUpdate);` → RED (the
		// expected Updated call is never received). 2026-08-02
		public void InvokeObserveCheck()
		{
			_list.Add(_previousValue);

			_list.InvokeObserve(_index, _caller.Call);

			_caller.DidNotReceive().Call(_index, _previousValue, _previousValue, ObservableUpdateType.Added);
			_caller.Received().Call(_index, _previousValue, _previousValue, ObservableUpdateType.Updated);
			_caller.DidNotReceive().Call(_index, _previousValue, _previousValue, ObservableUpdateType.Removed);
		}

		[Test]
		// ADMIT: ObservableList<T>.InvokeUpdate(int) must pass the element's current value as the previous value, so
		// a manual refresh reports (value, value).
		// RCR: ObservableList.cs InvokeUpdate(int) — change `InvokeUpdate(index, List[index]);` to
		// `InvokeUpdate(index, default);` → RED (previous is 0, not 5). Also reddens InvokeObserveCheck. 2026-08-02
		public void InvokeCheck()
		{
			_list.Add(_previousValue);
			_list.Observe(_caller.Call);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());

			_list.InvokeUpdate(_index);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Added);
			_caller.Received().Call(_index, _previousValue, _previousValue, ObservableUpdateType.Updated);
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeCheck_NotObserving_DoesNothing()
		{
			_list.Add(_previousValue);
			_list.InvokeUpdate(_index);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: ObservableList<T>.StopObserving must actually detach the delegate from `_updateActions`.
		// RCR: ObservableList.cs StopObserving — empty the body (drop `_updateActions.Remove(onUpdate);`) → RED (the
		// caller receives Added/Updated/Removed). Also reddens
		// StopObserve_WhenCalledOnce_RemovesOnlyOneObserverInstance. 2026-08-02
		public void StopObserveCheck()
		{
			_list.Observe(_caller.Call);
			_list.StopObserving(_caller.Call);
			_list.Add(_previousValue);

			_list[_index] = _previousValue;

			_list.RemoveAt(_index);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: ObservableList<T>.StopObserving uses List.Remove, which drops exactly ONE registration — a handler
		// subscribed twice must survive a single StopObserving (the 0.6.5 contract).
		// RCR: ObservableList.cs StopObserving — change to `while (_updateActions.Remove(onUpdate)) { }` → RED (both
		// registrations vanish, so Received(1) sees zero calls). 2026-08-02
		public void StopObserve_WhenCalledOnce_RemovesOnlyOneObserverInstance()
		{
			_list.Observe(_caller.Call);
			_list.Observe(_caller.Call);
			_list.StopObserving(_caller.Call);
			_list.Add(_previousValue);

			_list[_index] = _previousValue;

			_list.RemoveAt(_index);

			_caller.Received(1).Call(Arg.Any<int>(), Arg.Is(0), Arg.Is(_previousValue), ObservableUpdateType.Added);
			_caller.Received(1).Call(_index, _previousValue, _previousValue, ObservableUpdateType.Updated);
			_caller.Received(1).Call(_index, _previousValue, 0, ObservableUpdateType.Removed);
		}

		[Test]
		// ADMIT: ObservableList<T>.RemoveAt notifies via a backward loop whose start index is captured once, so an
		// observer that subscribes a new observer mid-notification cannot make that new observer fire for this call.
		// RCR: ObservableList.cs RemoveAt — change the backward loop to
		// `for (var i = 0; i < _updateActions.Count; i++)` (live bound) → RED (the newly-appended observer C fires
		// for the same RemoveAt that added it). 2026-08-01
		public void Observe_WhenObserverAddsAnotherObserverDuringNotification_NewObserverNotInvokedForCurrentUpdate()
		{
			_list.Add(_previousValue);

			var cCallCount = 0;
			void ObserverC(int index, int prev, int curr, ObservableUpdateType type) => cCallCount++;
			void ObserverA(int index, int prev, int curr, ObservableUpdateType type) => _list.Observe(ObserverC);

			_list.Observe(ObserverA);

			_list.RemoveAt(_index);

			Assert.AreEqual(0, cCallCount);
		}

		[Test]
		// ADMIT: ObservableList<T>.StopObservingAll(subscriber) matches on `Delegate.Target`, detaching the handlers
		// that belong to that object.
		// RCR: ObservableList.cs StopObservingAll — invert the `Target == subscriber` comparison → RED (the observer
		// survives Add/InvokeUpdate). Also reddens StopObservingAll_MultipleCalls_StopsAll. 2026-08-02
		public void StopObservingAllCheck()
		{
			_list.Observe(_caller.Call);
			_list.StopObservingAll(_caller);
			_list.Add(_previousValue);
			_list.InvokeUpdate(_index);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: ObservableList<T>.StopObservingAll must remove EVERY delegate owned by the subscriber, not stop at
		// the first match.
		// RCR: ObservableList.cs StopObservingAll — add `break;` after `_updateActions.RemoveAt(i);` → RED (the
		// surviving duplicate registration receives Added). 2026-08-02
		public void StopObservingAll_MultipleCalls_StopsAll()
		{
			_list.Observe(_caller.Call);
			_list.Observe(_caller.Call);
			_list.StopObservingAll(_caller);
			_list.Add(_previousValue);
			_list.InvokeUpdate(_index);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		// ADMIT: ObservableList<T>.StopObservingAll(null) takes the wholesale-clear branch instead of the
		// per-subscriber scan.
		// RCR: ObservableList.cs StopObservingAll — delete `_updateActions.Clear();` from the `subscriber == null`
		// branch → RED (the observer survives and receives Added). 2026-08-02
		public void StopObservingAll_Everything_Check()
		{
			_list.Observe(_caller.Call);
			_list.StopObservingAll();

			_list.Add(_previousValue);
			_list.InvokeUpdate(_index);

			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}


		[Test]
		// ADMIT: ObservableList<T>.Clear must report each element as the Removed callback's *previous* value before
		// emptying the list.
		// RCR: ObservableList.cs Clear — change `copy[i](j, List[j], default, Removed)` to
		// `copy[i](j, default, default, Removed)` → RED (Call(0,1,0,Removed) is never received). 2026-08-02
		public void Clear_NotifiesForEachItem()
		{
			_list.Add(1);
			_list.Add(2);
			_list.Observe(_caller.Call);

			_list.Clear();

			_caller.Received().Call(0, 1, 0, ObservableUpdateType.Removed);
			_caller.Received().Call(1, 2, 0, ObservableUpdateType.Removed);
			Assert.AreEqual(0, _list.Count);
		}

		[Test]
		// ADMIT: ObservableList<T>.Contains must delegate to the backing list rather than answering unconditionally.
		// RCR: ObservableList.cs Contains — `return true;` → RED (IsFalse(Contains(20)) fails). 2026-08-02
		public void Contains_ReturnsCorrect()
		{
			_list.Add(10);
			Assert.IsTrue(_list.Contains(10));
			Assert.IsFalse(_list.Contains(20));
		}

		[Test]
		// ADMIT: ObservableList<T>.IndexOf must return the backing list's position, not a constant.
		// RCR: ObservableList.cs IndexOf — `return -1;` → RED (AreEqual(0, IndexOf(10)) fails). 2026-08-02
		public void IndexOf_ReturnsCorrectIndex()
		{
			_list.Add(10);
			_list.Add(20);
			Assert.AreEqual(0, _list.IndexOf(10));
			Assert.AreEqual(1, _list.IndexOf(20));
			Assert.AreEqual(-1, _list.IndexOf(30));
		}

		[Test]
		// ADMIT: ObservableList<T>'s IBatchable.SuppressNotifications must set `_isBatching`, so mutations inside a
		// batch are silent and replayed once per element as Updated on resume.
		// RCR: ObservableList.cs IBatchable.SuppressNotifications — change `_isBatching = true;` to `false` → RED
		// (the live Call(0,1,3,Updated) replaces the expected Call(0,0,3,Updated)). 2026-08-02
		public void BeginBatch_MultipleOperations_SingleNotification()
		{
			_list.Add(1);
			_list.Observe(_caller.Call);

			using (_list.BeginBatch())
			{
				_list.Add(2);
				_list[0] = 3;
			}

			// In current implementation, BeginBatch notifies for ALL items in the list at the end
			_caller.Received(1).Call(0, 0, 3, ObservableUpdateType.Updated);
			_caller.Received(1).Call(1, 0, 2, ObservableUpdateType.Updated);
		}

		[Test]
		// ADMIT: ObservableList<T>.Rebind must swap the backing list while keeping observers registered.
		// RCR: ObservableList.cs Rebind — empty the body (drop `List = list as List<T> ?? list.ToList();`) → RED
		// (Count is 2, not 3). 2026-08-02
		public void RebindCheck_BaseClass()
		{
			// Add initial data
			_list.Add(_previousValue);
			_list.Add(_newValue);

			// Setup observer
			_list.Observe(_caller.Call);

			// Create new list and rebind
			var newList = new List<int> { 100, 200, 300 };
			_list.Rebind(newList);

			// Verify new list is being used
			Assert.AreEqual(3, _list.Count);
			Assert.AreEqual(100, _list[0]);
			Assert.AreEqual(200, _list[1]);
			Assert.AreEqual(300, _list[2]);

			// Verify observer still works
			_list.Add(400);
			_caller.Received(1).Call(Arg.Any<int>(), Arg.Is(0), Arg.Is(400), ObservableUpdateType.Added);
		}
	}
}