using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using GameLovers.GameData;
using NSubstitute;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ObservableResolverListTest
	{
		private int _index = 0;
		private IObservableResolverList<int, string> _list;
		private IList<string> _mockList;

		[SetUp]
		public void SetUp()
		{
			_mockList = Substitute.For<IList<string>>();
			_list = new ObservableResolverList<int, string>(_mockList,
				origin => int.Parse(origin),
				value => value.ToString());
		}

		[Test]
		// ADMIT: ObservableResolverList<T,TOrigin>.AddOrigin must push the origin-typed value into the origin list,
		// not only into the resolved list.
		// RCR: ObservableResolverList.cs AddOrigin — delete `_originList.Add(value);` → RED (the substituted
		// IList<string> never receives Add). 2026-08-02
		public void AddOrigin_AddsValueToOriginList()
		{
			var value = "1";

			_list.AddOrigin(value);

			_mockList.Received().Add(value);
		}

		[Test]
		// ADMIT: ObservableResolverList<T,TOrigin>.UpdateOrigin must write through to the origin list at the same
		// index it updates in the resolved list.
		// RCR: ObservableResolverList.cs UpdateOrigin — delete `_originList[index] = value;` → RED (the substituted
		// IList<string> never receives the indexer set). 2026-08-02
		public void UpdateOrigin_UpdatesOriginList()
		{
			var value = "1";

			_list.AddOrigin(value);
			_list.UpdateOrigin(value, _index);

			_mockList.Received()[_index] = value;
		}

		[Test]
		// ADMIT: ObservableResolverList<T,TOrigin>.RemoveOrigin must remove from the origin list as well as the
		// resolved list.
		// RCR: ObservableResolverList.cs RemoveOrigin — delete `_originList.Remove(value);` → RED (the substituted
		// IList<string> never receives Remove). 2026-08-02
		public void RemoveOrigin_RemovesValueFromOriginList()
		{
			var value = "1";

			_list.AddOrigin(value);

			Assert.IsTrue(_list.RemoveOrigin(value));
			_mockList.Received().Remove(value);
		}

		[Test]
		// ADMIT: ObservableResolverList<T,TOrigin>.ClearOrigin must clear the origin list, not just the resolved
		// one.
		// RCR: ObservableResolverList.cs ClearOrigin — delete `_originList.Clear();` → RED (the substituted
		// IList<string> never receives Clear). 2026-08-02
		public void ClearOrigin_ClearsOriginList()
		{
			_list.ClearOrigin();

			_mockList.Received().Clear();
		}

		[Test]
		// ADMIT: ObservableResolverList<T,TOrigin>.Rebind must empty the resolved list before rebuilding it, or the
		// old entries are prepended to the new origin's.
		// RCR: ObservableResolverList.cs Rebind — replace `List.Clear();` with `_ = List.Count;` → RED (Count is 6,
		// not 4, and List[0] is the stale 1). 2026-08-02
		public void Rebind_ChangesOriginList()
		{
			// Add initial data
			_list.AddOrigin("1");
			_list.AddOrigin("2");

			// Create new origin list and rebind
			var newOriginList = new List<string> { "10", "20", "30", "40" };
			_list.Rebind(
				newOriginList,
				origin => int.Parse(origin),
				value => value.ToString());

			// Verify new list is being used
			Assert.AreEqual(4, _list.Count);
			Assert.AreEqual(10, _list[0]);
			Assert.AreEqual(20, _list[1]);
			Assert.AreEqual(30, _list[2]);
			Assert.AreEqual(40, _list[3]);

			// Verify add operation uses new origin list
			_list.Add(50);
			Assert.AreEqual("50", newOriginList[4]);
		}

		[Test]
		// ADMIT: ObservableResolverList<T,TOrigin>.Rebind must leave `_updateActions` untouched, so observers
		// registered before the rebind still fire for post-rebind mutations.
		// RCR: ObservableResolverList.cs Rebind — add `StopObservingAll();` to the body → RED (observerCalls is 0,
		// not 1, after Add(300)). 2026-08-02
		public void Rebind_KeepsObservers()
		{
			// Setup observer
			var observerCalls = 0;
			_list.Observe((index, prev, curr, type) => observerCalls++);

			// Create new origin list and rebind
			var newOriginList = new List<string> { "100", "200" };
			_list.Rebind(
				newOriginList,
				origin => int.Parse(origin),
				value => value.ToString());

			// Trigger update and verify observer is still active
			_list.Add(300);
			Assert.AreEqual(1, observerCalls);
		}

		[Test]
		// ADMIT: ObservableResolverList<T,TOrigin> inherits ObservableList<T>.StopObserving, which must detach the
		// delegate before the resolver's overridden Add fans out.
		// RCR: ObservableList.cs StopObserving — empty the body → RED (observerCalls is 1, not 0). Unavoidable
		// overlap: this is the same edit that reddens ObservableListTest.StopObserveCheck. 2026-08-02
		public void StopObserving_StopsNotifications()
		{
			var observerCalls = 0;
			Action<int, int, int, ObservableUpdateType> observer = (index, prev, curr, type) => observerCalls++;
			
			_list.Observe(observer);
			_list.StopObserving(observer);
			
			_list.Add(300);
			Assert.AreEqual(0, observerCalls);
		}

		[Test]
		// ADMIT: ObservableResolverList<T,TOrigin>.AddOrigin resolves eagerly — it invokes `_fromOrignResolver`
		// during the call, so a bad origin value surfaces immediately instead of on first read.
		// RCR: ObservableResolverList.cs AddOrigin — delete `List.Add(_fromOrignResolver(value));` → RED (no
		// FormatException is thrown). 2026-08-02
		public void Add_InvalidFormat_ThrowsException()
		{
			// The resolver 'origin => int.Parse(origin)' will throw if origin is not a valid number
			// AddOrigin parses the origin value immediately, so it throws on add
			Assert.Throws<FormatException>(() => _list.AddOrigin("invalid"));
		}
	}
}