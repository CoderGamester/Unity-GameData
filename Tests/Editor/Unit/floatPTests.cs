using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class floatPTests
	{
		[Test]
		// ADMIT: floatP's raw constant table must match IEEE binary32 bit patterns, so floatP.MaxValue equals (floatP)float.MaxValue.
		// RCR: floatP.cs RawMaxValue - `0x7F7FFFFF` -> `0x7F7FFFFE` -> RED (MaxValue no longer equals (floatP)float.MaxValue).
		public void Representation()
		{
			Assert.AreEqual(floatP.Zero, (floatP)0f);
			Assert.AreEqual(-floatP.Zero, (floatP) (-0f));
			Assert.AreEqual(floatP.Zero, -floatP.Zero);
			Assert.AreEqual(floatP.NaN, (floatP) float.NaN);
			Assert.AreEqual(floatP.MinusOne, (floatP)  (- 1f));
			Assert.AreEqual(floatP.PositiveInfinity, (floatP)float.PositiveInfinity);
			Assert.AreEqual(floatP.NegativeInfinity, (floatP)float.NegativeInfinity);
			Assert.AreEqual(floatP.Epsilon, (floatP)float.Epsilon);
			Assert.AreEqual(floatP.MaxValue, (floatP)float.MaxValue);
			Assert.AreEqual(floatP.MinValue, (floatP)float.MinValue);
		}

		[Test]
		// ADMIT: floatP.operator== must report false for NaN operands while Equals reports true.
		// RCR: floatP.cs operator== - NaN branch `return false;` -> `return true;` -> RED (NaN != NaN becomes false).
		public void Equality()
		{
			Assert.IsTrue(floatP.NaN != floatP.NaN);
			Assert.IsTrue(floatP.NaN.Equals(floatP.NaN));
			Assert.IsTrue(floatP.Zero == -floatP.Zero);
			Assert.IsTrue(floatP.Zero.Equals(-floatP.Zero));
			Assert.IsTrue(!(floatP.NaN > floatP.Zero));
			Assert.IsTrue(!(floatP.NaN >= floatP.Zero));
			Assert.IsTrue(!(floatP.NaN < floatP.Zero));
			Assert.IsTrue(!(floatP.NaN <= floatP.Zero));
			Assert.IsTrue(floatP.NaN.CompareTo(floatP.Zero) == -1);
			Assert.IsTrue(floatP.NaN.CompareTo(floatP.NegativeInfinity) == -1);
			Assert.IsTrue(!(-floatP.Zero < floatP.Zero));
		}

		[Test]
		// ADMIT: floatP.InternalAdd sums the exponent-aligned mantissas; the sign of that combination is what makes 1+1 == 2.
		// RCR: floatP.cs InternalAdd - mantissa `+` -> `-` -> RED (One + One returns 0, expected 2).
		public void Addition()
		{
			Assert.AreEqual(floatP.One + floatP.One, (floatP)2f);
			Assert.AreEqual(floatP.One - floatP.One, (floatP)0f);
		}

		[Test]
		// ADMIT: floatP.operator* returns NaN for Infinity * 0, matching IEEE binary32.
		// RCR: floatP.cs operator* - `Infinity * 0` branch `return NaN;` -> `return PositiveInfinity;` -> RED (Inf*0 gives Inf).
		public void Multiplication()
		{
			Assert.AreEqual(floatP.PositiveInfinity * floatP.Zero, (floatP) (float.PositiveInfinity * 0f));
			Assert.AreEqual(floatP.PositiveInfinity * (-floatP.Zero), (floatP)(float.PositiveInfinity * (-0f)));
			Assert.AreEqual(floatP.PositiveInfinity * floatP.One, (floatP)(float.PositiveInfinity * 1f));
			Assert.AreEqual(floatP.PositiveInfinity * floatP.MinusOne, (floatP)(float.PositiveInfinity * -1f));

			Assert.AreEqual(floatP.NegativeInfinity * floatP.Zero, (floatP)(float.NegativeInfinity * 0f));
			Assert.AreEqual(floatP.NegativeInfinity * (-floatP.Zero), (floatP)(float.NegativeInfinity * (-0f)));
			Assert.AreEqual(floatP.NegativeInfinity * floatP.One, (floatP)(float.NegativeInfinity * 1f));
			Assert.AreEqual(floatP.NegativeInfinity * floatP.MinusOne, (floatP)(float.NegativeInfinity * -1f));

			Assert.AreEqual(floatP.One * floatP.One, (floatP)1f);
		}

		[Test]
		// ADMIT: floatP.operator/ derives the quotient exponent as exp1-exp2+bias.
		// RCR: floatP.cs operator/ - quotient exponent `+ ExponentBias` -> `+ ExponentBias + 1` -> RED (10/2 returns 10).
		public void Division_BasicCases()
		{
			Assert.AreEqual((floatP)10f / (floatP)2f, (floatP)5f);
			Assert.AreEqual((floatP)1f / (floatP)2f, (floatP)0.5f);
		}

		[Test]
		// ADMIT: floatP.operator/ produces a signed infinity when the divisor is zero.
		// RCR: floatP.cs operator/ - `f / 0` branch `| RawPositiveInfinity` -> `| RawZero` -> RED (1/0 is not infinity).
		public void Division_ByZero_ReturnsInfinity()
		{
			Assert.IsTrue(((floatP)1f / floatP.Zero).IsInfinity());
			Assert.IsTrue((floatP.One / floatP.Zero).IsPositiveInfinity());
			Assert.IsTrue((floatP.MinusOne / floatP.Zero).IsNegativeInfinity());
		}

		[Test]
		// ADMIT: floatP.operator% forwards its operands to MathfloatP.Mod in dividend-then-divisor order.
		// RCR: floatP.cs operator% - `Mod(f1, f2)` -> `Mod(f2, f1)` -> RED (10 % 3 returns 3, expected 1).
		public void Modulo_BasicCases()
		{
			Assert.AreEqual((floatP)10f % (floatP)3f, (floatP)1f);
			Assert.AreEqual((floatP)10f % (floatP)5f, (floatP)0f);
		}

		[Test]
		// ADMIT: floatP.FromRaw stores the supplied bit pattern verbatim so RawValue round-trips it.
		// RCR: floatP.cs FromRaw - `new floatP(raw)` -> `new floatP(raw ^ 1u)` -> RED (RawValue differs from the input raw).
		public void FromRaw_ToRaw_RoundTrip()
		{
			uint raw = 0x3F800000; // 1.0f
			floatP f = floatP.FromRaw(raw);
			Assert.AreEqual(1.0f, (float)f);
			Assert.AreEqual(raw, f.RawValue);
		}

		[Test]
		// ADMIT: floatP's implicit float conversion reinterprets the bits unscaled, so the value survives the round trip.
		// RCR: floatP.cs operator floatP(float) - reinterpret `f` -> `f * 2f` -> RED (1.23f round-trips as 2.46f).
		public void ImplicitConversion_FromFloat()
		{
			floatP f = 1.23f;
			Assert.AreEqual(1.23f, (float)f, 0.0001f);
		}

		[Test]
		// ADMIT: none - `(floatP)1.23f` invokes the same implicit float operator as the sibling test.
		// RCR: none exists - A5 duplicate of ImplicitConversion_FromFloat: identical causal chain
		// (operator floatP(float) + operator float(floatP)); every mutation reddens both.
		public void ExplicitConversion_ToFloat()
		{
			floatP f = (floatP)1.23f;
			float val = (float)f;
			Assert.AreEqual(1.23f, val, 0.0001f);
		}

		[Test]
		// ADMIT: floatP's int conversion re-applies the sign after shifting the mantissa, truncating toward zero.
		// RCR: floatP.cs operator int(floatP) - sign ternary inverted -> RED ((int)(floatP)1.9f returns -1).
		public void ExplicitConversion_ToInt_Truncates()
		{
			Assert.AreEqual(1, (int)(floatP)1.9f);
			Assert.AreEqual(-1, (int)(floatP)(-1.9f));
		}

		[Test]
		// ADMIT: floatP.operator/ short-circuits to NaN when either operand is NaN.
		// RCR: floatP.cs operator/ - NaN guard `return NaN;` -> `return Zero;` -> RED (One / NaN is not NaN).
		public void NaN_Propagation()
		{
			Assert.IsTrue((floatP.NaN + floatP.One).IsNaN());
			Assert.IsTrue((floatP.One - floatP.NaN).IsNaN());
			Assert.IsTrue((floatP.NaN * floatP.One).IsNaN());
			Assert.IsTrue((floatP.One / floatP.NaN).IsNaN());
		}

		[Test]
		// ADMIT: floatP.operator/ keeps the sign of infinity when dividing +Infinity by a finite positive value.
		// RCR: floatP.cs operator/ - `Infinity / finite` sign ternary inverted -> RED (Inf/One is -Infinity).
		public void Infinity_Handling()
		{
			Assert.IsTrue((floatP.PositiveInfinity + floatP.One).IsPositiveInfinity());
			Assert.IsTrue((floatP.NegativeInfinity - floatP.One).IsNegativeInfinity());
			Assert.IsTrue((floatP.PositiveInfinity * floatP.PositiveInfinity).IsPositiveInfinity());
			Assert.IsTrue((floatP.PositiveInfinity / floatP.One).IsPositiveInfinity());
		}

		[Test]
		// ADMIT: floatP.FromParts keeps all 8 biased-exponent bits when packing sign/exponent/mantissa.
		// RCR: floatP.cs FromParts - exponent mask `0xff` -> `0x7f` -> RED (FromParts(true,128,0x200000) loses the top exponent bit).
		public void FromParts_RoundTrip_ReconstructsRawValue()
		{
			// IEEE binary32 "+1.0": sign=false, biased exponent=127, mantissa=0
			Assert.AreEqual(0x3F800000u, floatP.FromParts(false, 127, 0).RawValue);

			// IEEE binary32 "-2.5": sign=true, biased exponent=128, mantissa=0x200000
			var negTwoAndAHalf = floatP.FromParts(true, 128, 0x200000);
			Assert.AreEqual(0xC0200000u, negTwoAndAHalf.RawValue);
			Assert.AreEqual(-2.5f, (float)negTwoAndAHalf, 0.0001f);
		}

		[Test]
		// ADMIT: floatP.FromIeeeRaw is the exact inverse of ToIeeeRaw for every value class including NaN and infinities.
		// RCR: floatP.cs FromIeeeRaw - `new floatP(ieeeRaw)` -> `new floatP(ieeeRaw ^ 1u)` -> RED (round-tripped RawValue differs).
		public void IeeeRaw_RoundTrip_PreservesValue()
		{
			var values = new[]
			{
				floatP.Zero, floatP.One, floatP.MinusOne,
				(floatP)1.234f, (floatP)(-9876.5f),
				floatP.NaN, floatP.PositiveInfinity, floatP.NegativeInfinity,
				floatP.MaxValue, floatP.MinValue, floatP.Epsilon
			};
			foreach (var f in values)
			{
				var roundTripped = floatP.FromIeeeRaw(f.ToIeeeRaw());
				Assert.AreEqual(f.RawValue, roundTripped.RawValue);
			}
		}

		[Test]
		// ADMIT: floatP.Sign(floatP) rejects NaN with ArithmeticException instead of returning a sign.
		// RCR: floatP.cs Sign(floatP) - guard `value.IsNaN()` -> `value.IsInfinity()` -> RED (Sign(NaN) returns -1, no throw).
		public void Sign_NaNArgument_Throws()
		{
			Assert.Throws<System.ArithmeticException>(() => floatP.Sign(floatP.NaN));

			Assert.AreEqual(1, floatP.Sign((floatP)5f));
			Assert.AreEqual(-1, floatP.Sign((floatP)(-5f)));
			Assert.AreEqual(0, floatP.Sign(floatP.Zero));
			Assert.AreEqual(0, floatP.Sign(-floatP.Zero));
		}

		[Test]
		// ADMIT: floatP's static IsFinite must agree with the instance IsFinite for every value class.
		// RCR: floatP.cs IsFinite(floatP) - `!= 255` -> `== 255` -> RED (static IsFinite disagrees with the instance form).
		public void StaticOverloads_IsInfinity_IsNegativeInfinity_IsNaN_IsFinite_MatchInstanceForms()
		{
			var samples = new[]
			{
				floatP.PositiveInfinity, floatP.NegativeInfinity, floatP.NaN,
				floatP.Zero, floatP.One, (floatP)1.234f
			};

			foreach (var f in samples)
			{
				Assert.AreEqual(f.IsInfinity(), floatP.IsInfinity(f));
				Assert.AreEqual(f.IsNegativeInfinity(), floatP.IsNegativeInfinity(f));
				Assert.AreEqual(f.IsNaN(), floatP.IsNaN(f));
				Assert.AreEqual(f.IsFinite(), floatP.IsFinite(f));
			}
		}
	}
}
