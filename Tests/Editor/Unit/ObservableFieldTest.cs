using GameLovers.GameData;
using NSubstitute;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ObservableFieldTest
	{
		/// <summary>
		/// Mocking interface to check method calls received
		/// </summary>
		public interface IMockCaller<in T>
		{
			void UpdateCall(T previous, T value);
		}

		private ObservableField<int> _observableField;
		private ObservableResolverField<int> _observableResolverField;
		private int _mockInt;
		private IMockCaller<int> _caller;

		[SetUp]
		public void Init()
		{
			// Reset before constructing: NUnit reuses one fixture instance for the whole class, so
			// _mockInt survives from any earlier test that wrote it (ValueSetCheck sets it to 5).
			_mockInt = 0;
			_caller = Substitute.For<IMockCaller<int>>();
			_observableField = new ObservableField<int>(_mockInt);
			_observableResolverField = new ObservableResolverField<int>(() => _mockInt, i => _mockInt = i);
		}

		[Test]
		public void ValueCheck()
		{
			Assert.AreEqual(_mockInt, _observableField.Value);
			Assert.AreEqual(_mockInt, _observableResolverField.Value);
		}

		[Test]
		// ADMIT: ObservableResolverField<T>'s setter must push through `_fieldSetter`, so the resolver field writes
		// the external backing variable while the plain ObservableField keeps its own copy.
		// RCR: ObservableResolverField.cs Value.set — delete `_fieldSetter(value);` → RED (_mockInt stays 5, so
		// AreEqual(valueCheck, _mockInt) fails). Also reddens RebindCheck. 2026-08-02
		public void ValueSetCheck()
		{
			const int valueCheck = 6;

			_mockInt = 5;

			Assert.AreNotEqual(_mockInt, _observableField.Value);
			Assert.AreEqual(_mockInt, _observableResolverField.Value);

			_observableField.Value = _mockInt;

			Assert.AreEqual(_mockInt, _observableField.Value);

			_observableResolverField.Value = valueCheck;

			Assert.AreEqual(valueCheck, _mockInt);
			Assert.AreNotEqual(_mockInt, _observableField.Value);
			Assert.AreEqual(_mockInt, _observableResolverField.Value);
		}

		[Test]
		// ADMIT: ObservableField<T>.Value.set must capture the pre-assignment value so observers receive the real
		// previous value, not the incoming one.
		// RCR: ObservableField.cs Value.set — change `var previousValue = _value;` to `= value;` → RED
		// (UpdateCall(6,6) instead of the expected UpdateCall(0,6)). Also reddens RebindCheck_BaseClass. 2026-08-02
		public void ObserveCheck()
		{
			const int valueCheck = 6;

			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());

			_observableField.Value = valueCheck;
			_observableResolverField.Value = valueCheck;

			_caller.Received(2).UpdateCall(0, valueCheck);
		}

		[Test]
		// ADMIT: ObservableField<T>.InvokeObserve must fire the callback once with the current value before
		// registering it, otherwise subscribers never get their initial state.
		// RCR: ObservableField.cs InvokeObserve — delete `onUpdate(Value, Value);` → RED (Received(2) on
		// UpdateCall(0,0) sees zero calls). 2026-08-02
		public void InvokeObserveCheck()
		{
			_observableField.InvokeObserve(_caller.UpdateCall);
			_observableResolverField.InvokeObserve(_caller.UpdateCall);

			_caller.Received(2).UpdateCall(0, 0);
		}

		[Test]
		// ADMIT: ObservableField<T>.InvokeUpdate() must re-broadcast the current value to every registered observer
		// on demand.
		// RCR: ObservableField.cs InvokeUpdate() — empty the body (drop `InvokeUpdate(Value);`) → RED
		// (Received(2) on UpdateCall(0,0) sees zero calls). 2026-08-02
		public void InvokeCheck()
		{
			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.Received(2).UpdateCall(0, 0);
		}

		[Test]
		public void InvokeCheck_NotObserving_DoesNothing()
		{
			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(0, 0);
		}

		[Test]
		// ADMIT: ObservableField<T>.StopObserving must actually detach the delegate from `_updateActions`.
		// RCR: ObservableField.cs StopObserving — empty the body (drop `_updateActions.Remove(onUpdate);`) → RED
		// (the caller still receives UpdateCall after InvokeUpdate). 2026-08-02
		public void StopObserveCheck()
		{
			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableField.StopObserving(_caller.UpdateCall);
			_observableResolverField.StopObserving(_caller.UpdateCall);

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		public void StopObserve_NotObserving_DoesNothing()
		{
			_observableField.StopObserving(_caller.UpdateCall);
			_observableResolverField.StopObserving(_caller.UpdateCall);

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		// ADMIT: ObservableField<T>.StopObservingAll(subscriber) matches on `Delegate.Target`, so passing the
		// substitute detaches the delegates that belong to it.
		// RCR: ObservableField.cs StopObservingAll — invert the `Target == subscriber` comparison → RED (the
		// observer survives and receives UpdateCall). Also reddens StopObservingAll_MultipleCalls_Check. 2026-08-02
		public void StopObservingAllCheck()
		{
			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableField.StopObservingAll(_caller);
			_observableResolverField.StopObservingAll(_caller);

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		// ADMIT: ObservableField<T>.StopObservingAll must remove EVERY delegate owned by the subscriber, not just
		// the last one, when the same handler was registered twice.
		// RCR: ObservableField.cs StopObservingAll — add `break;` after `_updateActions.RemoveAt(i);` → RED (one of
		// the two registrations survives and receives UpdateCall). 2026-08-02
		public void StopObservingAll_MultipleCalls_Check()
		{
			_observableField.Observe(_caller.UpdateCall);
			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableField.StopObservingAll(_caller);
			_observableResolverField.StopObservingAll(_caller);

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		// ADMIT: ObservableField<T>.StopObservingAll(null) takes the wholesale-clear branch instead of the
		// per-subscriber scan.
		// RCR: ObservableField.cs StopObservingAll — delete `_updateActions.Clear();` from the `subscriber == null`
		// branch → RED (the observer survives and receives UpdateCall). 2026-08-02
		public void StopObservingAll_Everything_Check()
		{
			_observableField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableField.StopObservingAll();
			_observableResolverField.StopObservingAll();

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		public void StopObservingAll_NotObserving_DoesNothing()
		{
			_observableField.StopObservingAll();
			_observableResolverField.StopObservingAll();

			_observableField.InvokeUpdate();
			_observableResolverField.InvokeUpdate();

			_caller.DidNotReceive().UpdateCall(Arg.Any<int>(), Arg.Any<int>());
		}

		[Test]
		// ADMIT: ObservableResolverField<T>.Rebind must swap the getter as well as the setter, or reads keep
		// resolving against the old backing variable.
		// RCR: ObservableResolverField.cs Rebind — delete `_fieldResolver = fieldResolver;` → RED
		// (AreEqual(newMockInt, Value) sees the old field's 0). Also reddens RebindCheck_KeepsObservers. 2026-08-02
		public void RebindCheck()
		{
			const int valueCheck = 10;
			var newMockInt = 5;

			// Setup observer
			_observableResolverField.Observe(_caller.UpdateCall);
			_caller.ClearReceivedCalls();

			// Rebind to a new field
			_observableResolverField.Rebind(() => newMockInt, i => newMockInt = i);

			// Verify rebind worked
			Assert.AreEqual(newMockInt, _observableResolverField.Value);

			// Set value through the rebinded field
			_observableResolverField.Value = valueCheck;

			// Verify the new field was updated
			Assert.AreEqual(valueCheck, newMockInt);
			Assert.AreEqual(valueCheck, _observableResolverField.Value);

			// Verify old field was not updated
			Assert.AreNotEqual(valueCheck, _mockInt);

			// Verify observers still work after rebind
			_caller.Received(1).UpdateCall(5, valueCheck);
		}

		[Test]
		// ADMIT: ObservableResolverField<T>.Rebind must not touch `_updateActions`, so observers registered before
		// the rebind keep firing afterwards.
		// RCR: ObservableResolverField.cs Rebind — add `StopObservingAll();` to the body → RED (Received(2) on
		// UpdateCall(0,15) sees zero calls). Also reddens RebindCheck. 2026-08-02
		public void RebindCheck_KeepsObservers()
		{
			const int valueCheck = 15;
			var newMockInt = 0;

			// Setup multiple observers before rebind
			_observableResolverField.Observe(_caller.UpdateCall);
			_observableResolverField.Observe(_caller.UpdateCall);

			// Rebind to a new field
			_observableResolverField.Rebind(() => newMockInt, i => newMockInt = i);
			_caller.ClearReceivedCalls();

			// Trigger update
			_observableResolverField.Value = valueCheck;

			// Verify both observers were notified
			_caller.Received(2).UpdateCall(0, valueCheck);
		}

		[Test]
		// ADMIT: ObservableField<T>.Rebind must replace `_value` silently — new value visible, observers intact.
		// RCR: ObservableField.cs Rebind(T) — empty the body (drop `_value = initialValue;`) → RED
		// (AreEqual(initialValue, Value) sees 0). 2026-08-02
		public void RebindCheck_BaseClass()
		{
			const int initialValue = 5;
			const int newValue = 10;

			// Setup observer on base class
			_observableField.Observe(_caller.UpdateCall);

			// Rebind to a new value
			_observableField.Rebind(initialValue);

			// Verify new value is set
			Assert.AreEqual(initialValue, _observableField.Value);

			// Trigger update and verify observer still works
			_observableField.Value = newValue;
			_caller.Received(1).UpdateCall(initialValue, newValue);
		}

		[Test]
		// ADMIT: ObservableField<T>'s IBatchable.SuppressNotifications must set `_isBatching`, collapsing the writes
		// inside a batch into one notification carrying the pre-batch previous value.
		// RCR: ObservableField.cs IBatchable.SuppressNotifications — change `_isBatching = true;` to `false` → RED
		// (two live notifications and none matching UpdateCall(0,20)). 2026-08-02
		public void BeginBatch_SuppressesNotifications()
		{
			_observableField.Observe(_caller.UpdateCall);

			using (_observableField.BeginBatch())
			{
				_observableField.Value = 10;
				_observableField.Value = 20;
			}

			_caller.Received(1).UpdateCall(0, 20);
		}

		[Test]
		// ADMIT: ObservableField<T>.Value.get must call ComputedTracker.OnRead so a ComputedField reading it
		// registers the field as a dependency and is invalidated on write.
		// RCR: ObservableField.cs Value.get — delete `ComputedTracker.OnRead(this);` → RED (computed.Value stays
		// cached at 0). Broad: also reddens most of ComputedFieldTest. 2026-08-02
		public void ComputedDependency_ReadTriggersTracking()
		{
			var dependencyCalled = false;
			var computed = new ComputedField<int>(() =>
			{
				dependencyCalled = true;
				return _observableField.Value;
			});

			var val = computed.Value;

			Assert.IsTrue(dependencyCalled);
			Assert.AreEqual(_observableField.Value, val);

			_observableField.Value = 10;
			// ComputedField should be dirty now and recompute on next access
			Assert.AreEqual(10, computed.Value);
		}

		[Test]
		// ADMIT: ObservableResolverField<T>'s implicit T operator must route through Value (the resolver), not a
		// stored copy.
		// RCR: ObservableResolverField.cs implicit operator T — change `=> value.Value;` to `=> default;` → RED
		// (converted 0 instead of 42). 2026-08-02
		public void ResolverField_ImplicitConversionToT_ReturnsCurrentValue()
		{
			_mockInt = 42;

			int implicitlyConverted = _observableResolverField;

			Assert.AreEqual(_mockInt, implicitlyConverted);
		}
	}
}