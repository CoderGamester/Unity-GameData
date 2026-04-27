using System;
using GameLovers.GameData;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class SerializableTypeTest
	{
		[Test]
		public void Constructor_WithType_StoresCorrectly()
		{
			var st = new SerializableType<int>();
			Assert.AreEqual(typeof(int), st.Value);
		}

		[Test]
		public void Value_Property_ResolvesCorrectly()
		{
			// Simulate Unity deserialization (private serialized fields populated, then
			// OnAfterDeserialize resolves them) via the internal test seam — no reflection,
			// no struct-boxing dance.
			var st = SerializableType<object>.FromSerializedNames(typeof(string).FullName, typeof(string).Assembly.FullName);

			Assert.AreEqual(typeof(string), st.Value);
		}

		[Test]
		public void Equals_SameType_ReturnsTrue()
		{
			var st1 = new SerializableType<int>();
			var st2 = new SerializableType<int>();
			Assert.IsTrue(st1.Equals(st2));
		}

		[Test]
		public void Equals_DifferentType_ReturnsFalse()
		{
			var st1 = new SerializableType<int>();
			var st2 = new SerializableType<string>();
			Assert.IsFalse(st1.Equals(st2));
		}

		[Test]
		public void GetHashCode_SameType_SameHash()
		{
			var st1 = new SerializableType<int>();
			var st2 = new SerializableType<int>();
			Assert.AreEqual(st1.GetHashCode(), st2.GetHashCode());
		}

		[Test]
		public void ImplicitConversion_ToType_Works()
		{
			var st = new SerializableType<int>();
			Type t = st;
			Assert.AreEqual(typeof(int), t);
		}
	}
}
