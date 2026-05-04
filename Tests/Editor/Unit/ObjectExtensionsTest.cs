using System.Collections.Generic;
using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ObjectExtensionsTest
	{
		[Test]
		public void IsValid_NullAndLiveReferences_ReturnsCorrectly()
		{
			object live = new object();
			object nullRef = null;

			Assert.IsFalse(nullRef.IsValid());
			Assert.IsTrue(live.IsValid());

			Assert.IsNull(((string)null).GetValid());
			Assert.AreEqual("x", "x".GetValid());

			Assert.IsFalse(((string)null).TryGetValid(out _));
			Assert.IsTrue("x".TryGetValid(out var valid));
			Assert.AreEqual("x", valid);
		}

		[Test]
		public void GetDisplayString_GenericAndArrayTypes_FormatsReadable()
		{
			var listType = typeof(List<int>);

			var first = listType.GetDisplayString();
			var second = listType.GetDisplayString();

			StringAssert.Contains("List", first);
			StringAssert.Contains("Int32", first);
			Assert.AreEqual(first, second);
		}
	}
}
