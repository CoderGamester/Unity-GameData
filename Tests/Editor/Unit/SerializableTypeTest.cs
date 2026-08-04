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
		// ADMIT: SerializableType<T>.Value falls back to typeof(T) when `_value` was never resolved, so a default
		// instance is usable rather than null.
		// RCR: SerializableType.cs Value.get — drop the `?? typeof(T)` fallback → RED (Value is null, not
		// typeof(int)). Also reddens ImplicitConversion_ToType_Works and the IEquatable<Type> test. 2026-08-02
		public void Constructor_WithType_StoresCorrectly()
		{
			var st = new SerializableType<int>();
			Assert.AreEqual(typeof(int), st.Value);
		}

		[Test]
		// ADMIT: SerializableType<T>.OnAfterDeserializeImpl must resolve the serialized class/assembly names back
		// into a Type instead of short-circuiting them away.
		// RCR: SerializableType.cs OnAfterDeserializeImpl — force the empty-names guard to `if (true)` → RED (Value
		// is typeof(object), not typeof(string)). 2026-08-02
		public void Value_Property_ResolvesCorrectly()
		{
			// Simulate Unity deserialization (private serialized fields populated, then
			// OnAfterDeserialize resolves them) via the internal test seam — no reflection,
			// no struct-boxing dance.
			var st = SerializableType<object>.FromSerializedNames(typeof(string).FullName, typeof(string).Assembly.FullName);

			Assert.AreEqual(typeof(string), st.Value);
		}

		[Test]
		// ADMIT: SerializableType<T>.Equals(SerializableType<T>) compares the resolved Value, so two instances
		// pointing at the same type are equal.
		// RCR: SerializableType.cs Equals(SerializableType<T>) — `return false;` → RED (IsTrue fails). 2026-08-02
		public void Equals_SameType_ReturnsTrue()
		{
			var st1 = new SerializableType<int>();
			var st2 = new SerializableType<int>();
			Assert.IsTrue(st1.Equals(st2));
		}


		[Test]
		public void GetHashCode_SameType_SameHash()
		{
			var st1 = new SerializableType<int>();
			var st2 = new SerializableType<int>();
			Assert.AreEqual(st1.GetHashCode(), st2.GetHashCode());
		}

		[Test]
		// ADMIT: SerializableType<T>'s implicit Type operator must route through Value, which is what lets the
		// struct stand in for a Type in call sites.
		// RCR: SerializableType.cs implicit operator Type — `return null;` → RED (null, not typeof(int)). 2026-08-02
		public void ImplicitConversion_ToType_Works()
		{
			var st = new SerializableType<int>();
			Type t = st;
			Assert.AreEqual(typeof(int), t);
		}

		[Test]
		// ADMIT: SerializableType<T>.Equals(Type) — the IEquatable<Type> overload — must compare against the
		// resolved Value in both directions.
		// RCR: SerializableType.cs Equals(Type) — `return true;` → RED (Equals(typeof(string)) is expected false).
		// 2026-08-02
		public void Equals_IEquatableType_SameRuntimeType_ReturnsTrue_DifferentType_ReturnsFalse()
		{
			var st = new SerializableType<int>();

			Assert.IsTrue(st.Equals(typeof(int)));
			Assert.IsFalse(st.Equals(typeof(string)));
		}
	}
}
