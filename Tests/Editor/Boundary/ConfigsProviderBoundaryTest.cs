using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests.Boundary
{
	[TestFixture]
	public class ConfigsProviderBoundaryTest
	{
		private ConfigsProvider _provider;

		[SetUp]
		public void Setup()
		{
			_provider = new ConfigsProvider();
		}

		[Test]
		// ADMIT: ConfigsProvider.GetConfigsList<T> must project exactly the container's Values — an empty
		// registration yields an empty list, not a one-element default.
		// RCR: ConfigsProvider.cs GetConfigsList<T> — add a `{ default(T) }` initialiser to the returned list → RED
		// (Count is 1, not 0). Also reddens the id-keyed count assertions in ConfigsProviderTest. 2026-08-02
		public void EmptyConfigs_GetConfigsList_ReturnsEmptyList()
		{
			_provider.AddConfigs<int>(x => x, new List<int>());
			var list = _provider.GetConfigsList<int>();
			Assert.AreEqual(0, list.Count);
		}

		[Test]
		public void MaxIntId_StoresCorrectly()
		{
			_provider.AddConfigs<int>(x => x, new List<int> { int.MaxValue });
			Assert.AreEqual(int.MaxValue, _provider.GetConfig<int>(int.MaxValue));
		}

		[Test]
		// ADMIT: ConfigsProvider.AddConfigs must reject a null referenceIdResolver with ArgumentNullException before
		// it is invoked in the fill loop.
		// RCR: ConfigsProvider.cs AddConfigs — disable the `referenceIdResolver == null` guard → RED
		// (NullReferenceException arrives instead of the expected ArgumentNullException). 2026-08-02
		public void NullResolver_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => _provider.AddConfigs<int>(null, new List<int> { 1 }));
		}
	}
}
