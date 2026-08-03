using System;
using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests.Boundary
{
	[TestFixture]
	public class floatPBoundaryTest
	{
		[Test]
		// ADMIT: floatP.operator* saturates to a signed infinity when the product exponent overflows past 255.
		// RCR: floatP.cs operator* - overflow `sign ^ RawPositiveInfinity` -> `sign` -> RED (MaxValue*2 is zero, not infinity).
		public void MaxValue_Operations()
		{
			var max = floatP.MaxValue;
			// Adding 1 to MaxValue doesn't overflow to infinity due to float precision limits
			// (the mantissa can't represent the difference). Multiplying by 2 does cause overflow.
			Assert.IsTrue((max * (floatP)2f).IsInfinity());
			Assert.AreEqual(max, max * floatP.One);
		}

		[Test]
		// ADMIT: floatP.operator* carries the product's sign bit, so MinValue * One stays negative.
		// RCR: floatP.cs operator* - `sign = (uint)man & 0x80000000` -> `sign = 0` -> RED (MinValue*One returns +MaxValue).
		public void MinValue_Operations()
		{
			var min = floatP.MinValue;
			// Subtracting 1 from MinValue doesn't overflow to infinity due to float precision limits.
			// Multiplying by 2 does cause overflow.
			Assert.IsTrue((min * (floatP)2f).IsInfinity());
			Assert.AreEqual(min, min * floatP.One);
		}

		[Test]
		// ADMIT: floatP.operator* propagates a NaN first operand instead of the other factor.
		// RCR: floatP.cs operator* - non-finite fallthrough `return f1;` -> `return f2;` -> RED (NaN * Zero returns 0).
		public void NaN_Propagation_AllOperations()
		{
			var nan = floatP.NaN;
			Assert.IsTrue((nan + floatP.One).IsNaN());
			Assert.IsTrue((nan * floatP.Zero).IsNaN());
			Assert.IsTrue(MathfloatP.Sin(nan).IsNaN());
		}

		[Test]
		// ADMIT: floatP.operator== equates +0 and -0 despite their differing raw bit patterns.
		// RCR: floatP.cs operator== - drop the zero-magnitude clause -> RED (Zero == -Zero becomes false).
		public void Zero_NegativeZero_Equality()
		{
			var zero = floatP.Zero;
			var negZero = -floatP.Zero;
			Assert.IsTrue(zero == negZero);
			Assert.IsFalse(zero.RawValue == negZero.RawValue); // They differ in sign bit
		}
	}
}
