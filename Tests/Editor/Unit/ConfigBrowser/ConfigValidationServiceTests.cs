using System.Collections.Generic;
using System.Linq;
using GameLovers.GameData;
using GameLovers.GameData.Editor;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	/// <summary>
	/// Unit tests for <see cref="ConfigValidationService"/>, verifying that validation attributes
	/// on config fields are correctly evaluated for both full-provider and single-entry scopes.
	/// </summary>
	[TestFixture]
	public class ConfigValidationServiceTests
	{
		[Test]
		public void ValidateAll_ReportsFieldsAndConfigIds()
		{
			// ConfigsProvider keys storage by Type, so a singleton and a keyed collection must
			// live under different types. MockValidatableConfigAlt covers the singleton role
			// (ConfigId reported as null) and MockValidatableConfig covers the keyed role
			// (ConfigId reported as the resolver-provided id).
			var provider = new ConfigsProvider();
			var invalidSingleton = new MockValidatableConfigAlt
			{
				Name = "",
				Health = 150,
				Tag = "A"
			};
			provider.AddSingletonConfig(invalidSingleton);

			var invalidList = new List<MockValidatableConfig>
			{
				new MockValidatableConfigBuilder().Invalid().Build(),
				new MockValidatableConfigBuilder().Invalid().Build()
			};

			var id = 5;
			provider.AddConfigs(_ => id++, invalidList);

			var errors = ConfigValidationService.ValidateAll(provider);

			Assert.IsTrue(errors.Any(e => e.ConfigId == null));
			Assert.IsTrue(errors.Any(e => e.ConfigId == 5));
			Assert.IsTrue(errors.Any(e => e.ConfigId == 6));

			Assert.IsTrue(errors.Any(e => e.FieldName == "Name"));
			Assert.IsTrue(errors.Any(e => e.FieldName == "Health"));
			Assert.IsTrue(errors.Any(e => e.FieldName == "Tag"));
		}

		[Test]
		public void ValidateSingle_UsesSelectionConfigId()
		{
			var invalid = new MockValidatableConfigBuilder().Invalid().Build();
			var selection = new ConfigSelection(typeof(MockValidatableConfig), 42, invalid);

			var errors = ConfigValidationService.ValidateSingle(selection);

			Assert.AreEqual(3, errors.Count);
			Assert.IsTrue(errors.All(e => e.ConfigId == 42));
			Assert.IsTrue(errors.All(e => e.ConfigTypeName == nameof(MockValidatableConfig)));
		}
	}
}
