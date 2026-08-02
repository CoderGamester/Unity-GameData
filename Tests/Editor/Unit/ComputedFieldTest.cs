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
		public void InvokeObserve_ImmediatelyInvokes()
		{
			var computed = new ComputedField<int>(() => _field1.Value);
			var notifiedCount = 0;

			computed.InvokeObserve((prev, curr) => notifiedCount++);

			Assert.AreEqual(1, notifiedCount);
		}

		[Test]
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
		public void Select_TransformsSingleField()
		{
			var computed = _field1.Select(x => x * 2);
			Assert.AreEqual(20, computed.Value);

			_field1.Value = 15;
			Assert.AreEqual(30, computed.Value);
		}

		[Test]
		public void CombineWith_TwoFields()
		{
			var computed = _field1.CombineWith(_field2, (a, b) => a + b);
			Assert.AreEqual(30, computed.Value);

			_field1.Value = 100;
			Assert.AreEqual(120, computed.Value);
		}

		[Test]
		public void CombineWith_ThreeFields()
		{
			var computed = _field1.CombineWith(_field2, _field3, (a, b, c) => a + b + c);
			Assert.AreEqual(60, computed.Value);

			_field3.Value = 100;
			Assert.AreEqual(130, computed.Value);
		}

		[Test]
		public void CombineWith_FourFields()
		{
			var computed = _field1.CombineWith(_field2, _field3, _field4, (a, b, c, d) => a + b + c + d);
			Assert.AreEqual(100, computed.Value);

			_field4.Value = 100;
			Assert.AreEqual(160, computed.Value);
		}

		[Test]
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
