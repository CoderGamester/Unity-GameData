using GameLovers.GameData;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ValueDataConversionTest
	{
		[Test]
		// ADMIT: Vector2Serializable's implicit Vector2 operator must map x→x and y→y (it builds a Vector3 that
		// then narrows, which is exactly where a component swap would hide).
		// RCR: ValueData.cs Vector2Serializable implicit operator Vector2 — swap to `new Vector3(v.y, v.x)` → RED
		// (x/y come back transposed). Also reddens JsonConvertersTest.Vector2_RoundTrip. 2026-08-02
		public void Vector2Serializable_ImplicitConversion_RoundTrip_PreservesXY()
		{
			var original = new Vector2(3f, 7f);

			Vector2Serializable serializable = original;
			Vector2 roundTripped = serializable;

			Assert.AreEqual(original.x, roundTripped.x);
			Assert.AreEqual(original.y, roundTripped.y);
		}

		[Test]
		// ADMIT: Vector3IntSerializable's implicit Vector3Int operator must carry all three components — the int
		// wrappers are separate code paths from the float ones.
		// RCR: ValueData.cs Vector3IntSerializable implicit operator Vector3Int — `new Vector3Int(v.x, v.y, 0)` →
		// RED (the Vector3Int round trip loses z). 2026-08-02
		public void VectorSerializable_ImplicitRoundTrip_PreservesValues()
		{
			var v3 = new Vector3(1f, 2f, 3f);
			Vector3Serializable v3s = v3;
			Vector3 v3r = v3s;
			Assert.AreEqual(v3, v3r);

			var v4 = new Vector4(1f, 2f, 3f, 4f);
			Vector4Serializable v4s = v4;
			Vector4 v4r = v4s;
			Assert.AreEqual(v4, v4r);

			var quat = new Quaternion(0.1f, 0.2f, 0.3f, 0.4f);
			Vector4Serializable quatS = quat;
			Quaternion quatR = quatS;
			Assert.AreEqual(quat.x, quatR.x);
			Assert.AreEqual(quat.y, quatR.y);
			Assert.AreEqual(quat.z, quatR.z);
			Assert.AreEqual(quat.w, quatR.w);

			var v3i = new Vector3Int(5, 6, 7);
			Vector3IntSerializable v3is = v3i;
			Vector3Int v3ir = v3is;
			Assert.AreEqual(v3i, v3ir);

			var v2i = new Vector2Int(8, 9);
			Vector2IntSerializable v2is = v2i;
			Vector2Int v2ir = v2is;
			Assert.AreEqual(v2i, v2ir);
		}
	}
}
