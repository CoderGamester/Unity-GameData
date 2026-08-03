using System;
using System.Collections.Generic;
using GameLovers.GameData;
using GameLoversEditor.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class EditorConfigValidatorTest
	{
		private ConfigsProvider _provider;

		[SetUp]
		public void Setup()
		{
			_provider = new ConfigsProvider();
		}

		[Test]
		// ADMIT: EditorConfigValidator.ValidateMember records an error only when `attr.IsValid` returns FALSE — a
		// fully valid config must produce none.
		// RCR: EditorConfigValidator.cs ValidateMember — drop the `!` from the `IsValid` guard → RED (3 errors on a
		// valid config). Broad: this is the one seam every test in this fixture crosses. 2026-08-02
		public void ValidateAll_NoErrors_ReturnsEmptyResult()
		{
			var config = new MockValidatableConfigBuilder()
				.WithName("Hero")
				.WithHealth(50)
				.WithTag("ABC")
				.Build();
			_provider.AddSingletonConfig(config);

			var result = EditorConfigValidator.ValidateAll(_provider);
			Assert.AreEqual(0, result.Errors.Count);
		}

		[Test]
		// ADMIT: EditorConfigValidator.ValidateMember must attribute each error to the offending member, so a caller
		// can point at Name/Health/Tag rather than just the config type.
		// RCR: EditorConfigValidator.cs ValidateMember — change `FieldName = memberName,` to `type.Name` → RED (all
		// three Exists(e => e.FieldName == ...) assertions fail). 2026-08-02
		public void ValidateAll_WithErrors_ReturnsAllErrors()
		{
			var config = new MockValidatableConfigBuilder().Invalid().Build();
			_provider.AddSingletonConfig(config);

			var result = EditorConfigValidator.ValidateAll(_provider);
			
			Assert.AreEqual(3, result.Errors.Count);
			Assert.IsTrue(result.Errors.Exists(e => e.FieldName == "Name"));
			Assert.IsTrue(result.Errors.Exists(e => e.FieldName == "Health"));
			Assert.IsTrue(result.Errors.Exists(e => e.FieldName == "Tag"));
		}

		[Test]
		// ADMIT: EditorConfigValidator.ValidateObject must walk the instance's members — an early-out reports every
		// invalid config as clean.
		// RCR: EditorConfigValidator.cs ValidateObject — invert the null guard to `if (obj != null) return;` → RED
		// (0 errors, not 1). Also reddens ValidateAll_WithErrors_ReturnsAllErrors. 2026-08-02
		public void Validate_SpecificType_OnlyValidatesThatType()
		{
			var config = new MockValidatableConfigBuilder().WithName("").Build();
			_provider.AddSingletonConfig(config);

			var result = EditorConfigValidator.Validate<MockValidatableConfig>(_provider);
			Assert.AreEqual(1, result.Errors.Count);
		}

		[Test]
		// ADMIT: EditorConfigValidator.Validate<T> must record a ValidConfig entry for each instance that added no
		// errors — the Config Browser's "clean" list comes from it.
		// RCR: EditorConfigValidator.cs Validate<T> — change the `Errors.Count == errorCountBefore` guard to
		// `if (false)` → RED (ValidConfigs.Count is 0, not 1). 2026-08-02
		public void ValidateAll_ValidConfigs_TracksValidOnes()
		{
			var config = new MockValidatableConfigBuilder().Build();
			_provider.AddSingletonConfig(config);

			var result = EditorConfigValidator.ValidateAll(_provider);
			Assert.AreEqual(1, result.ValidConfigs.Count);
		}

		[Test]
		public void Validate_UsingBuilder_DefaultValues_PassValidation()
		{
			// Default builder creates valid config
			var provider = new ConfigsProviderBuilder()
				.WithSingleton(new MockValidatableConfigBuilder().Build())
				.Build();

			var result = EditorConfigValidator.ValidateAll(provider);
			Assert.AreEqual(0, result.Errors.Count);
		}
	}
}
