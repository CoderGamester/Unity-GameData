using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NSubstitute;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ComputedFieldTest
	{
		private ObservableField<int> _field1;
		private ObservableField<int> _field2;
		private ObservableField<int> _field3;
		private ObservableField<int> _field4;

		[SetUp]
		public void Setup()
		{
			_field1 = new ObservableField<int>(10);
			_field2 = new ObservableField<int>(20);
			_field3 = new ObservableField<int>(30);
			_field4 = new ObservableField<int>(40);
		}

		[Test]
		// ADMIT: ComputedField<T> must stay lazy — the constructor records the delegate and `_isDirty` starts true,
		// so the computation runs on the first Value read and not before.
		// RCR: ComputedField.cs ctor — add `Recompute();` after `_computation = computation;` → RED (the first
		// AreEqual(0, callCount) sees 1). 2026-08-02
		public void Value_ComputesOnFirstAccess()
		{
			var callCount = 0;
			var computed = new ComputedField<int>(() =>
			{
				callCount++;
				return _field1.Value + _field2.Value;
			});

			Assert.AreEqual(0, callCount);
			Assert.AreEqual(30, computed.Value);
			Assert.AreEqual(1, callCount);
		}

		[Test]
		// ADMIT: ComputedField<T>.Value must recompute only while `_isDirty`; a second read of an unchanged field
		// serves the cached `_value`.
		// RCR: ComputedField.cs Value.get — drop the `if (_isDirty)` guard so `Recompute();` runs unconditionally →
		// RED (callCount is 2, not 1). 2026-08-02
		public void Value_CachesUntilDirty()
		{
			var callCount = 0;
			var computed = new ComputedField<int>(() =>
			{
				callCount++;
				return _field1.Value;
			});

			var val1 = computed.Value;
			var val2 = computed.Value;

			Assert.AreEqual(1, callCount);
			Assert.AreEqual(10, val1);
			Assert.AreEqual(10, val2);
		}

		[Test]
		// ADMIT: ComputedField<T>.OnDependencyChanged must act when the field is CLEAN — that is the transition that
		// invalidates a cached value after a dependency write.
		// RCR: ComputedField.cs OnDependencyChanged — invert the guard to `if (_isDirty)` → RED (Value stays cached
		// at 30). Broad: also reddens the sibling notification tests, which share this guard. 2026-08-02
		public void Value_RecalculatesWhenDependencyChanges()
		{
			var computed = new ComputedField<int>(() => _field1.Value + _field2.Value);

			Assert.AreEqual(30, computed.Value);

			_field1.Value = 100;
			Assert.AreEqual(120, computed.Value);

			_field2.Value = 200;
			Assert.AreEqual(300, computed.Value);
		}

		[Test]
		// ADMIT: ComputedField<T>.InvokeUpdate must snapshot `_value` BEFORE recomputing, so observers get the real
		// previous value rather than the new one twice.
		// RCR: ComputedField.cs InvokeUpdate — move `var previousValue = _value;` after `Recompute();` → RED
		// (lastPrev is 35, not 30). 2026-08-02
		public void Observe_NotifiesOnDependencyChange()
		{
			var computed = new ComputedField<int>(() => _field1.Value + _field2.Value);
			var notifiedCount = 0;
			var lastPrev = 0;
			var lastCurr = 0;

			computed.Observe((prev, curr) =>
			{
				notifiedCount++;
				lastPrev = prev;
				lastCurr = curr;
			});

			_field1.Value = 15;

			Assert.AreEqual(1, notifiedCount);
			Assert.AreEqual(30, lastPrev);
			Assert.AreEqual(35, lastCurr);
		}

		[Test]
		// ADMIT: ComputedField<T>.Observe must recompute a dirty field — establishing its ComputedTracker
		// dependency subscriptions — before adding the observer, or later dependency changes never notify it.
		// RCR: ComputedField.cs Observe — delete the `Recompute();` inside `if (_isDirty)` → RED (the observer
		// receives zero calls after the dependency changes). 2026-08-01
		public void Observe_WhenFieldIsDirty_RecomputesBeforeAddingObserver()
		{
			var callCount = 0;
			var computed = new ComputedField<int>(() => _field1.Value);

			// Field is dirty from construction (never read .Value), and no dependency is subscribed yet.
			computed.Observe((prev, curr) => callCount++);

			// Observe() itself must not have invoked the callback.
			Assert.AreEqual(0, callCount);

			// The dependency link must have been established during Observe()'s recompute, so this now notifies.
			_field1.Value = 999;
			Assert.AreEqual(1, callCount);
		}

		[Test]
		// ADMIT: ComputedField<T>.InvokeObserve must fire the handler once with the current value before
		// registering it.
		// RCR: ComputedField.cs InvokeObserve — delete `onUpdate(Value, Value);` → RED (notifiedCount is 0, not 1).
		// 2026-08-02
		public void InvokeObserve_ImmediatelyInvokes()
		{
			var computed = new ComputedField<int>(() => _field1.Value);
			var notifiedCount = 0;

			computed.InvokeObserve((prev, curr) => notifiedCount++);

			Assert.AreEqual(1, notifiedCount);
		}

		[Test]
		// ADMIT: ComputedField<T>.StopObserving must detach the handler from `_updateActions` while leaving the
		// dependency subscriptions intact.
		// RCR: ComputedField.cs StopObserving — replace `_updateActions.Remove(onUpdate);` with a read-only Contains
		// → RED (notifiedCount reaches 2 after the second dependency write). 2026-08-02
		public void StopObserving_StopsNotifications()
		{
			var computed = new ComputedField<int>(() => _field1.Value);
			var notifiedCount = 0;
			Action<int, int> observer = (prev, curr) => notifiedCount++;

			computed.Observe(observer);
			_field1.Value = 20;
			Assert.AreEqual(1, notifiedCount);

			computed.StopObserving(observer);
			_field1.Value = 30;
			Assert.AreEqual(1, notifiedCount);
		}

		[Test]
		// ADMIT: ComputedField<T>.Dispose must unsubscribe from every tracked IComputedDependency, or the field
		// keeps recomputing after its owner is gone.
		// RCR: ComputedField.cs Dispose — delete `dependency.Unsubscribe(OnDependencyChanged);` → RED (callCount
		// reaches 2 after the post-dispose write). 2026-08-02
		public void Dispose_UnsubscribesFromDependencies()
		{
			var callCount = 0;
			var computed = new ComputedField<int>(() =>
			{
				callCount++;
				return _field1.Value;
			});

			Assert.AreEqual(10, computed.Value);
			Assert.AreEqual(1, callCount);

			computed.Dispose();

			_field1.Value = 20;
			// Should not trigger recompute or notification if it were observed
			Assert.AreEqual(1, callCount); 
		}

		[Test]
		// ADMIT: ComputedField<T> stays uncomputed until Value is read, even after its sources change repeatedly.
		// RCR: ComputedField.cs Value getter — `if (false)` in place of `if (_isDirty)` → RED (Value is 0, not 30).
		// A5 note: no mutation reddens the `callCount == 0` half — nothing computes eagerly to begin with — so this
		// pins nothing that Value_ComputesOnFirstAccess does not already pin. 2026-08-02
		public void LazyEvaluation_DoesNotComputeUntilAccessed()
		{
			var callCount = 0;
			var computed = new ComputedField<int>(() =>
			{
				callCount++;
				return _field1.Value;
			});

			_field1.Value = 20;
			_field1.Value = 30;

			Assert.AreEqual(0, callCount);
			Assert.AreEqual(30, computed.Value);
			Assert.AreEqual(1, callCount);
		}

		[Test]
		// ADMIT: ComputedField<T>.InvokeUpdate must also fan out to `_dependencyActions`, which is how a computed
		// field marks a downstream computed field dirty.
		// RCR: ComputedField.cs InvokeUpdate — neuter the `_dependencyActions` loop (`for (var i = 0; i < 0; i++)`)
		// → RED (computed2.Value stays 12). Also reddens ChainedComputed_DeepHierarchy. 2026-08-02
		public void ChainedComputed_MultiLevelDependencies()
		{
			var computed1 = new ComputedField<int>(() => _field1.Value + 1);
			var computed2 = new ComputedField<int>(() => computed1.Value + 1);

			Assert.AreEqual(11, computed1.Value);
			Assert.AreEqual(12, computed2.Value);

			_field1.Value = 20;

			Assert.AreEqual(21, computed1.Value);
			Assert.AreEqual(22, computed2.Value);
		}

		[Test]
		// ADMIT: ObservableExtensions.Select must feed the SOURCE field's live value into the selector.
		// RCR: ObservableExtensions.cs Select — change `selector(source.Value)` to `selector(default)` → RED
		// (computed.Value is 0, not 20). Also reddens ChainedComputed_DeepHierarchy. 2026-08-02
		public void Select_TransformsSingleField()
		{
			var computed = _field1.Select(x => x * 2);
			Assert.AreEqual(20, computed.Value);

			_field1.Value = 15;
			Assert.AreEqual(30, computed.Value);
		}

		[Test]
		// ADMIT: ObservableExtensions.CombineWith (2-arg) must pass BOTH source values to the combiner.
		// RCR: ObservableExtensions.cs CombineWith<T1,T2,TResult> — change `combiner(first.Value, second.Value)` to
		// `combiner(first.Value, default)` → RED (30 becomes 10). 2026-08-02
		public void CombineWith_TwoFields()
		{
			var computed = _field1.CombineWith(_field2, (a, b) => a + b);
			Assert.AreEqual(30, computed.Value);

			_field1.Value = 100;
			Assert.AreEqual(120, computed.Value);
		}

		[Test]
		// ADMIT: ObservableExtensions.CombineWith (3-arg) must pass all three source values to the combiner.
		// RCR: ObservableExtensions.cs CombineWith<T1,T2,T3,TResult> — replace `third.Value` with `default` → RED
		// (60 becomes 30). 2026-08-02
		public void CombineWith_ThreeFields()
		{
			var computed = _field1.CombineWith(_field2, _field3, (a, b, c) => a + b + c);
			Assert.AreEqual(60, computed.Value);

			_field3.Value = 100;
			Assert.AreEqual(130, computed.Value);
		}

		[Test]
		// ADMIT: ObservableExtensions.CombineWith (4-arg) must pass all four source values to the combiner.
		// RCR: ObservableExtensions.cs CombineWith<T1,T2,T3,T4,TResult> — replace `fourth.Value` with `default` →
		// RED (100 becomes 60). 2026-08-02
		public void CombineWith_FourFields()
		{
			var computed = _field1.CombineWith(_field2, _field3, _field4, (a, b, c, d) => a + b + c + d);
			Assert.AreEqual(100, computed.Value);

			_field4.Value = 100;
			Assert.AreEqual(160, computed.Value);
		}

		[Test]
		// ADMIT: ComputedField<T>'s IBatchable.SuppressNotifications must set `_isBatching`, collapsing N dependency
		// writes inside a batch into a single recomputation on resume.
		// RCR: ComputedField.cs IBatchable.SuppressNotifications — change `_isBatching = true;` to `false` → RED
		// (callCount is 2, not 1). 2026-08-02
		public void BeginBatch_SuppressesRecomputation()
		{
			var callCount = 0;
			var computed = new ComputedField<int>(() =>
			{
				callCount++;
				return _field1.Value + _field2.Value;
			});

			computed.Observe((p, c) => { }); // Need to observe to trigger InvokeUpdate logic
			var initial = computed.Value; // initial compute
			callCount = 0;

			using (computed.BeginBatch())
			{
				_field1.Value = 100;
				_field2.Value = 200;
			}

			Assert.AreEqual(1, callCount); // Recomputed once at end of batch
			Assert.AreEqual(300, computed.Value);
		}

		[Test]
		// ADMIT: ObservableField.Computed<T> must hand the caller's delegate to the ComputedField it builds.
		// RCR: ComputedField.cs ObservableField.Computed — change `new ComputedField<T>(computation)` to
		// `new ComputedField<T>(() => default)` → RED (Value is 0, not 15). 2026-08-02
		public void StaticComputed_CreatesInstance()
		{
			var computed = ObservableField.Computed(() => _field1.Value + 5);
			Assert.AreEqual(15, computed.Value);
		}

		[Test]
		public void ChainedComputed_DeepHierarchy()
		{
			var c1 = _field1.Select(x => x + 1);
			var c2 = c1.Select(x => x + 1);
			var c3 = c2.Select(x => x + 1);
			var c4 = c3.Select(x => x + 1);

			Assert.AreEqual(14, c4.Value);

			_field1.Value = 20;
			Assert.AreEqual(24, c4.Value);
		}

		[Test]
		// ADMIT: ComputedField<T>.StopObservingAll(null) takes the wholesale-clear branch, dropping every registered
		// observer.
		// RCR: ComputedField.cs StopObservingAll — delete `_updateActions.Clear();` from the `subscriber == null`
		// branch → RED (count reaches 2). 2026-08-02
		public void StopObservingAll_Works()
		{
			var computed = _field1.Select(x => x);
			var count = 0;
			computed.Observe((p, c) => count++);
			computed.Observe((p, c) => count++);

			computed.StopObservingAll();
			_field1.Value = 20;

			Assert.AreEqual(0, count);
		}

		[Test]
		// ADMIT: ComputedField<T>.StopObservingAll(subscriber) matches on `Delegate.Target`, detaching only the
		// handlers owned by that object and leaving the others subscribed.
		// RCR: ComputedField.cs StopObservingAll — invert the `Target == subscriber` comparison → RED (count1 is 1
		// and count2 is 0 — exactly inverted). 2026-08-02
		public void StopObservingAll_WithSubscriber_Works()
		{
			var computed = _field1.Select(x => x);
			var subscriber1 = new object();
			var subscriber2 = new object();
			var count1 = 0;
			var count2 = 0;

			// NSubstitute can't easily provide Target, so using manual target actions
			Action<int, int> action1 = (p, c) => count1++;
			Action<int, int> action2 = (p, c) => count2++;

			// Wrapping in a way that we can set Target if we wanted, but List<Action>.Target is the object the method belongs to.
			// Let's use a helper class.
			var s1 = new TestSubscriber(() => count1++);
			var s2 = new TestSubscriber(() => count2++);

			computed.Observe(s1.OnUpdate);
			computed.Observe(s2.OnUpdate);

			computed.StopObservingAll(s1);
			_field1.Value = 20;

			Assert.AreEqual(0, count1);
			Assert.AreEqual(1, count2);
		}

		private class TestSubscriber
		{
			private readonly Action _onUpdate;
			public TestSubscriber(Action onUpdate) => _onUpdate = onUpdate;
			public void OnUpdate(int p, int c) => _onUpdate();
		}

		[Test]
		// ADMIT: ComputedField<T>.Dispose must leave the field inert — no observer may fire after disposal.
		// RCR: none exists — Dispose both unsubscribes from every dependency and clears `_updateActions`; removing
		// either leaves the other silencing the observer (verified: with `_updateActions.Clear()` gone,
		// OnDependencyChanged is never reached). Double-covered, not single-line falsifiable. 2026-08-02
		public void Dispose_ClearsObservers()
		{
			var computed = _field1.Select(x => x);
			var count = 0;
			computed.Observe((p, c) => count++);

			computed.Dispose();
			_field1.Value = 20;

			Assert.AreEqual(0, count);
		}

		[Test]
		// ADMIT: ComputedField<T>.Recompute must clear `_isDirty`, or OnDependencyChanged's `if (!_isDirty)` gate
		// latches shut and only the first dependency write is ever honoured.
		// RCR: ComputedField.cs Recompute — delete `_isDirty = false;` → RED (callCount is 1, not 2). Also reddens
		// Value_CachesUntilDirty. 2026-08-02
		public void Value_RecomputeOnEveryDependencyChange_WhenObserved()
		{
			var callCount = 0;
			var computed = new ComputedField<int>(() =>
			{
				callCount++;
				return _field1.Value;
			});

			computed.Observe((p, c) => { });
			var x = computed.Value; // 1
			callCount = 0;

			_field1.Value = 20; // triggers InvokeUpdate -> Recompute
			_field1.Value = 30; // triggers InvokeUpdate -> Recompute

			Assert.AreEqual(2, callCount);
		}
	}
}
