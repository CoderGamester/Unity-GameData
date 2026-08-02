using System.Collections;
using GameLovers.GameData;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.GameData.Tests.PlayMode.Performance
{
	[TestFixture]
	public class RuntimePerformanceTest
	{
		[UnityTest, Performance]
		// ADMIT: `yield return Measure.Frames().Run();` drains the FramesMeasurement enumerator before any
		// workload runs, so every FrameTime sample covers idle frames rather than ObservableField<int>.Value's
		// setter/notify cost; driving the enumerator by hand puts the workload inside the measured window.
		// RCR (benchmark, inverted per Tests/AGENTS.md §2): comment out `field.Value = i;` below → the reported
		// FrameTime Median must drop from ~0.20ms back toward the ~0.11ms idle baseline. The per-frame count must
		// stay large: at ~8 assignments/frame the cost sits under the noise floor and cannot be discriminated. 2026-08-02
		public IEnumerator ObservableField_HighFrequencyUpdates_FrameTimeImpact()
		{
			var field = new ObservableField<int>(0);
			field.Observe((p, c) => { /* some work */ });

			var frames = Measure.Frames().Run();
			var i = 0;

			while (frames.MoveNext())
			{
				for (var j = 0; j < 50000; j++)
				{
					field.Value = i;
					i++;
				}

				yield return frames.Current;
			}
		}
	}
}
