using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class floatPTests
	{
		[Test]
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
		public void Addition()
		{
			Assert.AreEqual(floatP.One + floatP.One, (floatP)2f);
			Assert.AreEqual(floatP.One - floatP.One, (floatP)0f);
		}

		[Test]
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
		public void Division_BasicCases()
		{
			Assert.AreEqual((floatP)10f / (floatP)2f, (floatP)5f);
			Assert.AreEqual((floatP)1f / (floatP)2f, (floatP)0.5f);
		}

		[Test]
		public void Division_ByZero_ReturnsInfinity()
		{
			Assert.IsTrue(((floatP)1f / floatP.Zero).IsInfinity());
			Assert.IsTrue((floatP.One / floatP.Zero).IsPositiveInfinity());
			Assert.IsTrue((floatP.MinusOne / floatP.Zero).IsNegativeInfinity());
		}

		[Test]
		public void Modulo_BasicCases()
		{
			Assert.AreEqual((floatP)10f % (floatP)3f, (floatP)1f);
			Assert.AreEqual((floatP)10f % (floatP)5f, (floatP)0f);
		}

		[Test]
		public void FromRaw_ToRaw_RoundTrip()
		{
			uint raw = 0x3F800000; // 1.0f
			floatP f = floatP.FromRaw(raw);
			Assert.AreEqual(1.0f, (float)f);
			Assert.AreEqual(raw, f.RawValue);
		}

		[Test]
		public void ImplicitConversion_FromFloat()
		{
			floatP f = 1.23f;
			Assert.AreEqual(1.23f, (float)f, 0.0001f);
		}

		[Test]
		public void ExplicitConversion_ToFloat()
		{
			floatP f = (floatP)1.23f;
			float val = (float)f;
			Assert.AreEqual(1.23f, val, 0.0001f);
		}

		[Test]
		public void ExplicitConversion_ToInt_Truncates()
		{
			Assert.AreEqual(1, (int)(floatP)1.9f);
			Assert.AreEqual(-1, (int)(floatP)(-1.9f));
		}

		[Test]
		public void NaN_Propagation()
		{
			Assert.IsTrue((floatP.NaN + floatP.One).IsNaN());
			Assert.IsTrue((floatP.One - floatP.NaN).IsNaN());
			Assert.IsTrue((floatP.NaN * floatP.One).IsNaN());
			Assert.IsTrue((floatP.One / floatP.NaN).IsNaN());
		}

		[Test]
		public void Infinity_Handling()
		{
			Assert.IsTrue((floatP.PositiveInfinity + floatP.One).IsPositiveInfinity());
			Assert.IsTrue((floatP.NegativeInfinity - floatP.One).IsNegativeInfinity());
			Assert.IsTrue((floatP.PositiveInfinity * floatP.PositiveInfinity).IsPositiveInfinity());
			Assert.IsTrue((floatP.PositiveInfinity / floatP.One).IsPositiveInfinity());
		}

		[Test]
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
		public void Sign_NaNArgument_Throws()
		{
			Assert.Throws<System.ArithmeticException>(() => floatP.Sign(floatP.NaN));

			Assert.AreEqual(1, floatP.Sign((floatP)5f));
			Assert.AreEqual(-1, floatP.Sign((floatP)(-5f)));
			Assert.AreEqual(0, floatP.Sign(floatP.Zero));
			Assert.AreEqual(0, floatP.Sign(-floatP.Zero));
		}
	}
}
