using System;
using GameLovers.GameData;
using GameLoversEditor.GameData;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class MigrationRunnerTest
	{
		[Serializable]
		public class MockConfig
		{
			public int Value;
			public string NewField;
		}

		[Serializable]
		public class MockStats
		{
			public int DamageReduction;
			public int CritChance;
		}

		[Serializable]
		public class MockComplexConfig
		{
			public int Id;
			public string Name;
			public int AttackDamage;
			public string ArmorType;
			public int BaseHealth;
			public int BonusHealth;
			public MockStats Stats;
			public string[] Abilities;
		}

		public class MockScriptableConfig : ScriptableObject
		{
			public int Value;
		}

		[ConfigMigration(typeof(MockConfig))]
		public class MockMigration_v1_v2 : IConfigMigration
		{
			public ulong FromVersion => 1;
			public ulong ToVersion => 2;
			public void Migrate(JObject configJson)
			{
				configJson["Value"] = (int)configJson["Value"] + 10;
			}
		}

		[ConfigMigration(typeof(MockConfig))]
		public class MockMigration_v2_v3 : IConfigMigration
		{
			public ulong FromVersion => 2;
			public ulong ToVersion => 3;
			public void Migrate(JObject configJson)
			{
				configJson["NewField"] = "Migrated";
			}
		}

		[ConfigMigration(typeof(MockComplexConfig))]
		public class MockComplex_v1_v2 : IConfigMigration
		{
			public ulong FromVersion => 1;
			public ulong ToVersion => 2;
			public void Migrate(JObject configJson)
			{
				// Rename Damage -> AttackDamage
				configJson["AttackDamage"] = configJson["Damage"];
				configJson.Remove("Damage");

				// Add ArmorType based on Health
				int health = (int)configJson["Health"];
				configJson["ArmorType"] = health >= 100 ? "Heavy" : "Light";
			}
		}

		[ConfigMigration(typeof(MockComplexConfig))]
		public class MockComplex_v2_v3 : IConfigMigration
		{
			public ulong FromVersion => 2;
			public ulong ToVersion => 3;
			public void Migrate(JObject configJson)
			{
				// Split Health -> Base + Bonus
				int totalHealth = (int)configJson["Health"];
				configJson["BaseHealth"] = (int)(totalHealth * 0.8f);
				configJson["BonusHealth"] = totalHealth - (int)configJson["BaseHealth"];
				configJson.Remove("Health");

				// Add Stats object
				configJson["Stats"] = new JObject
				{
					["DamageReduction"] = (string)configJson["ArmorType"] == "Heavy" ? 40 : 10,
					["CritChance"] = 5
				};

				// Add empty array
				configJson["Abilities"] = new JArray();
			}
		}

		[ConfigMigration(typeof(MockScriptableConfig))]
		public class MockScriptable_v1_v2 : IConfigMigration
		{
			public ulong FromVersion => 1;
			public ulong ToVersion => 2;
			public void Migrate(JObject configJson)
			{
				configJson["Value"] = (int)configJson["Value"] + 10;
			}
		}

		public class MockThrowingScriptableConfig : ScriptableObject
		{
			public int Value;
		}

		[ConfigMigration(typeof(MockThrowingScriptableConfig))]
		public class MockThrowingMigration_v1_v2 : IConfigMigration
		{
			public ulong FromVersion => 1;
			public ulong ToVersion => 2;
			public void Migrate(JObject configJson)
			{
				throw new InvalidOperationException("simulated migration failure");
			}
		}

		[SetUp]
		public void Setup()
		{
			MigrationRunner.Initialize(force: true);
		}

		[Test]
		// ADMIT: MigrationRunner.GetConfigTypesWithMigrations must expose the keys discovered by Initialize, or the
		// Config Browser's migration panel shows nothing.
		// RCR: MigrationRunner.cs GetConfigTypesWithMigrations — `return Array.Empty<Type>();` → RED (Assert.Contains
		// cannot find MockConfig). 2026-08-02
		public void GetConfigTypesWithMigrations_ReturnsCorrectTypes()
		{
			var types = MigrationRunner.GetConfigTypesWithMigrations();
			Assert.Contains(typeof(MockConfig), (System.Collections.ICollection)types);
		}

		[Test]
		// ADMIT: MigrationRunner.GetAvailableMigrations must return migrations ordered by FromVersion — reflection
		// discovery order is arbitrary.
		// RCR: MigrationRunner.cs GetAvailableMigrations — `.OrderBy(m => m.FromVersion)` →
		// `.OrderByDescending(...)` → RED (migrations[0].FromVersion is 2, not 1). 2026-08-02
		public void GetAvailableMigrations_ReturnsOrderedMigrations()
		{
			var migrations = MigrationRunner.GetAvailableMigrations<MockConfig>();
			Assert.AreEqual(2, migrations.Count);
			Assert.AreEqual(1, (int)migrations[0].FromVersion);
			Assert.AreEqual(2, (int)migrations[1].FromVersion);
		}

		[Test]
		// ADMIT: MigrationRunner.GetLatestVersion must report the HIGHEST ToVersion across the registered
		// migrations — MigrateScriptableObject uses it as the implicit target.
		// RCR: MigrationRunner.cs GetLatestVersion — change `list.Max(...)` to `list.Min(...)` → RED (2, not 3).
		// 2026-08-02
		public void GetLatestVersion_ReturnsCorrectVersion()
		{
			Assert.AreEqual(3, (int)MigrationRunner.GetLatestVersion(typeof(MockConfig)));
		}

		[Test]
		// ADMIT: MigrationRunner.Migrate must report how many migrations it actually applied — callers use the count
		// to decide whether to write the asset back.
		// RCR: MigrationRunner.cs Migrate — `return 0;` instead of `applicableMigrations.Count` → RED (count is 0,
		// not 2). Also reddens MigrateScriptableObject_AppliesMigrations_UpdatesObject. 2026-08-02
		public void Migrate_AppliesSequentialMigrations()
		{
			var json = new JObject { ["Value"] = 5 };
			var count = MigrationRunner.Migrate(typeof(MockConfig), json, 1, 3);

			Assert.AreEqual(2, count);
			Assert.AreEqual(15, (int)json["Value"]);
			Assert.AreEqual("Migrated", (string)json["NewField"]);
		}

		[Test]
		// ADMIT: MigrationRunner.Migrate must actually invoke each selected IConfigMigration against the JObject —
		// the rename/derive transformations happen in that call.
		// RCR: MigrationRunner.cs Migrate — delete `migration.Migrate(configJson);` from the apply loop → RED
		// ("Damage" survives and AttackDamage is absent). Also reddens the sibling Migrate_* tests. 2026-08-02
		public void Migrate_ComplexPatterns_v1ToV2_Works()
		{
			var json = new JObject
			{
				["Id"] = 1,
				["Name"] = "Unit",
				["Health"] = 150,
				["Damage"] = 20
			};

			MigrationRunner.Migrate(typeof(MockComplexConfig), json, 1, 2);

			Assert.IsNull(json["Damage"]);
			Assert.AreEqual(20, (int)json["AttackDamage"]);
			Assert.AreEqual("Heavy", (string)json["ArmorType"]);
		}

		[Test]
		// ADMIT: MigrationRunner.Migrate's lower bound is INCLUSIVE — a migration whose FromVersion equals the
		// current version is the one that must run.
		// RCR: MigrationRunner.cs Migrate — change `m.Migration.FromVersion >= currentVersion` to `>` → RED (nothing
		// is applied; "Health" survives). Also reddens the sibling Migrate_* tests. 2026-08-02
		public void Migrate_ComplexPatterns_v2ToV3_Works()
		{
			var json = new JObject
			{
				["Id"] = 1,
				["Name"] = "Unit",
				["AttackDamage"] = 20,
				["ArmorType"] = "Heavy",
				["Health"] = 100
			};

			MigrationRunner.Migrate(typeof(MockComplexConfig), json, 2, 3);

			Assert.IsNull(json["Health"]);
			Assert.AreEqual(80, (int)json["BaseHealth"]);
			Assert.AreEqual(20, (int)json["BonusHealth"]);
			Assert.IsNotNull(json["Stats"]);
			Assert.AreEqual(40, (int)json["Stats"]["DamageReduction"]);
			Assert.IsInstanceOf<JArray>(json["Abilities"]);
		}

		[Test]
		// ADMIT: MigrationRunner.Migrate must apply a chain in ascending FromVersion order — v2→v3 consumes the
		// "Health" field that v1→v2 still needs to read.
		// RCR: MigrationRunner.cs Migrate — `.OrderBy(m => m.Migration.FromVersion)` → `.OrderByDescending(...)` →
		// RED (v2→v3 runs first and removes "Health", so v1→v2's read of it fails). 2026-08-02
		public void Migrate_ComplexPatterns_Chained_v1ToV3_Works()
		{
			var json = new JObject
			{
				["Id"] = 1,
				["Name"] = "Unit",
				["Health"] = 150,
				["Damage"] = 20
			};

			MigrationRunner.Migrate(typeof(MockComplexConfig), json, 1, 3);

			Assert.IsNull(json["Damage"]);
			Assert.IsNull(json["Health"]);
			Assert.AreEqual(20, (int)json["AttackDamage"]);
			Assert.AreEqual("Heavy", (string)json["ArmorType"]);
			Assert.AreEqual(120, (int)json["BaseHealth"]);
			Assert.AreEqual(30, (int)json["BonusHealth"]);
			Assert.AreEqual(40, (int)json["Stats"]["DamageReduction"]);
			Assert.AreEqual(0, ((JArray)json["Abilities"]).Count);
		}

		[Test]
		// ADMIT: MigrationRunner.MigrateScriptableObject must write the migrated JSON back onto the asset, not just
		// compute it.
		// RCR: MigrationRunner.cs MigrateScriptableObject — delete the `JsonConvert.PopulateObject(...)` call → RED
		// (so.Value stays 5 instead of 15). 2026-08-02
		public void MigrateScriptableObject_AppliesMigrations_UpdatesObject()
		{
			var so = ScriptableObject.CreateInstance<MockScriptableConfig>();
			try
			{
				so.Value = 5;

				var result = MigrationRunner.MigrateScriptableObject(
					so, typeof(MockScriptableConfig), fromVersion: 1, toVersion: 2);

				Assert.IsTrue(result.Success);
				Assert.AreEqual(1, result.MigrationsApplied);
				Assert.AreEqual(15, so.Value);
			}
			finally
			{
				ScriptableObject.DestroyImmediate(so);
			}
		}

		[Test]
		// ADMIT: MigrationRunner.MigrateScriptableObject must not touch the asset when fromVersion is already at or
		// above toVersion.
		// RCR: none exists — the no-op is protected twice: the `fromVersion >= toVersion` early return AND the
		// `count == 0` check after Migrate finds no applicable migration for 5→5; disabling either leaves the other
		// returning NoMigrations with Value untouched (verified). Double-covered, not single-line falsifiable.
		// 2026-08-02
		public void MigrateScriptableObject_FromAtOrAboveTo_ReturnsNoMigrations()
		{
			var so = ScriptableObject.CreateInstance<MockScriptableConfig>();
			try
			{
				so.Value = 5;

				var result = MigrationRunner.MigrateScriptableObject(
					so, typeof(MockScriptableConfig), fromVersion: 5, toVersion: 5);

				Assert.IsTrue(result.Success);
				Assert.AreEqual(0, result.MigrationsApplied);
				Assert.AreEqual(5, so.Value);
			}
			finally
			{
				ScriptableObject.DestroyImmediate(so);
			}
		}

		[Test]
		// ADMIT: MigrationRunner.MigrateScriptableObject must convert a throwing migration into a failed
		// MigrationResult carrying the message, not let it escape or report success.
		// RCR: MigrationRunner.cs MigrateScriptableObject — return `MigrationResult.Ok(0)` from the catch block →
		// RED (IsFalse(result.Success) fails). 2026-08-02
		public void MigrateScriptableObject_MigrationThrows_ReturnsErrorResult()
		{
			var so = ScriptableObject.CreateInstance<MockThrowingScriptableConfig>();
			try
			{
				so.Value = 5;

				var result = MigrationRunner.MigrateScriptableObject(
					so, typeof(MockThrowingScriptableConfig), fromVersion: 1, toVersion: 2);

				Assert.IsFalse(result.Success);
				Assert.IsNotNull(result.Message);
				StringAssert.Contains("simulated migration failure", result.Message);
				Assert.AreEqual(0, result.MigrationsApplied);
			}
			finally
			{
				ScriptableObject.DestroyImmediate(so);
			}
		}
	}
}
