using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ObservableDebugRegistryTest
	{
		[Test]
		// ADMIT: ObservableDebugRegistry.GetAutoName falls back to the caller-supplied `kind` when the StackTrace
		// frame's source line matches none of its three regexes (e.g. a call made from inside a lambda).
		// RCR: ObservableDebugRegistry.cs GetAutoName — change `string memberName = kind;` to `= "";` → RED (Name
		// becomes "Unknown.<Object>", no longer containing "TestKind"). The surrounding try/catch is NOT a valid
		// mutation site: every access inside it is already null/bounds-guarded, so removing it changes nothing. 2026-08-02
		public void GetAutoName_WhenSourceFileIsUnavailable_FallsBackToKindAndDoesNotThrow()
		{
			var instance = new object();

			Assert.DoesNotThrow(() =>
			{
				// Register from inside a lambda so the calling line has no clean field/property/assignment
				// declaration for TryExtractMemberName's regexes to match, forcing the no-match fallback path.
				System.Action register = () => ObservableDebugRegistry.Register(instance, "TestKind", () => "value", () => 0);
				register();
			});

			var snapshot = ObservableDebugRegistry.FindByInstance(instance);

			Assert.IsTrue(snapshot.HasValue);
			StringAssert.Contains("TestKind", snapshot.Value.Name);
		}
	}
}
