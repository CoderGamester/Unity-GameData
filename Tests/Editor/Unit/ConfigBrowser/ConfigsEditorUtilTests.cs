using System.Collections.Generic;
using GameLovers.GameData;
using GameLovers.GameData.Editor;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	/// <summary>
	/// Unit tests for <see cref="ConfigsEditorUtil"/>, verifying that reflection-based
	/// config reading from <c>Dictionary&lt;int, T&gt;</c> containers works correctly
	/// and rejects unsupported container types.
	/// </summary>
	[TestFixture]
	public class ConfigsEditorUtilTests
	{
		[Test]
		// ADMIT: ConfigsEditorUtil.TryReadConfigs must return entries sorted by id — Dictionary iteration order is
		// not insertion order, and the browser tree renders in the returned order.
		// RCR: ConfigsEditorUtil.cs TryReadConfigs — reverse the comparator to `b.Id.CompareTo(a.Id)` → RED
		// (entries[0].Id is 2, not 1). 2026-08-02
		public void TryReadConfigs_WithDictionaryIntKey_ReturnsSortedEntries()
		{
			var dict = new Dictionary<int, string>
			{
				{ 2, "B" },
				{ 1, "A" }
			};

			var success = ConfigsEditorUtil.TryReadConfigs(dict, out var entries);

			Assert.IsTrue(success);
			Assert.AreEqual(2, entries.Count);
			Assert.AreEqual(1, entries[0].Id);
			Assert.AreEqual("A", entries[0].Value);
			Assert.AreEqual(2, entries[1].Id);
			Assert.AreEqual("B", entries[1].Value);
		}

		[Test]
		// ADMIT: ConfigsEditorUtil.TryReadConfigs must reject a container that is not a generic Dictionary<,>
		// instead of reporting a successful read of nothing.
		// RCR: ConfigsEditorUtil.cs TryReadConfigs — change that guard's `return false;` to `return true;` → RED
		// (IsFalse fails). 2026-08-02
		public void TryReadConfigs_WithNonDictionary_ReturnsFalse()
		{
			var list = new List<int> { 1, 2, 3 };

			var success = ConfigsEditorUtil.TryReadConfigs(list, out var entries);

			Assert.IsFalse(success);
			Assert.AreEqual(0, entries.Count);
		}

		[Test]
		// ADMIT: ConfigsEditorUtil.TryReadConfigs must reject a dictionary whose key is not int, before the
		// reflection loop casts the key.
		// RCR: ConfigsEditorUtil.cs TryReadConfigs — change the `keyType != typeof(int)` guard's `return false;` to
		// `return true;` → RED (IsFalse fails). 2026-08-02
		public void TryReadConfigs_WithNonIntKey_ReturnsFalse()
		{
			var dict = new Dictionary<string, int>
			{
				{ "one", 1 }
			};

			var success = ConfigsEditorUtil.TryReadConfigs(dict, out var entries);

			Assert.IsFalse(success);
			Assert.AreEqual(0, entries.Count);
		}
	}
}
