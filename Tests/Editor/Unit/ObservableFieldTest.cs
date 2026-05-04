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
		public void InvokeObserveCheck()
		{
			_observableField.InvokeObserve(_caller.UpdateCall);
			_observableResolverField.InvokeObserve(_caller.UpdateCall);

			_caller.Received(2).UpdateCall(0, 0);
		}

		[Test]
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
		public void ResolverField_ImplicitConversionToT_ReturnsCurrentValue()
		{
			_mockInt = 42;

			int implicitlyConverted = _observableResolverField;

			Assert.AreEqual(_mockInt, implicitlyConverted);
		}
	}
}