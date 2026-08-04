using System;
using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class MathfloatPTest
	{
		private const float _epsilon = 0.001f;

		[Test]
		// ADMIT: MathfloatP.Abs must clear the sign bit of a finite floatP, so Abs(-5) is 5 and NaN passes through untouched.
		// RCR: MathfloatP.cs Abs - sign mask `& 0x7FFFFFFF` -> `| 0x80000000` -> RED (Abs(5f) returns -5).
		public void Abs_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Abs((floatP)5f));
			Assert.AreEqual((floatP)5f, MathfloatP.Abs((floatP)(-5f)));
			Assert.AreEqual(floatP.Zero, MathfloatP.Abs(floatP.Zero));
			Assert.IsTrue(MathfloatP.Abs(floatP.NaN).IsNaN());
		}

		[Test]
		// ADMIT: MathfloatP.Max must return NaN when the first argument is NaN, since `>` is false for every NaN comparison.
		// RCR: MathfloatP.cs Max - NaN branch `return val1;` -> `return val2;` -> RED (Max(NaN,1) returns 1, IsNaN false).
		public void Max_Works()
		{
			Assert.AreEqual((floatP)10f, MathfloatP.Max((floatP)5f, (floatP)10f));
			Assert.AreEqual((floatP)10f, MathfloatP.Max((floatP)10f, (floatP)5f));
			Assert.AreEqual((floatP)5f, MathfloatP.Max((floatP)5f, (floatP)5f));
			Assert.IsTrue(MathfloatP.Max(floatP.NaN, (floatP)1f).IsNaN());
		}

		[Test]
		// ADMIT: MathfloatP.Min must return NaN when the first argument is NaN, since `<` is false for every NaN comparison.
		// RCR: MathfloatP.cs Min - NaN branch `return val1;` -> `return val2;` -> RED (Min(NaN,1) returns 1, IsNaN false).
		public void Min_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Min((floatP)5f, (floatP)10f));
			Assert.AreEqual((floatP)5f, MathfloatP.Min((floatP)10f, (floatP)5f));
			Assert.AreEqual((floatP)5f, MathfloatP.Min((floatP)5f, (floatP)5f));
			Assert.IsTrue(MathfloatP.Min(floatP.NaN, (floatP)1f).IsNaN());
		}

		[Test]
		// ADMIT: MathfloatP.Clamp returns the lower bound, not the upper, when value is below min.
		// RCR: MathfloatP.cs Clamp - lower branch `return min;` -> `return max;` -> RED (Clamp(-5,0,10) returns 10).
		public void Clamp_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Clamp((floatP)5f, (floatP)0f, (floatP)10f));
			Assert.AreEqual((floatP)0f, MathfloatP.Clamp((floatP)(-5f), (floatP)0f, (floatP)10f));
			Assert.AreEqual((floatP)10f, MathfloatP.Clamp((floatP)15f, (floatP)0f, (floatP)10f));
		}

		[Test]
		// ADMIT: MathfloatP.Clamp01 saturates at One for inputs above 1.
		// RCR: MathfloatP.cs Clamp01 - upper branch `return floatP.One;` -> `return floatP.Zero;` -> RED (Clamp01(1.1) returns 0).
		public void Clamp01_Works()
		{
			Assert.AreEqual((floatP)0.5f, MathfloatP.Clamp01((floatP)0.5f));
			Assert.AreEqual(floatP.Zero, MathfloatP.Clamp01((floatP)(-0.1f)));
			Assert.AreEqual(floatP.One, MathfloatP.Clamp01((floatP)1.1f));
		}

		[Test]
		// ADMIT: MathfloatP.Lerp interpolates a + (b-a)*t, so t=0.5 between 0 and 10 is 5.
		// RCR: MathfloatP.cs Lerp - `a + (b - a) * t` -> `a - (b - a) * t` -> RED (Lerp(0,10,1) returns -10).
		public void Lerp_Works()
		{
			Assert.AreEqual((floatP)0f, MathfloatP.Lerp((floatP)0f, (floatP)10f, (floatP)0f));
			Assert.AreEqual((floatP)10f, MathfloatP.Lerp((floatP)0f, (floatP)10f, (floatP)1f));
			Assert.AreEqual((floatP)5f, MathfloatP.Lerp((floatP)0f, (floatP)10f, (floatP)0.5f));
			// Clamped
			Assert.AreEqual((floatP)10f, MathfloatP.Lerp((floatP)0f, (floatP)10f, (floatP)1.5f));
		}

		[Test]
		// ADMIT: MathfloatP.LerpUnclamped extrapolates past b for t>1 instead of clamping.
		// RCR: MathfloatP.cs LerpUnclamped - `a + (b - a) * t` -> `a - (b - a) * t` -> RED (returns -15, expected 15).
		public void LerpUnclamped_Works()
		{
			Assert.AreEqual((floatP)15f, MathfloatP.LerpUnclamped((floatP)0f, (floatP)10f, (floatP)1.5f));
		}

		[Test]
		// ADMIT: MathfloatP.SmoothStep applies the -2t^3+3t^2 Hermite curve, which is 0.5 at t=0.5.
		// RCR: MathfloatP.cs SmoothStep - coefficient `3.0f` -> `4.0f` -> RED (t=0.5 gives 7.5, expected 5).
		public void SmoothStep_Works()
		{
			Assert.AreEqual((floatP)0f, MathfloatP.SmoothStep((floatP)0f, (floatP)10f, (floatP)0f));
			Assert.AreEqual((floatP)10f, MathfloatP.SmoothStep((floatP)0f, (floatP)10f, (floatP)1f));
			Assert.AreEqual((floatP)5f, MathfloatP.SmoothStep((floatP)0f, (floatP)10f, (floatP)0.5f));
		}

		[Test]
		// ADMIT: MathfloatP.InverseLerp computes (value-a)/(b-a), so 5 within [0,10] is 0.5.
		// RCR: MathfloatP.cs InverseLerp - `(value - a)` -> `(a - value)` -> RED (returns 0, expected 0.5).
		public void InverseLerp_Works()
		{
			Assert.AreEqual((floatP)0.5f, MathfloatP.InverseLerp((floatP)0f, (floatP)10f, (floatP)5f));
			Assert.AreEqual((floatP)0f, MathfloatP.InverseLerp((floatP)0f, (floatP)10f, (floatP)0f));
			Assert.AreEqual((floatP)1f, MathfloatP.InverseLerp((floatP)0f, (floatP)10f, (floatP)10f));
		}

		[Test]
		// ADMIT: MathfloatP.MoveTowards steps current toward target by maxDelta in the direction of the difference.
		// RCR: MathfloatP.cs MoveTowards - `current + Sign(...)` -> `current - Sign(...)` -> RED (0->10 by 5 returns -5).
		public void MoveTowards_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.MoveTowards((floatP)0f, (floatP)10f, (floatP)5f));
			Assert.AreEqual((floatP)10f, MathfloatP.MoveTowards((floatP)0f, (floatP)10f, (floatP)15f));
			Assert.AreEqual((floatP)5f, MathfloatP.MoveTowards((floatP)10f, (floatP)0f, (floatP)5f));
		}

		[Test]
		// ADMIT: MathfloatP.Sign treats zero as positive (returns 1), unlike floatP.Sign which returns 0.
		// RCR: MathfloatP.cs Sign - `f >= floatP.Zero` -> `f > floatP.Zero` -> RED (Sign(Zero) returns -1, expected 1).
		public void Sign_Works()
		{
			Assert.AreEqual(1, MathfloatP.Sign((floatP)5f));
			Assert.AreEqual(-1, MathfloatP.Sign((floatP)(-5f)));
			Assert.AreEqual(1, MathfloatP.Sign(floatP.Zero));
		}

		[Test]
		// ADMIT: MathfloatP.Repeat subtracts floor(t/length)*length, wrapping negatives up into [0,length].
		// RCR: MathfloatP.cs Repeat - `Floor(t / length)` -> `Ceil(t / length)` -> RED (Repeat(12,10) returns 0, expected 2).
		public void Repeat_Works()
		{
			Assert.AreEqual((floatP)2f, MathfloatP.Repeat((floatP)12f, (floatP)10f));
			Assert.AreEqual((floatP)8f, MathfloatP.Repeat((floatP)(-2f), (floatP)10f));
		}

		[Test]
		// ADMIT: MathfloatP.PingPong folds the doubled-period value back with length - |t-length|.
		// RCR: MathfloatP.cs PingPong - `length - Abs(...)` -> `length + Abs(...)` -> RED (PingPong(5,10) returns 15).
		public void PingPong_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.PingPong((floatP)5f, (floatP)10f));
			Assert.AreEqual((floatP)5f, MathfloatP.PingPong((floatP)15f, (floatP)10f));
		}

		[Test]
		// ADMIT: MathfloatP.DeltaAngle subtracts a full 360 turn when the wrapped delta exceeds 180, giving the signed shortest arc.
		// RCR: MathfloatP.cs DeltaAngle - `delta -= 360.0f` -> `delta -= 180.0f` -> RED (DeltaAngle(10,0) returns 170, expected -10).
		public void DeltaAngle_Works()
		{
			Assert.AreEqual((floatP)10f, MathfloatP.DeltaAngle((floatP)0f, (floatP)10f));
			Assert.AreEqual((floatP)(-10f), MathfloatP.DeltaAngle((floatP)10f, (floatP)0f));
			Assert.AreEqual((floatP)(-20f), MathfloatP.DeltaAngle((floatP)350f, (floatP)330f));
			Assert.AreEqual((floatP)20f, MathfloatP.DeltaAngle((floatP)350f, (floatP)10f));
		}

		[Test]
		// ADMIT: MathfloatP.Sin uses Bhaskara's 16x(pi-x) numerator, giving 1 at pi/2.
		// RCR: MathfloatP.cs Sin - numerator `16.0f` -> `8.0f` -> RED (Sin(pi/2) returns ~0.5, tolerance 0.001).
		public void Sin_Works()
		{
			Assert.AreEqual(0f, (float)MathfloatP.Sin(floatP.Zero), _epsilon);
			Assert.AreEqual(1f, (float)MathfloatP.Sin((floatP)(Math.PI / 2.0)), _epsilon);
			Assert.AreEqual(0f, (float)MathfloatP.Sin((floatP)Math.PI), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Cos is Sin phase-shifted by +pi/2.
		// RCR: MathfloatP.cs Cos - `Sin(x + pi/2)` -> `Sin(x - pi/2)` -> RED (Cos(0) returns -1, expected 1).
		public void Cos_Works()
		{
			Assert.AreEqual(1f, (float)MathfloatP.Cos(floatP.Zero), _epsilon);
			Assert.AreEqual(0f, (float)MathfloatP.Cos((floatP)(Math.PI / 2.0)), _epsilon);
			Assert.AreEqual(-1f, (float)MathfloatP.Cos((floatP)Math.PI), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Tan is sine over cosine, not the reciprocal.
		// RCR: MathfloatP.cs Tan - `Sin(x) / Cos(x)` -> `Cos(x) / Sin(x)` -> RED (Tan(0) returns Infinity, expected 0).
		public void Tan_Works()
		{
			Assert.AreEqual(0f, (float)MathfloatP.Tan(floatP.Zero), _epsilon);
			Assert.AreEqual(1f, (float)MathfloatP.Tan((floatP)(Math.PI / 4.0)), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Sqrt returns NaN for negative inputs instead of a real root.
		// RCR: MathfloatP.cs Sqrt - negative-input `return floatP.NaN;` -> `return floatP.Zero;` -> RED (Sqrt(-1) is not NaN).
		public void Sqrt_Works()
		{
			Assert.AreEqual((floatP)2f, MathfloatP.Sqrt((floatP)4f));
			Assert.AreEqual(floatP.Zero, MathfloatP.Sqrt(floatP.Zero));
			Assert.IsTrue(MathfloatP.Sqrt((floatP)(-1f)).IsNaN());
		}

		[Test]
		// ADMIT: MathfloatP.Pow returns One for a zero exponent before any other special-casing.
		// RCR: MathfloatP.cs Pow - `iy == 0` early `return floatP.One;` -> `return floatP.Zero;` -> RED (Pow(10,0) returns 0).
		public void Pow_Works()
		{
			Assert.AreEqual((floatP)8f, MathfloatP.Pow((floatP)2f, (floatP)3f));
			Assert.AreEqual(floatP.One, MathfloatP.Pow((floatP)10f, floatP.Zero));
		}

		[TestCase(0f, 0f)]
		// ADMIT: MathfloatP.Pow2 squares its argument by delegating to Pow with exponent 2.
		// RCR: MathfloatP.cs Pow2 - `Pow(f, 2)` -> `Pow(f, 3)` -> RED (Pow2(2) returns 8, expected 4).
		[TestCase(1f, 1f)]
		[TestCase(2f, 4f)]
		[TestCase(10f, 100f)]
		[TestCase(-2f, 4f)]
		public void Pow2_KnownInputs_ReturnsSquare(float input, float expected)
		{
			Assert.AreEqual(expected, (float)MathfloatP.Pow2((floatP)input), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Exp rescales the reduced-range polynomial by 2^k with the same sign as the reduction step.
		// RCR: MathfloatP.cs Exp - `ScaleB(y, k)` -> `ScaleB(y, -k)` -> RED (Exp(1) returns ~0.68, expected e).
		public void Exp_Works()
		{
			Assert.AreEqual(1f, (float)MathfloatP.Exp(floatP.Zero), _epsilon);
			Assert.AreEqual((float)Math.E, (float)MathfloatP.Exp(floatP.One), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Log short-circuits ln(1) to exactly zero.
		// RCR: MathfloatP.cs Log - `ix == 0x3f800000` branch `return floatP.Zero;` -> `return floatP.One;` -> RED (Log(1) returns 1).
		public void Log_Works()
		{
			Assert.AreEqual(0f, (float)MathfloatP.Log(floatP.One), _epsilon);
			Assert.AreEqual(1f, (float)MathfloatP.Log((floatP)Math.E), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Floor biases negative inputs by the fraction mask before masking, so -1.1 floors to -2.
		// RCR: MathfloatP.cs Floor - negative branch `ui += m;` -> `ui -= m;` -> RED (Floor(-1.1) returns ~-0.55).
		public void Floor_Works()
		{
			Assert.AreEqual((floatP)1f, MathfloatP.Floor((floatP)1.9f));
			Assert.AreEqual((floatP)(-2f), MathfloatP.Floor((floatP)(-1.1f)));
		}

		[Test]
		// ADMIT: MathfloatP.Ceil biases positive inputs by the fraction mask before masking, so 1.1 ceils to 2.
		// RCR: MathfloatP.cs Ceil - positive branch `ui += m;` -> `ui -= m;` -> RED (Ceil(1.1) returns ~0.55, expected 2).
		public void Ceil_Works()
		{
			Assert.AreEqual((floatP)2f, MathfloatP.Ceil((floatP)1.1f));
			Assert.AreEqual((floatP)(-1f), MathfloatP.Ceil((floatP)(-1.9f)));
		}

		[Test]
		// ADMIT: MathfloatP.Round reconstructs the rounded value by adding x back to the TOINT residue.
		// RCR: MathfloatP.cs Round - else branch `y += x;` -> `y -= x;` -> RED (Round(1.6) returns -1.2, expected 2).
		public void Round_Works()
		{
			Assert.AreEqual((floatP)2f, MathfloatP.Round((floatP)1.6f));
			Assert.AreEqual((floatP)1f, MathfloatP.Round((floatP)1.4f));
		}

		[Test]
		// ADMIT: MathfloatP.Truncate clears the fraction bits rather than setting them, dropping the fractional part toward zero.
		// RCR: MathfloatP.cs Truncate - `i &= ~m;` -> `i |= m;` -> RED (Truncate(1.9) returns ~2.0, expected 1).
		public void Truncate_Works()
		{
			Assert.AreEqual((floatP)1f, MathfloatP.Truncate((floatP)1.9f));
			Assert.AreEqual((floatP)(-1f), MathfloatP.Truncate((floatP)(-1.9f)));
		}

		[Test]
		// ADMIT: MathfloatP.Hypothenuse sums the squares before taking the root.
		// RCR: MathfloatP.cs Hypothenuse - `x * x + y * y` -> `x * x - y * y` -> RED (Hypothenuse(3,4) returns NaN).
		public void Hypothenuse_Works()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Hypothenuse((floatP)3f, (floatP)4f));
		}

		[Test]
		// ADMIT: none - the test compares one in-process expression against itself.
		// RCR: OWED, not exempt - A3 reject: every edit to floatP/MathfloatP moves raw1 and raw2 together, so
		// `raw1 == raw2` pins C# purity, not package behaviour. Assert a hard-coded raw literal instead.
		public void Determinism_VerifyRawValues()
		{
			// Verify that basic operations produce identical raw values
			var a = (floatP)1.234f;
			var b = (floatP)5.678f;
			
			var res1 = a * b + MathfloatP.Sin(a);
			var raw1 = res1.RawValue;
			
			var res2 = a * b + MathfloatP.Sin(a);
			var raw2 = res2.RawValue;
			
			Assert.AreEqual(raw1, raw2);
		}

		#region Trigonometry Extended Tests

		[Test]
		// ADMIT: MathfloatP.Sin reduces the argument modulo 2pi, preserving the negative half of the cycle.
		// RCR: MathfloatP.cs Sin - range reduction `% 2pi` -> `% pi` -> RED (Sin(5pi/4) becomes positive).
		public void Sin_AllQuadrants()
		{
			// Quadrant 1 (0 to π/2)
			Assert.Greater((float)MathfloatP.Sin((floatP)(Math.PI / 4.0)), 0f);
			// Quadrant 2 (π/2 to π)
			Assert.Greater((float)MathfloatP.Sin((floatP)(3 * Math.PI / 4.0)), 0f);
			// Quadrant 3 (π to 3π/2)
			Assert.Less((float)MathfloatP.Sin((floatP)(5 * Math.PI / 4.0)), 0f);
			// Quadrant 4 (3π/2 to 2π)
			Assert.Less((float)MathfloatP.Sin((floatP)(7 * Math.PI / 4.0)), 0f);
		}

		[Test]
		// ADMIT: MathfloatP.Sin negates the mirrored result for arguments in (pi, 2pi], which is what makes Cos negative in quadrants 2 and 3.
		// RCR: MathfloatP.cs Sin - `negate = true;` -> `negate = false;` -> RED (Cos(3pi/4) comes back positive).
		public void Cos_AllQuadrants()
		{
			// Quadrant 1
			Assert.Greater((float)MathfloatP.Cos((floatP)(Math.PI / 4.0)), 0f);
			// Quadrant 2
			Assert.Less((float)MathfloatP.Cos((floatP)(3 * Math.PI / 4.0)), 0f);
			// Quadrant 3
			Assert.Less((float)MathfloatP.Cos((floatP)(5 * Math.PI / 4.0)), 0f);
			// Quadrant 4
			Assert.Greater((float)MathfloatP.Cos((floatP)(7 * Math.PI / 4.0)), 0f);
		}

		[Test]
		// ADMIT: MathfloatP.Sin mirrors arguments in (pi, 2pi] via 2pi - x before negating the result.
		// RCR: MathfloatP.cs Sin - mirror `2pi - x` -> `pi - x` -> RED (Sin(3pi/2) returns ~0.5, expected -1).
		public void Sin_3PiOver2_ReturnsMinusOne()
		{
			Assert.AreEqual(-1f, (float)MathfloatP.Sin((floatP)(3 * Math.PI / 2.0)), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Asin is pi/2 minus Acos.
		// RCR: MathfloatP.cs Asin - `pi/2 - Acos(x)` -> `pi/2 + Acos(x)` -> RED (Asin(0) returns pi, expected 0).
		public void Asin_Works()
		{
			Assert.AreEqual(0f, (float)MathfloatP.Asin(floatP.Zero), _epsilon);
			Assert.AreEqual((float)(Math.PI / 2), (float)MathfloatP.Asin(floatP.One), _epsilon);
			Assert.AreEqual((float)(-Math.PI / 2), (float)MathfloatP.Asin(floatP.MinusOne), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Acos short-circuits acos(1) to exactly zero.
		// RCR: MathfloatP.cs Acos - `|x| == 1` positive branch `return floatP.Zero;` -> `return floatP.One;` -> RED (Acos(1) returns 1).
		public void Acos_Works()
		{
			Assert.AreEqual((float)(Math.PI / 2), (float)MathfloatP.Acos(floatP.Zero), _epsilon);
			Assert.AreEqual(0f, (float)MathfloatP.Acos(floatP.One), _epsilon);
			Assert.AreEqual((float)Math.PI, (float)MathfloatP.Acos(floatP.MinusOne), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Atan selects the ATAN_HI table entry by the reduction index id, not a fixed slot.
		// RCR: MathfloatP.cs Atan - `ATAN_HI[id]` -> `ATAN_HI[0]` -> RED (Atan(1) returns ~0.464, expected pi/4).
		public void Atan_Works()
		{
			Assert.AreEqual(0f, (float)MathfloatP.Atan(floatP.Zero), _epsilon);
			Assert.AreEqual((float)(Math.PI / 4), (float)MathfloatP.Atan(floatP.One), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Atan2 reflects quadrant-2 results about pi, not pi/2.
		// RCR: MathfloatP.cs Atan2 - case 2 `RawPi - (z - PI_LO)` -> `RawPiOver2 - ...` -> RED (Atan2(1,-1) returns pi/4).
		public void Atan2_AllQuadrants()
		{
			// Quadrant 1
			var q1 = MathfloatP.Atan2((floatP)1f, (floatP)1f);
			Assert.AreEqual((float)(Math.PI / 4), (float)q1, _epsilon);
			// Quadrant 2
			var q2 = MathfloatP.Atan2((floatP)1f, (floatP)(-1f));
			Assert.AreEqual((float)(3 * Math.PI / 4), (float)q2, _epsilon);
			// Quadrant 3
			var q3 = MathfloatP.Atan2((floatP)(-1f), (floatP)(-1f));
			Assert.AreEqual((float)(-3 * Math.PI / 4), (float)q3, _epsilon);
			// Quadrant 4
			var q4 = MathfloatP.Atan2((floatP)(-1f), (floatP)1f);
			Assert.AreEqual((float)(-Math.PI / 4), (float)q4, _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Atan2 returns +pi/2 on the positive Y axis and -pi/2 on the negative Y axis when x is zero.
		// RCR: MathfloatP.cs Atan2 - `x == 0` sign ternary inverted -> RED (Atan2(1,0) returns -pi/2).
		public void Atan2_OnAxes()
		{
			// Positive X axis
			Assert.AreEqual(0f, (float)MathfloatP.Atan2(floatP.Zero, floatP.One), _epsilon);
			// Positive Y axis
			Assert.AreEqual((float)(Math.PI / 2), (float)MathfloatP.Atan2(floatP.One, floatP.Zero), _epsilon);
			// Negative X axis
			Assert.AreEqual((float)Math.PI, (float)MathfloatP.Atan2(floatP.Zero, floatP.MinusOne), _epsilon);
			// Negative Y axis
			Assert.AreEqual((float)(-Math.PI / 2), (float)MathfloatP.Atan2(floatP.MinusOne, floatP.Zero), _epsilon);
		}

		#endregion

		#region Power/Exp/Log Extended Tests

		[Test]
		// ADMIT: MathfloatP.Pow carries a negative result sign when the base is negative and the exponent is an odd integer.
		// RCR: MathfloatP.cs Pow - odd-exponent `sn = -floatP.One;` -> `sn = floatP.One;` -> RED (Pow(-2,3) returns 8).
		public void Pow_NegativeBase_IntExponent()
		{
			Assert.AreEqual((floatP)(-8f), MathfloatP.Pow((floatP)(-2f), (floatP)3f));
			Assert.AreEqual((floatP)4f, MathfloatP.Pow((floatP)(-2f), (floatP)2f));
		}

		[Test]
		// ADMIT: MathfloatP.Pow returns x itself for y=+1 and the reciprocal for y=-1.
		// RCR: MathfloatP.cs Pow - `iy == 0x3f800000` ternary inverted -> RED (Pow(5,1) returns 0.2, expected 5).
		public void Pow_OnePower_ReturnsSame()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Pow((floatP)5f, floatP.One));
		}

		[Test]
		// ADMIT: MathfloatP.Log maps +-0 to negative infinity.
		// RCR: MathfloatP.cs Log - zero branch `NegativeInfinity` -> `PositiveInfinity` -> RED (Log(0) is not -Infinity).
		public void Log_Zero_ReturnsNegativeInfinity()
		{
			Assert.IsTrue(MathfloatP.Log(floatP.Zero).IsNegativeInfinity());
		}

		[Test]
		// ADMIT: MathfloatP.Log maps negative inputs to NaN rather than computing a value.
		// RCR: MathfloatP.cs Log - sign-bit branch `return floatP.NaN;` -> `return floatP.Zero;` -> RED (Log(-1) is not NaN).
		public void Log_Negative_ReturnsNaN()
		{
			Assert.IsTrue(MathfloatP.Log((floatP)(-1f)).IsNaN());
		}

		[Test]
		// ADMIT: MathfloatP.Log2 uses base 2 in the change-of-base division.
		// RCR: MathfloatP.cs Log2 - `Log(x, 2)` -> `Log(x, 3)` -> RED (Log2(2) returns ~0.63, expected 1).
		public void Log2_Works()
		{
			Assert.AreEqual(1f, (float)MathfloatP.Log2((floatP)2f), _epsilon);
			Assert.AreEqual(2f, (float)MathfloatP.Log2((floatP)4f), _epsilon);
			Assert.AreEqual(3f, (float)MathfloatP.Log2((floatP)8f), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Log10 uses base 10 in the change-of-base division.
		// RCR: MathfloatP.cs Log10 - `Log(x, 10)` -> `Log(x, 100)` -> RED (Log10(10) returns 0.5, expected 1).
		public void Log10_Works()
		{
			Assert.AreEqual(1f, (float)MathfloatP.Log10((floatP)10f), _epsilon);
			Assert.AreEqual(2f, (float)MathfloatP.Log10((floatP)100f), _epsilon);
			Assert.AreEqual(3f, (float)MathfloatP.Log10((floatP)1000f), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Sqrt reassembles the result with exponent bias 0x3f000000 after the bit-by-bit root loop.
		// RCR: MathfloatP.cs Sqrt - result bias `0x3f000000` -> `0x3f800000` -> RED (Sqrt(1) returns 2).
		public void Sqrt_One_ReturnsOne()
		{
			Assert.AreEqual(floatP.One, MathfloatP.Sqrt(floatP.One));
		}

		#endregion

		#region Rounding Extended Tests

		[Test]
		// ADMIT: MathfloatP.Floor short-circuits and returns x when no fraction bits are set.
		// RCR: MathfloatP.cs Floor - `(ui & m) == 0` early `return x;` -> `return floatP.Zero;` -> RED (Floor(5) returns 0).
		public void Floor_Integer_ReturnsSame()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Floor((floatP)5f));
			Assert.AreEqual((floatP)(-5f), MathfloatP.Floor((floatP)(-5f)));
		}

		[Test]
		// ADMIT: MathfloatP.Ceil short-circuits and returns x when no fraction bits are set.
		// RCR: MathfloatP.cs Ceil - `(ui & m) == 0` early `return x;` -> `return floatP.Zero;` -> RED (Ceil(5) returns 0).
		public void Ceil_Integer_ReturnsSame()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Ceil((floatP)5f));
			Assert.AreEqual((floatP)(-5f), MathfloatP.Ceil((floatP)(-5f)));
		}

		[Test]
		// ADMIT: MathfloatP.Round uses round-half-up, not banker's rounding, via the inclusive `<= -0.5` residue test.
		// RCR: MathfloatP.cs Round - `y <= -0.5f` -> `y < -0.5f` -> RED (Round(2.5) returns 2, expected 3).
		public void Round_HalfCases()
		{
			// Implementation uses round-half-up behavior (not banker's rounding)
			Assert.AreEqual((floatP)2f, MathfloatP.Round((floatP)1.5f));
			Assert.AreEqual((floatP)3f, MathfloatP.Round((floatP)2.5f));
		}

		[Test]
		// ADMIT: MathfloatP.Round restores the original sign only for negative inputs.
		// RCR: MathfloatP.cs Round - sign restore ternary inverted -> RED (Round(5) returns -5).
		public void Round_Integer_ReturnsSame()
		{
			Assert.AreEqual((floatP)5f, MathfloatP.Round((floatP)5f));
		}

		#endregion

		#region Utility Extended Tests

		[Test]
		// ADMIT: MathfloatP.LerpAngle advances from a along the shortest signed arc by t.
		// RCR: MathfloatP.cs LerpAngle - `a + delta * Clamp01(t)` -> `a - ...` -> RED (0->90 at 0.5 returns -45).
		public void LerpAngle_Works()
		{
			// 0 to 90 degrees at 0.5
			Assert.AreEqual(45f, (float)MathfloatP.LerpAngle((floatP)0f, (floatP)90f, (floatP)0.5f), _epsilon);
			// Wrapping across 360: LerpAngle takes the shortest path but doesn't normalize the result
			// delta = DeltaAngle(350, 20) = 30 (shortest path), result = 350 + 30 * 0.5 = 365
			Assert.AreEqual(365f, (float)MathfloatP.LerpAngle((floatP)350f, (floatP)20f, (floatP)0.5f), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.MoveTowardsAngle rebases target to current + shortest-arc delta before delegating to MoveTowards.
		// RCR: MathfloatP.cs MoveTowardsAngle - `current + delta` -> `current - delta` -> RED (0->10 by 5 returns -5).
		public void MoveTowardsAngle_Works()
		{
			Assert.AreEqual(5f, (float)MathfloatP.MoveTowardsAngle((floatP)0f, (floatP)10f, (floatP)5f), _epsilon);
			Assert.AreEqual(10f, (float)MathfloatP.MoveTowardsAngle((floatP)0f, (floatP)10f, (floatP)15f), _epsilon);
		}

		[Test]
		// ADMIT: MathfloatP.Approximately reports equality when the difference is BELOW the relative tolerance.
		// RCR: MathfloatP.cs Approximately - `diff < tolerance` -> `diff > tolerance` -> RED (1.0 vs 1.000001 reported unequal).
		public void Approximately_Works()
		{
			Assert.IsTrue(MathfloatP.Approximately((floatP)1.0f, (floatP)1.000001f));
			Assert.IsFalse(MathfloatP.Approximately((floatP)1.0f, (floatP)1.1f));
		}

		#endregion

		#region Min/Max Extended Tests

		[Test]
		// ADMIT: MathfloatP.Max orders by `>` so +Infinity wins and -Infinity loses.
		// RCR: MathfloatP.cs Max - `if (val1 > val2)` -> `if (val1 < val2)` -> RED (Max(+Inf,100) returns 100).
		public void Max_WithInfinity()
		{
			Assert.IsTrue(MathfloatP.Max(floatP.PositiveInfinity, (floatP)100f).IsPositiveInfinity());
			Assert.AreEqual((floatP)100f, MathfloatP.Max((floatP)100f, floatP.NegativeInfinity));
		}

		[Test]
		// ADMIT: MathfloatP.Min orders by `<` so -Infinity wins and +Infinity loses.
		// RCR: MathfloatP.cs Min - `if (val1 < val2)` -> `if (val1 > val2)` -> RED (Min(-Inf,-100) returns -100).
		public void Min_WithInfinity()
		{
			Assert.IsTrue(MathfloatP.Min(floatP.NegativeInfinity, (floatP)(-100f)).IsNegativeInfinity());
			Assert.AreEqual((floatP)(-100f), MathfloatP.Min((floatP)(-100f), floatP.PositiveInfinity));
		}

		#endregion

		#region Abs Extended Tests

		[Test]
		// ADMIT: MathfloatP.Abs routes infinities through the sign-stripping branch, not the NaN passthrough.
		// RCR: MathfloatP.cs Abs - drop `|| f.IsInfinity()` from the guard -> RED (Abs(-Infinity) stays negative).
		public void Abs_Infinity()
		{
			Assert.IsTrue(MathfloatP.Abs(floatP.NegativeInfinity).IsPositiveInfinity());
			Assert.IsTrue(MathfloatP.Abs(floatP.PositiveInfinity).IsPositiveInfinity());
		}

		#endregion

		#region Clamp Extended Tests

		[Test]
		// ADMIT: MathfloatP.Clamp passes a value equal to either bound through untouched.
		// RCR: MathfloatP.cs Clamp - fall-through `return value;` -> `return min;` -> RED (Clamp(10,0,10) returns 0).
		public void Clamp_AtBoundaries()
		{
			Assert.AreEqual((floatP)0f, MathfloatP.Clamp((floatP)0f, (floatP)0f, (floatP)10f));
			Assert.AreEqual((floatP)10f, MathfloatP.Clamp((floatP)10f, (floatP)0f, (floatP)10f));
		}

		#endregion

		#region Determinism Extended Tests

		[Test]
		// ADMIT: none - Sin/Cos/Tan are each called twice on the same input and compared to themselves.
		// RCR: OWED, not exempt - A3 reject: any mutation changes both calls identically, so no production edit
		// can redden it. Assert hard-coded expected RawValues to make it falsifiable.
		public void AllTrigFunctions_RawValueConsistent()
		{
			var input = (floatP)0.5f;
			
			var sin1 = MathfloatP.Sin(input).RawValue;
			var sin2 = MathfloatP.Sin(input).RawValue;
			Assert.AreEqual(sin1, sin2);
			
			var cos1 = MathfloatP.Cos(input).RawValue;
			var cos2 = MathfloatP.Cos(input).RawValue;
			Assert.AreEqual(cos1, cos2);
			
			var tan1 = MathfloatP.Tan(input).RawValue;
			var tan2 = MathfloatP.Tan(input).RawValue;
			Assert.AreEqual(tan1, tan2);
		}

		[Test]
		// ADMIT: none - Sqrt/Log/Exp are each called twice on the same input and compared to themselves.
		// RCR: OWED, not exempt - A3 reject: any mutation changes both calls identically, so no production edit
		// can redden it. Assert hard-coded expected RawValues to make it falsifiable.
		public void AllPowerFunctions_RawValueConsistent()
		{
			var input = (floatP)2.5f;
			
			var sqrt1 = MathfloatP.Sqrt(input).RawValue;
			var sqrt2 = MathfloatP.Sqrt(input).RawValue;
			Assert.AreEqual(sqrt1, sqrt2);
			
			var log1 = MathfloatP.Log(input).RawValue;
			var log2 = MathfloatP.Log(input).RawValue;
			Assert.AreEqual(log1, log2);
			
			var exp1 = MathfloatP.Exp(input).RawValue;
			var exp2 = MathfloatP.Exp(input).RawValue;
			Assert.AreEqual(exp1, exp2);
		}

		[Test]
		// ADMIT: none - the name promises cross-platform determinism; the body compares two evaluations of
		// the same expression in one process.
		// RCR: OWED, not exempt - D2 overclaim: no production edit can separate result1 from result2. Strengthen
		// by asserting the expected RawValue literal, or rename to what it actually checks.
		public void CrossPlatform_Determinism_ComplexExpression()
		{
			// This test verifies that a complex expression produces the same raw value
			var a = (floatP)1.5f;
			var b = (floatP)2.5f;
			var c = (floatP)3.5f;
			
			var result1 = MathfloatP.Sin(a) * MathfloatP.Cos(b) + MathfloatP.Sqrt(c);
			var result2 = MathfloatP.Sin(a) * MathfloatP.Cos(b) + MathfloatP.Sqrt(c);
			
			Assert.AreEqual(result1.RawValue, result2.RawValue);
		}

		#endregion

		#region ScaleB Tests

		[Test]
		// ADMIT: MathfloatP.ScaleB multiplies by 2^n by adding n to the exponent bias.
		// RCR: MathfloatP.cs ScaleB - `0x7f + n` -> `0x7f - n` -> RED (ScaleB(1,2) returns 0.25, expected 4).
		public void ScaleB_PowerOfTwo()
		{
			Assert.AreEqual((floatP)4f, MathfloatP.ScaleB(floatP.One, 2));
			Assert.AreEqual((floatP)8f, MathfloatP.ScaleB(floatP.One, 3));
			Assert.AreEqual((floatP)0.5f, MathfloatP.ScaleB(floatP.One, -1));
		}

		#endregion

		#region Edge Case Tests

		[Test]
		// ADMIT: MathfloatP.Lerp clamps t to [0,1] before interpolating, so a negative t yields a.
		// RCR: MathfloatP.cs Lerp - `Clamp01(t)` -> `Clamp(t, MinusOne, One)` -> RED (Lerp(0,10,-0.5) returns -5).
		public void Lerp_NegativeT_Clamped()
		{
			Assert.AreEqual((floatP)0f, MathfloatP.Lerp((floatP)0f, (floatP)10f, (floatP)(-0.5f)));
		}

		[Test]
		// ADMIT: MathfloatP.InverseLerp clamps its result to [0,1] for values outside [a,b].
		// RCR: MathfloatP.cs InverseLerp - drop the `Clamp01(...)` wrapper -> RED (value -5 returns -0.5, expected 0).
		public void InverseLerp_OutOfRange()
		{
			// Value below range
			Assert.AreEqual((floatP)0f, MathfloatP.InverseLerp((floatP)0f, (floatP)10f, (floatP)(-5f)));
			// Value above range
			Assert.AreEqual((floatP)1f, MathfloatP.InverseLerp((floatP)0f, (floatP)10f, (floatP)15f));
		}

		[Test]
		// ADMIT: MathfloatP.SmoothStep clamps t to [0,1] before the Hermite curve, so out-of-range t saturates at from/to.
		// RCR: MathfloatP.cs SmoothStep - `Clamp01(t)` -> `Clamp(t, MinusOne, 2f)` -> RED (t=-0.5 gives 10, expected 0).
		public void SmoothStep_ClampsInput()
		{
			Assert.AreEqual((floatP)0f, MathfloatP.SmoothStep((floatP)0f, (floatP)10f, (floatP)(-0.5f)));
			Assert.AreEqual((floatP)10f, MathfloatP.SmoothStep((floatP)0f, (floatP)10f, (floatP)1.5f));
		}

		#endregion

		#region Audit follow-up: untested-API stubs (Medium confidence)

		[Test]
		// ADMIT: MathfloatP.DivRem negates the quotient only when the operand signs differ.
		// RCR: MathfloatP.cs DivRem - quotient sign ternary inverted -> RED (DivRem(7,3) yields quotient -2, expected 2).
		public void DivRem_KnownPair_ReturnsExpectedRemainderAndQuotient()
		{
			// 7 / 3 = 2 remainder 1
			MathfloatP.DivRem((floatP)7f, (floatP)3f, out var rem, out var quo);
			Assert.AreEqual(2, quo);
			Assert.AreEqual(1f, (float)rem, _epsilon);

			// x = 0: remainder = x, quotient = 0
			MathfloatP.DivRem(floatP.Zero, (floatP)3f, out var rem0, out var quo0);
			Assert.AreEqual(0, quo0);
			Assert.AreEqual(0f, (float)rem0, _epsilon);

			// y = 0: NaN remainder, quotient = 0
			MathfloatP.DivRem((floatP)5f, floatP.Zero, out var remNaN, out var quoNaN);
			Assert.AreEqual(0, quoNaN);
			Assert.IsTrue(remNaN.IsNaN());
		}

		[Test]
		// ADMIT: MathfloatP.IEEERemainder forwards DivRem's remainder unchanged.
		// RCR: MathfloatP.cs IEEERemainder - `return remainder;` -> `return -remainder;` -> RED (IEEERemainder(7,3) returns -1).
		public void IEEERemainder_KnownPair_MatchesDivRemRemainder()
		{
			var rem = MathfloatP.IEEERemainder((floatP)7f, (floatP)3f);
			Assert.AreEqual(1f, (float)rem, _epsilon);

			MathfloatP.DivRem((floatP)7f, (floatP)3f, out var divRemRem, out _);
			Assert.AreEqual(divRemRem.RawValue, rem.RawValue);
		}

		[Test]
		// ADMIT: MathfloatP.Mod returns exact zero when a reduction step divides evenly (10 mod 5).
		// RCR: MathfloatP.cs Mod - in-loop exact-division `return floatP.Zero;` -> `return floatP.One;` -> RED (Mod(10,5) returns 1).
		public void Mod_KnownPair_ReturnsExpectedRemainder()
		{
			Assert.AreEqual(1f, (float)MathfloatP.Mod((floatP)10f, (floatP)3f), _epsilon);
			Assert.AreEqual(0f, (float)MathfloatP.Mod((floatP)10f, (floatP)5f), _epsilon);

			// |x| < |y|: returns x unchanged
			Assert.AreEqual(((floatP)2f).RawValue, MathfloatP.Mod((floatP)2f, (floatP)5f).RawValue);

			// Divide-by-zero: NaN
			Assert.IsTrue(MathfloatP.Mod((floatP)5f, floatP.Zero).IsNaN());
		}

		[Test]
		// ADMIT: MathfloatP.Log(x, e) propagates a NaN base straight through before any domain checks.
		// RCR: MathfloatP.cs Log(x,e) - NaN-base `return e;` -> `return floatP.Zero;` -> RED (Log(5,NaN) is not NaN).
		public void LogTwoArg_BaseAndDomain_HandlesSpecialCases()
		{
			// Log(8, 2) ≈ 3
			Assert.AreEqual(3f, (float)MathfloatP.Log((floatP)8f, (floatP)2f), 0.01f);

			// Log(x, 1) → NaN
			Assert.IsTrue(MathfloatP.Log((floatP)5f, (floatP)1f).IsNaN());

			// Log(x != 1, 0) → NaN
			Assert.IsTrue(MathfloatP.Log((floatP)5f, floatP.Zero).IsNaN());

			// NaN x preserved
			Assert.IsTrue(MathfloatP.Log(floatP.NaN, (floatP)2f).IsNaN());

			// NaN base preserved
			Assert.IsTrue(MathfloatP.Log((floatP)5f, floatP.NaN).IsNaN());
		}

		[Test]
		// ADMIT: MathfloatP.RawDistance counts representable steps as the absolute DIFFERENCE of the ordered raw keys.
		// RCR: MathfloatP.cs RawDistance - `val1 - val2` -> `val1 + val2` -> RED (RawDistance(1,1) returns 2130706432, expected 0).
		public void RawDistance_FiniteAndInfinite_ReturnsExpected()
		{
			// Same value → 0
			Assert.AreEqual(0L, MathfloatP.RawDistance((floatP)1f, (floatP)1f));

			// Adjacent floatPs → 1
			var oneRaw = ((floatP)1f).RawValue;
			var nextAfterOne = floatP.FromIeeeRaw(oneRaw + 1u);
			Assert.AreEqual(1L, MathfloatP.RawDistance((floatP)1f, nextAfterOne));

			// Equal infinities → 0
			Assert.AreEqual(0L, MathfloatP.RawDistance(floatP.PositiveInfinity, floatP.PositiveInfinity));

			// Mixed finite + infinity → long.MaxValue
			Assert.AreEqual(long.MaxValue, MathfloatP.RawDistance((floatP)1f, floatP.PositiveInfinity));
		}

		#endregion
	}
}
