using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests.Integration
{
	[TestFixture]
	public class ObservableComputedIntegrationTest
	{
		[Test]
		// ADMIT: ObservableList<T>.Count.get must call ComputedTracker.OnRead, or a ComputedField that reads Count
		// never registers the list as a dependency and goes stale on Add.
		// RCR: ObservableList.cs Count.get — delete `ComputedTracker.OnRead(this);` → RED (computed.Value stays 122
		// after list.Add(3)). 2026-08-02
		public void ComputedField_WithMultipleObservables_TracksAll()
		{
			var field1 = new ObservableField<int>(10);
			var field2 = new ObservableField<int>(20);
			var list = new ObservableList<int>(new List<int> { 1, 2 });
			
			var computed = new ComputedField<int>(() => field1.Value + field2.Value + list.Count);
			
			Assert.AreEqual(32, computed.Value);

			field1.Value = 100;
			Assert.AreEqual(122, computed.Value);

			list.Add(3);
			Assert.AreEqual(123, computed.Value);
		}

		[Test]
		// ADMIT: ComputedField<T>'s IBatchable.ResumeNotifications must fire the deferred InvokeUpdate, so a batch
		// that spans several dependencies recomputes exactly once at the end.
		// RCR: ComputedField.cs IBatchable.ResumeNotifications — delete the `InvokeUpdate();` call → RED (callCount
		// is 0, not 1). Also reddens ComputedFieldTest.BeginBatch_SuppressesRecomputation. 2026-08-02
		public void BatchUpdates_WithComputedField_SingleRecalculation()
		{
			var field1 = new ObservableField<int>(10);
			var field2 = new ObservableField<int>(20);
			var callCount = 0;
			
			var computed = new ComputedField<int>(() =>
			{
				callCount++;
				return field1.Value + field2.Value;
			});

			computed.Observe((p, c) => { }); // Observe to trigger recompute on dependency change
			var val = computed.Value; // initial compute
			callCount = 0;

			// When computed is included in the batch, it should only recompute once
			// when the batch ends (not once per field)
			using (var batch = new ObservableBatch())
			{
				batch.Add(field1);
				batch.Add(field2);
				batch.Add(computed);
				
				field1.Value = 100;
				field2.Value = 200;
			}
			
			Assert.AreEqual(1, callCount);
		}

		[Test]
		// ADMIT: a ComputedField acting as another's dependency must record the downstream subscriber, or the dirty
		// flag stops at the first link.
		// RCR: ComputedField.cs IComputedDependency.Subscribe — comment out
		// `_dependencyActions.Add(onDependencyChanged);` → RED (c2 still reports 12 after the root changes). A5 note:
		// shared with ComputedFieldTest.ChainedComputed_MultiLevelDependencies. 2026-08-02
		public void ChainedComputedFields_PropagateDirtyFlag()
		{
			var field = new ObservableField<int>(10);
			var c1 = field.Select(x => x + 1);
			var c2 = c1.Select(x => x + 1);
			
			Assert.AreEqual(12, c2.Value);
			
			field.Value = 20;
			Assert.AreEqual(22, c2.Value);
		}
	}
}
