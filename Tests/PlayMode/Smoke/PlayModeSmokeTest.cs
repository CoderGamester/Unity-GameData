using System.Collections;
using GameLovers.GameData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.GameData.Tests.PlayMode.Smoke
{
	[TestFixture]
	public class PlayModeSmokeTest
	{
		[UnityTest]
		// ADMIT: smoke — exempt from A1/A2 by directory (Tests/AGENTS.md §1, smoke exemption). Defect class: the
		// runtime assembly no longer loads in a PlayMode player loop.
		// RCR: none claimed — smoke exemption; ObservableField's notify path is mutated under ObservableFieldTest.
		// 2026-08-02
		public IEnumerator ObservableField_UpdatesDuringPlayMode()
		{
			var field = new ObservableField<int>(10);
			var val = 0;
			field.Observe((p, c) => val = c);
			
			yield return null; // Wait for one frame
			
			field.Value = 20;
			Assert.AreEqual(20, val);
		}

		[UnityTest]
		// ADMIT: smoke — exempt from A1/A2 by directory (Tests/AGENTS.md §1, smoke exemption). Defect class: the
		// runtime assembly no longer loads in a PlayMode player loop.
		// RCR: none claimed — smoke exemption; ComputedField's dependency path is mutated under ComputedFieldTest.
		// 2026-08-02
		public IEnumerator ComputedField_UpdatesDuringPlayMode()
		{
			var field = new ObservableField<int>(10);
			var computed = field.Select(x => x * 2);
			
			yield return null;
			
			field.Value = 20;
			Assert.AreEqual(40, computed.Value);
		}
	}
}
