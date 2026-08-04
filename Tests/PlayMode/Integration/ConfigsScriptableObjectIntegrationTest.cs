using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.GameData.Tests.PlayMode.Integration
{
	[TestFixture]
	public class ConfigsScriptableObjectIntegrationTest
	{
		[Serializable]
		public class MockHeroConfigSO : ConfigsScriptableObject<int, string> { }

		[Test]
		// ADMIT: ConfigsScriptableObject<TId,TAsset>.OnAfterDeserialize must publish the dictionary it just built
		// through ConfigsDictionary — the list is the serialized form, the dictionary is the lookup form.
		// RCR: ConfigsScriptableObject.cs OnAfterDeserialize — wrap an empty dictionary instead of `dictionary` →
		// RED (Count is 0, not 2). Also reddens OnAfterDeserialize_DuplicateKeys_LogsError. 2026-08-02
		public void OnAfterDeserialize_BuildsDictionary()
		{
			var so = ScriptableObject.CreateInstance<MockHeroConfigSO>();
			so.Configs = new List<Pair<int, string>>
			{
				new Pair<int, string>(1, "Hero1"),
				new Pair<int, string>(2, "Hero2")
			};

			((ISerializationCallbackReceiver)so).OnAfterDeserialize();

			Assert.IsNotNull(so.ConfigsDictionary);
			Assert.AreEqual(2, so.ConfigsDictionary.Count);
			Assert.AreEqual("Hero1", so.ConfigsDictionary[1]);
			Assert.AreEqual("Hero2", so.ConfigsDictionary[2]);
		}

		[Test]
		// ADMIT: ConfigsScriptableObject<TId,TAsset>.OnAfterDeserialize uses TryAdd so a duplicate key is SKIPPED
		// with a logged error — first-one-wins, never a silent overwrite.
		// RCR: ConfigsScriptableObject.cs OnAfterDeserialize — replace the TryAdd guard with an unconditional
		// indexer assignment → RED (no expected LogError arrives and the value becomes "Second"). 2026-08-02
		public void OnAfterDeserialize_DuplicateKeys_LogsError()
		{
			var so = ScriptableObject.CreateInstance<MockHeroConfigSO>();
			so.Configs = new List<Pair<int, string>>
			{
				new Pair<int, string>(1, "First"),
				new Pair<int, string>(1, "Second")
			};

			LogAssert.Expect(LogType.Error, "Duplicate key '1' found in MockHeroConfigSO. Skipping.");
			((ISerializationCallbackReceiver)so).OnAfterDeserialize();

			Assert.AreEqual(1, so.ConfigsDictionary.Count);
			Assert.AreEqual("First", so.ConfigsDictionary[1]);
		}
	}
}
