using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests.Smoke
{
	[TestFixture]
	public class EditModeSmokeTest
	{
		[Test]
		// ADMIT: smoke — exempt from A1/A2 by directory (Tests/AGENTS.md §1, smoke exemption). Defect class: the
		// GameLovers.GameData assembly no longer loads, or ConfigsProvider construction/registration regressed.
		// RCR: none claimed — smoke exemption; the per-symbol mutations live on the ConfigsProviderTest fixture.
		// 2026-08-02
		public void Configs_SmokeTest()
		{
			var provider = new ConfigsProvider();
			provider.AddSingletonConfig(new int[] { 1, 2, 3 });
			Assert.AreEqual(3, provider.GetConfig<int[]>().Length);
		}

		[Test]
		// ADMIT: smoke — exempt from A1/A2 by directory (Tests/AGENTS.md §1, smoke exemption). Defect class: the
		// GameLovers.GameData assembly no longer loads, or the observable bootstrap regressed.
		// RCR: none claimed — smoke exemption; the per-symbol mutations live on the ObservableFieldTest fixture.
		// 2026-08-02
		public void Observables_SmokeTest()
		{
			var field = new ObservableField<int>(10);
			var notified = false;
			field.Observe((p, c) => notified = true);
			field.Value = 20;
			Assert.IsTrue(notified);
		}

		[Test]
		// ADMIT: smoke — exempt from A1/A2 by directory (Tests/AGENTS.md §1, smoke exemption). Defect class: the
		// GameLovers.GameData assembly no longer loads, or the floatP/MathfloatP bootstrap regressed.
		// RCR: none claimed — smoke exemption; the per-symbol mutations live on the floatP/MathfloatP fixtures.
		// 2026-08-02
		public void Math_SmokeTest()
		{
			var a = (floatP)1.5f;
			var b = (floatP)2.5f;
			Assert.AreEqual((floatP)4.0f, a + b);
			Assert.AreEqual((floatP)1.0f, MathfloatP.Abs((floatP)(-1.0f)));
		}
	}
}
