using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NSubstitute;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ObservableBatchTest
	{
		private IBatchable _mockBatchable1;
		private IBatchable _mockBatchable2;

		[SetUp]
		public void Setup()
		{
			_mockBatchable1 = Substitute.For<IBatchable>();
			_mockBatchable2 = Substitute.For<IBatchable>();
		}

		[Test]
		public void Add_SuppressesNotificationsImmediately()
		{
			var batch = new ObservableBatch();
			batch.Add(_mockBatchable1);

			_mockBatchable1.Received(1).SuppressNotifications();
		}

		[Test]
		// ADMIT: ObservableBatch.Add must suppress each observable as it joins the batch, not defer suppression to
		// Dispose.
		// RCR: ObservableBatch.cs Add — delete `observable.SuppressNotifications();` → RED (neither substitute
		// receives SuppressNotifications). 2026-08-02
		public void Add_MultipleObservables_AllSuppressed()
		{
			var batch = new ObservableBatch();
			batch.Add(_mockBatchable1);
			batch.Add(_mockBatchable2);

			_mockBatchable1.Received(1).SuppressNotifications();
			_mockBatchable2.Received(1).SuppressNotifications();
		}

		[Test]
		// ADMIT: ObservableBatch.Dispose must resume every observable it collected.
		// RCR: ObservableBatch.cs Dispose — delete `observable.ResumeNotifications();` from the loop → RED (neither
		// substitute receives ResumeNotifications). Also reddens DoubleDispose_NoError_NoDoubleNotification.
		// 2026-08-02
		public void Dispose_ResumesAllNotifications()
		{
			var batch = new ObservableBatch();
			batch.Add(_mockBatchable1);
			batch.Add(_mockBatchable2);

			batch.Dispose();

			_mockBatchable1.Received(1).ResumeNotifications();
			_mockBatchable2.Received(1).ResumeNotifications();
		}

		[Test]
		// ADMIT: ObservableBatch.Dispose replays in insertion order, so a computed field added after its sources
		// resumes last and recomputes once.
		// RCR: ObservableBatch.cs Dispose — reverse the loop (`for (var i = _observables.Count - 1; i > -1; i--)`) →
		// RED (callOrder is [2,1]). 2026-08-02
		public void Dispose_NotificationsInAddOrder()
		{
			// Received calls are tracked in order in NSubstitute, but we can also use a list
			var callOrder = new List<int>();
			_mockBatchable1.When(x => x.ResumeNotifications()).Do(_ => callOrder.Add(1));
			_mockBatchable2.When(x => x.ResumeNotifications()).Do(_ => callOrder.Add(2));

			var batch = new ObservableBatch();
			batch.Add(_mockBatchable1);
			batch.Add(_mockBatchable2);

			batch.Dispose();

			Assert.AreEqual(1, callOrder[0]);
			Assert.AreEqual(2, callOrder[1]);
		}

		[Test]
		// ADMIT: ObservableBatch.Dispose must be idempotent — a second call may not re-fire ResumeNotifications.
		// RCR: none exists — the second call is stopped by both `if (_disposed) return;` and the already-emptied
		// `_observables` list; removing either leaves the other preventing the re-notify (verified).
		// Double-covered, not single-line falsifiable. 2026-08-02
		public void DoubleDispose_NoError_NoDoubleNotification()
		{
			var batch = new ObservableBatch();
			batch.Add(_mockBatchable1);

			batch.Dispose();
			batch.Dispose();

			_mockBatchable1.Received(1).ResumeNotifications();
		}

		[Test]
		// ADMIT: ObservableBatch.Add must reject a disposed batch instead of silently suppressing an observable
		// nothing will ever resume.
		// RCR: ObservableBatch.cs Add — change the `if (_disposed)` guard to `if (false)` → RED (no
		// ObjectDisposedException is thrown). 2026-08-02
		public void AddAfterDispose_ThrowsObjectDisposedException()
		{
			var batch = new ObservableBatch();
			batch.Dispose();

			Assert.Throws<ObjectDisposedException>(() => batch.Add(_mockBatchable1));
		}
	}
}
