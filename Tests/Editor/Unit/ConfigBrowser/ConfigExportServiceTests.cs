using GameLovers.GameData;
using GameLovers.GameData.Editor;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ConfigExportServiceTests
	{
		[Test]
		public void ExportProviderToJson_WithSingletonAndCollection_EmitsKeyedJson()
		{
			var provider = new ConfigsProviderBuilder()
				.WithSingleton(new MockSingletonConfig { Value = 42 })
				.WithCollection(c => c.Id,
					new MockCollectionConfig { Id = 1, Name = "First" },
					new MockCollectionConfig { Id = 2, Name = "Second" })
				.Build();

			var json = ConfigExportService.ExportProviderToJson(provider);

			StringAssert.Contains(typeof(MockCollectionConfig).FullName, json);
			StringAssert.Contains("\"1\"", json);
			StringAssert.Contains("\"2\"", json);
			StringAssert.Contains("First", json);
			StringAssert.Contains("Second", json);
		}
	}
}
