using System;

namespace GameLovers.GameData
{
	/// <summary>
	/// Math calculation for the deterministic floating point <see cref="floatP"/>
	/// </summary>
	/// <author>
	/// https://github.com/Kimbatt/unity-deterministic-physics/tree/master/Assets/Scripts/SoftFloat/sfloat/libm
	/// </author>
	public static class MathfloatP
	{
		private const uint RawPi = 0x40490fdb; // 3.1415926535897932384626433832795
		private const uint RawPiOver2 = 0x3fc90fdb; // 1.5707963267948966192313216916398
		private const uint RawPiOver4 = 0x3f490fdb; // 0.78539816339744830961566084581988
		private const uint Raw2Pi = 0x40c90fdb; // 6.283185307179586476925286766559
		private const uint Raw3PiOver4 = 0x4016cbe4; // 2.3561944901923449288469825374596

		/// <summary>
		/// Returns the absolute value of the given floatP number
		/// </summary>
		public static floatP Abs(floatP f)
		{
			if (f.RawExponent != 255 || f.IsInfinity())
			{
				return new floatP(f.RawValue & 0x7FFFFFFF);
			}
			else
			{
				// Leave NaN untouched
				return f;
			}
		}

		/// <summary>
		/// Returns the maximum of the two given floatP values. Returns NaN iff either argument is NaN.
		/// </summary>
		public static floatP Max(floatP val1, floatP val2)
		{
			if (val1 > val2)
			{
				return val1;
			}
			else if (val1.IsNaN())
			{
				return val1;
			}
			else
			{
				return val2;
			}
		}

		/// <summary>
		/// Returns the minimum of the two given floatP values. Returns NaN iff either argument is NaN.
		/// </summary>
		public static floatP Min(floatP val1, floatP val2)
		{
			if (val1 < val2)
			{
				return val1;
			}
			else if (val1.IsNaN())
			{
				return val1;
			}
			else
			{
				return val2;
			}
		}

		/// <summary>
		/// Clamps the given value between the given minimum and maximum values.
		/// Returns the given value if it is within the range.
		/// Returns min if the value is less than min.
		/// Returns max if the value is greater than max.
		/// </summary>
		public static floatP Clamp(floatP value, floatP min, floatP max)
		{
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		/// <summary>
		/// Clamps the given value between 0 and 1.
		/// </summary>
		public static floatP Clamp01(floatP value)
		{
			if (value < floatP.Zero)
			{
				return floatP.Zero;
			}
			if (value > floatP.One)
			{
				return floatP.One;
			}
			return value;
		}

		/// <summary>
		/// Linearly interpolates between a and b by t.
		/// t is clamped to the range [0, 1].
		/// </summary>
		public static floatP Lerp(floatP a, floatP b, floatP t)
		{
			t = Clamp01(t);
			return a + (b - a) * t;
		}

		/// <summary>
		/// Linearly interpolates between a and b by t.
		/// t is not clamped.
		/// </summary>
		public static floatP LerpUnclamped(floatP a, floatP b, floatP t)
		{
			return a + (b - a) * t;
		}

		/// <summary>
		/// Interpolates between a and b by t with smoothing at the limits.
		/// </summary>
		public static floatP SmoothStep(floatP from, floatP to, floatP t)
		{
			t = Clamp01(t);
			t = (floatP)(-2.0f) * t * t * t + (floatP)3.0f * t * t;
			return to * t + from * (floatP.One - t);
		}

		/// <summary>
		/// Calculates the linear parameter t that produces the interpolant value within the range [a, b].
		/// </summary>
		public static floatP InverseLerp(floatP a, floatP b, floatP value)
		{
			if (a != b)
			{
				return Clamp01((value - a) / (b - a));
			}
			return floatP.Zero;
		}

		/// <summary>
		/// Moves a value current towards target.
		/// </summary>
		public static floatP MoveTowards(floatP current, floatP target, floatP maxDelta)
		{
			if (Abs(target - current) <= maxDelta)
			{
				return target;
			}
			return current + Sign(target - current) * maxDelta;
		}

		/// <summary>
		/// Returns the sign of f.
		/// Returns 1 if f is positive or zero, -1 if f is negative.
		/// </summary>
		public static int Sign(floatP f)
		{
			return f >= floatP.Zero ? 1 : -1;
		}

		/// <summary>
		/// Loops the value t, so that it is never larger than length and never smaller than 0.
		/// </summary>
		public static floatP Repeat(floatP t, floatP length)
		{
			return Clamp(t - Floor(t / length) * length, floatP.Zero, length);
		}

		/// <summary>
		/// Ping-pongs the value t, so that it is never larger than length and never smaller than 0.
		/// </summary>
		public static floatP PingPong(floatP t, floatP length)
		{
			t = Repeat(t, length * (floatP)2.0f);
			return length - Abs(t - length);
		}

		/// <summary>
		/// Calculates the shortest difference between two given angles given in degrees.
		/// </summary>
		public static floatP DeltaAngle(floatP current, floatP target)
		{
			floatP delta = Repeat(target - current, (floatP)360.0f);
			if (delta > (floatP)180.0f)
			{
				delta -= (floatP)360.0f;
			}
			return delta;
		}

		/// <summary>
		/// Same as Lerp but makes sure the values interpolate correctly when they wrap around 360 degrees.
		/// </summary>
		public static floatP LerpAngle(floatP a, floatP b, floatP t)
		{
			floatP delta = DeltaAngle(a, b);
			return a + delta * Clamp01(t);
		}

		/// <summary>
		/// Same as MoveTowards but makes sure the values interpolate correctly when they wrap around 360 degrees.
		/// </summary>
		public static floatP MoveTowardsAngle(floatP current, floatP target, floatP maxDelta)
		{
			floatP delta = DeltaAngle(current, target);
			if (-maxDelta < delta && delta < maxDelta)
			{
				return target;
			}
			target = current + delta;
			return MoveTowards(current, target, maxDelta);
		}

		/// <summary>
		/// Compares two floating point values and returns true if they are similar.
		/// </summary>
		public static bool Approximately(floatP a, floatP b)
		{
			// Similar to Unity's implementation:
			// Returns true if the difference between a and b is less than the larger of
			// 1E-06 * max(|a|, |b|) or Epsilon * 8
			floatP diff = Abs(b - a);
			floatP maxMagnitude = Max(Abs(a), Abs(b));
			floatP tolerance = Max((floatP)1E-06f * maxMagnitude, floatP.Epsilon * (floatP)8f);
			return diff < tolerance;
		}

		/// <summary>
		/// Returns the remainder and the quotient when dividing x by y, so that x == y * quotient + remainder
		/// </summary>
		/// <returns>
		/// The quotient
		/// </returns>
		public static int DivRem(floatP x, floatP y, out floatP remainder, out int quotient)
		{
			uint ux = x.RawValue;
			uint uy = y.RawValue;
			int ex = (int)((ux >> 23) & 0xff);
			int ey = (int)((uy >> 23) & 0xff);
			bool sx = (ux >> 31) != 0;
			bool sy = (uy >> 31) != 0;
			uint q;
			uint i;
			var uxi = ux;

			if ((uy << 1) == 0 || y.IsNaN() || ex == 0xff)
			{
				floatP m = (x * y);
				remainder = m / m;
				quotient = 0;
				return quotient;
			}

			if ((ux << 1) == 0)
			{
				remainder = x;
				quotient = 0;
				return quotient;
			}

			/* normalize x and y */
			if (ex == 0)
			{
				i = uxi << 9;
				while ((i >> 31) == 0)
				{
					ex -= 1;
					i <<= 1;
				}

				uxi <<= -ex + 1;
			}
			else
			{
				uxi &= (~0u) >> 9;
				uxi |= 1 << 23;
			}

			if (ey == 0)
			{
				i = uy << 9;
				while ((i >> 31) == 0)
				{
					ey -= 1;
					i <<= 1;
				}

				uy <<= -ey + 1;
			}
			else
			{
				uy &= (~0u) >> 9;
				uy |= 1 << 23;
			}

			q = 0;
			if (ex + 1 != ey)
			{
				if (ex < ey)
				{
					remainder = x;
					quotient = 0;
					return quotient;
				}

				/* x mod y */
				while (ex > ey)
				{
					i = uxi - uy;
					if ((i >> 31) == 0)
					{
						uxi = i;
						q += 1;
					}

					uxi <<= 1;
					q <<= 1;
					ex -= 1;
				}

				i = uxi - uy;
				if ((i >> 31) == 0)
				{
					uxi = i;
					q += 1;
				}

				if (uxi == 0)
				{
					ex = -30;
				}
				else
				{
					while ((uxi >> 23) == 0)
					{
						uxi <<= 1;
						ex -= 1;
					}
				}
			}

			/* scale result and decide between |x| and |x|-|y| */
			if (ex > 0)
			{
				uxi -= 1 << 23;
				uxi |= ((uint)ex) << 23;
			}
			else
			{
				uxi >>= -ex + 1;
			}

			x = floatP.FromRaw(uxi);
			if (sy)
			{
				y = -y;
			}

			if ((ex == ey || (ex + 1 == ey && ((floatP)2.0f * x > y || ((floatP)2.0f * x == y && (q % 2) != 0)))) && x > y)
			{
				x -= y;
				q += 1;
			}

			q &= 0x7fffffff;
			int quo = sx ^ sy ? -(int)q : (int)q;
			remainder = sx ? -x : x;
			quotient = quo;

			return quotient;
		}

		/// <summary>
		/// Returns the remainder when dividing x by y
		/// </summary>
		public static floatP IEEERemainder(floatP x, floatP y)
		{
			DivRem(x, y, out floatP remainder, out _);
			return remainder;
		}

		/// <summary>
		/// Returns x modulo y
		/// </summary>
		public static floatP Mod(floatP x, floatP y)
		{
			uint uxi = x.RawValue;
			uint uyi = y.RawValue;
			int ex = (int)(uxi >> 23 & 0xff);
			int ey = (int)(uyi >> 23 & 0xff);
			uint sx = uxi & 0x80000000;
			uint i;

			if (uyi << 1 == 0 || y.IsNaN() || ex == 0xff)
			{
				return (x * y) / (x * y);
			}

			if (uxi << 1 <= uyi << 1)
			{
				if (uxi << 1 == uyi << 1)
				{
					//return 0.0 * x;
					return floatP.Zero;
				}

				return x;
			}

			/* normalize x and y */
			if (ex == 0)
			{
				i = uxi << 9;
				while (i >> 31 == 0)
				{
					ex -= 1;
					i <<= 1;
				}

				uxi <<= -ex + 1;
			}
			else
			{
				uxi &= uint.MaxValue >> 9;
				uxi |= 1 << 23;
			}

			if (ey == 0)
			{
				i = uyi << 9;
				while (i >> 31 == 0)
				{
					ey -= 1;
					i <<= 1;
				}

				uyi <<= -ey + 1;
			}
			else
			{
				uyi &= uint.MaxValue >> 9;
				uyi |= 1 << 23;
			}

			/* x mod y */
			while (ex > ey)
			{
				i = uxi - uyi;
				if (i >> 31 == 0)
				{
					if (i == 0)
					{
						//return 0.0 * x;
						return floatP.Zero;
					}

					uxi = i;
				}

				uxi <<= 1;

				ex -= 1;
			}

			i = uxi - uyi;
			if (i >> 31 == 0)
			{
				if (i == 0)
				{
					//return 0.0 * x;
					return floatP.Zero;
				}

				uxi = i;
			}

			while (uxi >> 23 == 0)
			{
				uxi <<= 1;
				ex -= 1;
			}

			/* scale result up */
			if (ex > 0)
			{
				uxi -= 1 << 23;
				uxi |= ((uint)ex) << 23;
			}
			else
			{
				uxi >>= -ex + 1;
			}

			uxi |= sx;
			return floatP.FromRaw(uxi);
		}

		/// <summary>
		/// Rounds x to the nearest integer
		/// </summary>
		public static floatP Round(floatP x)
		{
			floatP TOINT = (floatP)8388608.0f;

			uint i = x.RawValue;
			uint e = i >> 23 & 0xff;
			floatP y;

			if (e >= 0x7f + 23)
			{
				return x;
			}

			if (e < 0x7f - 1)
			{
				//force_eval!(x + TOINT);
				//return 0.0 * x;
				return floatP.Zero;
			}

			if (i >> 31 != 0)
			{
				x = -x;
			}

			y = x + TOINT - TOINT - x;

			if (y > (floatP)0.5f)
			{
				y = y + x - floatP.One;
			}
			else if (y <= (floatP)(-0.5f))
			{
				y = y + x + floatP.One;
			}
			else
			{
				y += x;
			}

			return i >> 31 != 0 ? -y : y;
		}

		/// <summary>
		/// Rounds x down to the nearest integer
		/// </summary>
		public static floatP Floor(floatP x)
		{
			uint ui = x.RawValue;
			int e = (((int)(ui >> 23)) & 0xff) - 0x7f;

			if (e >= 23)
			{
				return x;
			}

			if (e >= 0)
			{
				uint m = 0x007fffffu >> e;
				if ((ui & m) == 0)
				{
					return x;
				}
				if (ui >> 31 != 0)
				{
					ui += m;
				}
				ui &= ~m;
			}
			else
			{
				if (ui >> 31 == 0)
				{
					ui = 0;
				}
				else if (ui << 1 != 0)
				{
					return (floatP)(-1.0f);
				}
			}

			return floatP.FromRaw(ui);
		}

		/// <summary>
		/// Rounds x up to the nearest integer
		/// </summary>
		public static floatP Ceil(floatP x)
		{
			uint ui = x.RawValue;
			int e = (int)(((ui >> 23) & 0xff) - (0x7f));

			if (e >= 23)
			{
				return x;
			}

			if (e >= 0)
			{
				uint m = 0x007fffffu >> e;
				if ((ui & m) == 0)
				{
					return x;
				}
				if (ui >> 31 == 0)
				{
					ui += m;
				}
				ui &= ~m;
			}
			else
			{
				if (ui >> 31 != 0)
				{
					return (floatP)(-0.0f);
				}
				else if (ui << 1 != 0)
				{
					return floatP.One;
				}
			}

			return floatP.FromRaw(ui);
		}

		/// <summary>
		/// Truncates x, removing its fractional parts
		/// </summary>
		public static floatP Truncate(floatP x)
		{
			uint i = x.RawValue;
			int e = (int)(i >> 23 & 0xff) - 0x7f + 9;
			uint m;

			if (e >= 23 + 9)
			{
				return x;
			}

			if (e < 9)
			{
				e = 1;
			}

			m = unchecked((uint)-1) >> e;
			if ((i & m) == 0)
			{
				return x;
			}

			i &= ~m;
			return floatP.FromRaw(i);
		}

		/// <summary>
		/// Returns the square root of x
		/// </summary>
		public static floatP Sqrt(floatP f)
		{
			int sign = unchecked((int)0x80000000);
			int ix;
			int s;
			int q;
			int m;
			int t;
			int i;
			uint r;

			ix = (int)f.RawValue;

			/* take care of Inf and NaN */
			if (((uint)ix & 0x7f800000) == 0x7f800000)
			{
				//return x * x + x; /* sqrt(NaN)=NaN, sqrt(+inf)=+inf, sqrt(-inf)=sNaN */
				if (f.IsNaN() || f.IsNegativeInfinity())
				{
					return floatP.NaN;
				}
				else // if (x.IsPositiveInfinity())
				{
					return floatP.PositiveInfinity;
				}
			}

			/* take care of zero */
			if (ix <= 0)
			{
				if ((ix & ~sign) == 0)
				{
					return f; /* sqrt(+-0) = +-0 */
				}

				if (ix < 0)
				{
					//return (x - x) / (x - x); /* sqrt(-ve) = sNaN */
					return floatP.NaN;
				}
			}

			/* normalize x */
			m = ix >> 23;
			if (m == 0)
			{
				/* subnormal x */
				i = 0;
				while ((ix & 0x00800000) == 0)
				{
					ix <<= 1;
					i += 1;
				}

				m -= i - 1;
			}

			m -= 127; /* unbias exponent */
			ix = (ix & 0x007fffff) | 0x00800000;
			if ((m & 1) == 1)
			{
				/* odd m, double x to make it even */
				ix += ix;
			}

			m >>= 1; /* m = [m/2] */

			/* generate sqrt(x) bit by bit */
			ix += ix;
			q = 0;
			s = 0;
			r = 0x01000000; /* r = moving bit from right to left */

			while (r != 0)
			{
				t = s + (int)r;
				if (t <= ix)
				{
					s = t + (int)r;
					ix -= t;
					q += (int)r;
				}

				ix += ix;
				r >>= 1;
			}

			/* use floating add to find out rounding direction */
			if (ix != 0)
			{
				q += q & 1;
			}

			ix = (q >> 1) + 0x3f000000;
			ix += m << 23;
			return floatP.FromRaw((uint)ix);
		}

		/// <summary>
		/// Returns e raised to the power x (e ~= 2.71828182845904523536)
		/// </summary>
		public static floatP Exp(floatP x)
		{
			const uint LN2_HI_U32 = 0x3f317200; // 6.9314575195e-01
			const uint LN2_LO_U32 = 0x35bfbe8e; // 1.4286067653e-06
			const uint INV_LN2_U32 = 0x3fb8aa3b; // 1.4426950216e+00

			const uint P1_U32 = 0x3e2aaa8f; // 1.6666625440e-1 /*  0xaaaa8f.0p-26 */
			const uint P2_U32 = 0xbb355215; // -2.7667332906e-3 /* -0xb55215.0p-32 */

			floatP x1p127 = floatP.FromRaw(0x7f000000); // 0x1p127f === 2 ^ 127
			floatP x1p_126 = floatP.FromRaw(0x800000); // 0x1p-126f === 2 ^ -126  /*original 0x1p-149f    ??????????? */
			uint hx = x.RawValue;
			int sign = (int)(hx >> 31); /* sign bit of x */
			bool signb = sign != 0;
			hx &= 0x7fffffff; /* high word of |x| */

			/* special cases */
			if (hx >= 0x42aeac50)
			{
				/* if |x| >= -87.33655f or NaN */
				if (hx > 0x7f800000)
				{
					/* NaN */
					return x;
				}

				if (hx >= 0x42b17218 && !signb)
				{
					/* x >= 88.722839f */
					/* overflow */
					x *= x1p127;
					return x;
				}

				if (signb)
				{
					/* underflow */
					//force_eval!(-x1p_126 / x);

					if (hx >= 0x42cff1b5)
					{
						/* x <= -103.972084f */
						return floatP.Zero;
					}
				}
			}

			/* argument reduction */
			int k;
			floatP hi;
			floatP lo;
			if (hx > 0x3eb17218)
			{
				/* if |x| > 0.5 ln2 */
				if (hx > 0x3f851592)
				{
					/* if |x| > 1.5 ln2 */
					k = (int)(floatP.FromRaw(INV_LN2_U32) * x + (signb ? (floatP)0.5f : (floatP)(-0.5f)));
				}
				else
				{
					k = 1 - sign - sign;
				}

				floatP kf = (floatP)k;
				hi = x - kf * floatP.FromRaw(LN2_HI_U32); /* k*ln2hi is exact here */
				lo = kf * floatP.FromRaw(LN2_LO_U32);
				x = hi - lo;
			}
			else if (hx > 0x39000000)
			{
				/* |x| > 2**-14 */
				k = 0;
				hi = x;
				lo = floatP.Zero;
			}
			else
			{
				/* raise inexact */
				//force_eval!(x1p127 + x);
				return floatP.One + x;
			}

			/* x is now in primary range */
			floatP xx = x * x;
			floatP c = x - xx * (floatP.FromRaw(P1_U32) + xx * floatP.FromRaw(P2_U32));
			floatP y = floatP.One + (x * c / ((floatP)2.0f - c) - lo + hi);
			return k == 0 ? y : ScaleB(y, k);
		}

		/// <summary>
		/// Returns x raised to the power y
		/// </summary>
		public static floatP Pow(floatP x, floatP y)
		{
			const uint BP_0_U32 = 0x3f800000; /* 1.0 */
			const uint BP_1_U32 = 0x3fc00000; /* 1.5 */
			const uint DP_H_0_U32 = 0x00000000; /* 0.0 */
			const uint DP_H_1_U32 = 0x3f15c000; /* 5.84960938e-01 */
			const uint DP_L_0_U32 = 0x00000000; /* 0.0 */
			const uint DP_L_1_U32 = 0x35d1cfdc; /* 1.56322085e-06 */
			const uint TWO24_U32 = 0x4b800000; /* 16777216.0 */
			const uint HUGE_U32 = 0x7149f2ca; /* 1.0e30 */
			const uint TINY_U32 = 0x0da24260; /* 1.0e-30 */
			const uint L1_U32 = 0x3f19999a; /* 6.0000002384e-01 */
			const uint L2_U32 = 0x3edb6db7; /* 4.2857143283e-01 */
			const uint L3_U32 = 0x3eaaaaab; /* 3.3333334327e-01 */
			const uint L4_U32 = 0x3e8ba305; /* 2.7272811532e-01 */
			const uint L5_U32 = 0x3e6c3255; /* 2.3066075146e-01 */
			const uint L6_U32 = 0x3e53f142; /* 2.0697501302e-01 */
			const uint P1_U32 = 0x3e2aaaab; /* 1.6666667163e-01 */
			const uint P2_U32 = 0xbb360b61; /* -2.7777778450e-03 */
			const uint P3_U32 = 0x388ab355; /* 6.6137559770e-05 */
			const uint P4_U32 = 0xb5ddea0e; /* -1.6533901999e-06 */
			const uint P5_U32 = 0x3331bb4c; /* 4.1381369442e-08 */
			const uint LG2_U32 = 0x3f317218; /* 6.9314718246e-01 */
			const uint LG2_H_U32 = 0x3f317200; /* 6.93145752e-01 */
			const uint LG2_L_U32 = 0x35bfbe8c; /* 1.42860654e-06 */
			const uint OVT_U32 = 0x3338aa3c; /* 4.2995665694e-08 =-(128-log2(ovfl+.5ulp)) */
			const uint CP_U32 = 0x3f76384f; /* 9.6179670095e-01 =2/(3ln2) */
			const uint CP_H_U32 = 0x3f764000; /* 9.6191406250e-01 =12b cp */
			const uint CP_L_U32 = 0xb8f623c6; /* -1.1736857402e-04 =tail of cp_h */
			const uint IVLN2_U32 = 0x3fb8aa3b; /* 1.4426950216e+00 */
			const uint IVLN2_H_U32 = 0x3fb8aa00; /* 1.4426879883e+00 */
			const uint IVLN2_L_U32 = 0x36eca570; /* 7.0526075433e-06 */

			floatP z;
			floatP ax;
			floatP z_h;
			floatP z_l;
			floatP p_h;
			floatP p_l;
			floatP y1;
			floatP t1;
			floatP t2;
			floatP r;
			floatP s;
			floatP sn;
			floatP t;
			floatP u;
			floatP v;
			floatP w;
			int i;
			int j;
			int k;
			int yisint;
			int n;
			int hx;
			int hy;
			int ix;
			int iy;
			int iS;

			hx = (int)x.RawValue;
			hy = (int)y.RawValue;

			ix = hx & 0x7fffffff;
			iy = hy & 0x7fffffff;

			/* x**0 = 1, even if x is NaN */
			if (iy == 0)
			{
				return floatP.One;
			}

			/* 1**y = 1, even if y is NaN */
			if (hx == 0x3f800000)
			{
				return floatP.One;
			}

			/* NaN if either arg is NaN */
			if (ix > 0x7f800000 || iy > 0x7f800000)
			{
				return floatP.NaN;
			}

			/* determine if y is an odd int when x < 0
				* yisint = 0       ... y is not an integer
				* yisint = 1       ... y is an odd int
				* yisint = 2       ... y is an even int
				*/
			yisint = 0;
			if (hx < 0)
			{
				if (iy >= 0x4b800000)
				{
					yisint = 2; /* even integer y */
				}
				else if (iy >= 0x3f800000)
				{
					k = (iy >> 23) - 0x7f; /* exponent */
					j = iy >> (23 - k);
					if ((j << (23 - k)) == iy)
					{
						yisint = 2 - (j & 1);
					}
				}
			}

			/* special value of y */
			if (iy == 0x7f800000)
			{
				/* y is +-inf */
				if (ix == 0x3f800000)
				{
					/* (-1)**+-inf is 1 */
					return floatP.One;
				}
				else if (ix > 0x3f800000)
				{
					/* (|x|>1)**+-inf = inf,0 */
					return hy >= 0 ? y : floatP.Zero;
				}
				else
				{
					/* (|x|<1)**+-inf = 0,inf */
					return hy >= 0 ? floatP.Zero : -y;
				}
			}

			if (iy == 0x3f800000)
			{
				/* y is +-1 */
				return hy >= 0 ? x : floatP.One / x;
			}

			if (hy == 0x40000000)
			{
				/* y is 2 */
				return x * x;
			}

			if (hy == 0x3f000000
				/* y is  0.5 */
				&& hx >= 0)
			{
				/* x >= +0 */
				return Sqrt(x);
			}

			ax = Abs(x);
			/* special value of x */
			if (ix == 0x7f800000 || ix == 0 || ix == 0x3f800000)
			{
				/* x is +-0,+-inf,+-1 */
				z = ax;
				if (hy < 0)
				{
					/* z = (1/|x|) */
					z = floatP.One / z;
				}

				if (hx < 0)
				{
					if (((ix - 0x3f800000) | yisint) == 0)
					{
						z = (z - z) / (z - z); /* (-1)**non-int is NaN */
					}
					else if (yisint == 1)
					{
						z = -z; /* (x<0)**odd = -(|x|**odd) */
					}
				}

				return z;
			}

			sn = floatP.One; /* sign of result */
			if (hx < 0)
			{
				if (yisint == 0)
				{
					/* (x<0)**(non-int) is NaN */
					//return (x - x) / (x - x);
					return floatP.NaN;
				}

				if (yisint == 1)
				{
					/* (x<0)**(odd int) */
					sn = -floatP.One;
				}
			}

			/* |y| is HUGE */
			if (iy > 0x4d000000)
			{
				/* if |y| > 2**27 */
				/* over/underflow if x is not close to one */
				if (ix < 0x3f7ffff8)
				{
					return hy < 0
						? sn * floatP.FromRaw(HUGE_U32) * floatP.FromRaw(HUGE_U32)
						: sn * floatP.FromRaw(TINY_U32) * floatP.FromRaw(TINY_U32);
				}

				if (ix > 0x3f800007)
				{
					return hy > 0
						? sn * floatP.FromRaw(HUGE_U32) * floatP.FromRaw(HUGE_U32)
						: sn * floatP.FromRaw(TINY_U32) * floatP.FromRaw(TINY_U32);
				}

				/* now |1-x| is TINY <= 2**-20, suffice to compute
				log(x) by x-x^2/2+x^3/3-x^4/4 */
				t = ax - floatP.One; /* t has 20 trailing zeros */
				w = (t * t) * (floatP.FromRaw(0x3f000000) - t * (floatP.FromRaw(0x3eaaaaab) - t * floatP.FromRaw(0x3e800000)));
				u = floatP.FromRaw(IVLN2_H_U32) * t; /* IVLN2_H has 16 sig. bits */
				v = t * floatP.FromRaw(IVLN2_L_U32) - w * floatP.FromRaw(IVLN2_U32);
				t1 = u + v;
				iS = (int)t1.RawValue;
				t1 = floatP.FromRaw((uint)iS & 0xfffff000);
				t2 = v - (t1 - u);
			}
			else
			{
				floatP s2;
				floatP s_h;
				floatP s_l;
				floatP t_h;
				floatP t_l;

				n = 0;
				/* take care subnormal number */
				if (ix < 0x00800000)
				{
					ax *= floatP.FromRaw(TWO24_U32);
					n -= 24;
					ix = (int)ax.RawValue;
				}

				n += ((ix) >> 23) - 0x7f;
				j = ix & 0x007fffff;
				/* determine interval */
				ix = j | 0x3f800000; /* normalize ix */
				if (j <= 0x1cc471)
				{
					/* |x|<sqrt(3/2) */
					k = 0;
				}
				else if (j < 0x5db3d7)
				{
					/* |x|<sqrt(3)   */
					k = 1;
				}
				else
				{
					k = 0;
					n += 1;
					ix -= 0x00800000;
				}

				ax = floatP.FromRaw((uint)ix);

				/* compute s = s_h+s_l = (x-1)/(x+1) or (x-1.5)/(x+1.5) */
				u = ax - floatP.FromRaw(k == 0 ? BP_0_U32 : BP_1_U32); /* bp[0]=1.0, bp[1]=1.5 */
				v = floatP.One / (ax + floatP.FromRaw(k == 0 ? BP_0_U32 : BP_1_U32));
				s = u * v;
				s_h = s;
				iS = (int)s_h.RawValue;
				s_h = floatP.FromRaw((uint)iS & 0xfffff000);

				/* t_h=ax+bp[k] High */
				iS = (int)((((uint)ix >> 1) & 0xfffff000) | 0x20000000);
				t_h = floatP.FromRaw((uint)iS + 0x00400000 + (((uint)k) << 21));
				t_l = ax - (t_h - floatP.FromRaw(k == 0 ? BP_0_U32 : BP_1_U32));
				s_l = v * ((u - s_h * t_h) - s_h * t_l);

				/* compute log(ax) */
				s2 = s * s;
				r = s2 * s2 * (floatP.FromRaw(L1_U32) + s2 * (floatP.FromRaw(L2_U32) + s2 * (floatP.FromRaw(L3_U32) + s2 * (floatP.FromRaw(L4_U32) + s2 * (floatP.FromRaw(L5_U32) + s2 * floatP.FromRaw(L6_U32))))));
				r += s_l * (s_h + s);
				s2 = s_h * s_h;
				t_h = floatP.FromRaw(0x40400000) + s2 + r;
				iS = (int)t_h.RawValue;
				t_h = floatP.FromRaw((uint)iS & 0xfffff000);
				t_l = r - ((t_h - floatP.FromRaw(0x40400000)) - s2);

				/* u+v = s*(1+...) */
				u = s_h * t_h;
				v = s_l * t_h + t_l * s;

				/* 2/(3log2)*(s+...) */
				p_h = u + v;
				iS = (int)p_h.RawValue;
				p_h = floatP.FromRaw((uint)iS & 0xfffff000);
				p_l = v - (p_h - u);
				z_h = floatP.FromRaw(CP_H_U32) * p_h; /* cp_h+cp_l = 2/(3*log2) */
				z_l = floatP.FromRaw(CP_L_U32) * p_h + p_l * floatP.FromRaw(CP_U32) + floatP.FromRaw(k == 0 ? DP_L_0_U32 : DP_L_1_U32);

				/* log2(ax) = (s+..)*2/(3*log2) = n + dp_h + z_h + z_l */
				t = (floatP)n;
				t1 = ((z_h + z_l) + floatP.FromRaw(k == 0 ? DP_H_0_U32 : DP_H_1_U32)) + t;
				iS = (int)t1.RawValue;
				t1 = floatP.FromRaw((uint)iS & 0xfffff000);
				t2 = z_l - (((t1 - t) - floatP.FromRaw(k == 0 ? DP_H_0_U32 : DP_H_1_U32)) - z_h);
			};

			/* split up y into y1+y2 and compute (y1+y2)*(t1+t2) */
			iS = (int)y.RawValue;
			y1 = floatP.FromRaw((uint)iS & 0xfffff000);
			p_l = (y - y1) * t1 + y * t2;
			p_h = y1 * t1;
			z = p_l + p_h;
			j = (int)z.RawValue;
			if (j > 0x43000000)
			{
				/* if z > 128 */
				return sn * floatP.FromRaw(HUGE_U32) * floatP.FromRaw(HUGE_U32); /* overflow */
			}
			else if (j == 0x43000000)
			{
				/* if z == 128 */
				if (p_l + floatP.FromRaw(OVT_U32) > z - p_h)
				{
					return sn * floatP.FromRaw(HUGE_U32) * floatP.FromRaw(HUGE_U32); /* overflow */
				}
			}
			else if ((j & 0x7fffffff) > 0x43160000)
			{
				/* z < -150 */
				// FIXME: check should be  (uint32_t)j > 0xc3160000
				return sn * floatP.FromRaw(TINY_U32) * floatP.FromRaw(TINY_U32); /* underflow */
			}
			else if ((uint)j == 0xc3160000
					/* z == -150 */
					&& p_l <= z - p_h)
			{
				return sn * floatP.FromRaw(TINY_U32) * floatP.FromRaw(TINY_U32); /* underflow */
			}

			/*
				* compute 2**(p_h+p_l)
				*/
			i = j & 0x7fffffff;
			k = (i >> 23) - 0x7f;
			n = 0;
			if (i > 0x3f000000)
			{
				/* if |z| > 0.5, set n = [z+0.5] */
				n = j + (0x00800000 >> (k + 1));
				k = ((n & 0x7fffffff) >> 23) - 0x7f; /* new k for n */
				t = floatP.FromRaw((uint)n & ~(0x007fffffu >> k));
				n = ((n & 0x007fffff) | 0x00800000) >> (23 - k);
				if (j < 0)
				{
					n = -n;
				}
				p_h -= t;
			}

			t = p_l + p_h;
			iS = (int)t.RawValue;
			t = floatP.FromRaw((uint)iS & 0xffff8000);
			u = t * floatP.FromRaw(LG2_H_U32);
			v = (p_l - (t - p_h)) * floatP.FromRaw(LG2_U32) + t * floatP.FromRaw(LG2_L_U32);
			z = u + v;
			w = v - (z - u);
			t = z * z;
			t1 = z - t * (floatP.FromRaw(P1_U32) + t * (floatP.FromRaw(P2_U32) + t * (floatP.FromRaw(P3_U32) + t * (floatP.FromRaw(P4_U32) + t * floatP.FromRaw(P5_U32)))));
			r = (z * t1) / (t1 - floatP.FromRaw(0x40000000)) - (w + z * w);
			z = floatP.One - (r - z);
			j = (int)z.RawValue;
			j += n << 23;
			if ((j >> 23) <= 0)
			{
				/* subnormal output */
				z = ScaleB(z, n);
			}
			else
			{
				z = floatP.FromRaw((uint)j);
			}

			return sn * z;
		}

		/// <summary>
		/// Returns the power 2 of x
		/// </summary>
		public static floatP Pow2(floatP f)
		{
			return Pow(f, 2);
		}

		/// <summary>
		/// Returns the natural logarithm (base e) of x
		/// </summary>
		public static floatP Log(floatP x)
		{
			const uint LN2_HI_U32 = 0x3f317180; // 6.9313812256e-01
			const uint LN2_LO_U32 = 0x3717f7d1; // 9.0580006145e-06

			/* |(log(1+s)-log(1-s))/s - Lg(s)| < 2**-34.24 (~[-4.95e-11, 4.97e-11]). */
			const uint LG1_U32 = 0x3f2aaaaa; // 0.66666662693 /*  0xaaaaaa.0p-24*/
			const uint LG2_U32 = 0x3eccce13; // 0.40000972152 /*  0xccce13.0p-25 */
			const uint LG3_U32 = 0x3e91e9ee; // 0.28498786688 /*  0x91e9ee.0p-25 */
			const uint LG4_U32 = 0x3e789e26; // 0.24279078841 /*  0xf89e26.0p-26 */

			uint ix = x.RawValue;
			int k = 0;

			if ((ix < 0x00800000) || ((ix >> 31) != 0))
			{
				/* x < 2**-126  */
				if (ix << 1 == 0)
				{
					//return -1. / (x * x); /* log(+-0)=-inf */
					return floatP.NegativeInfinity;
				}

				if ((ix >> 31) != 0)
				{
					//return (x - x) / 0.; /* log(-#) = NaN */
					return floatP.NaN;
				}

				/* subnormal number, scale up x */
				floatP x1p25 = floatP.FromRaw(0x4c000000); // 0x1p25f === 2 ^ 25
				k -= 25;
				x *= x1p25;
				ix = x.RawValue;
			}
			else if (ix >= 0x7f800000)
			{
				return x;
			}
			else if (ix == 0x3f800000)
			{
				return floatP.Zero;
			}

			/* reduce x into [sqrt(2)/2, sqrt(2)] */
			ix += 0x3f800000 - 0x3f3504f3;
			k += ((int)(ix >> 23)) - 0x7f;
			ix = (ix & 0x007fffff) + 0x3f3504f3;
			x = floatP.FromRaw(ix);

			floatP f = x - floatP.One;
			floatP s = f / ((floatP)2.0f + f);
			floatP z = s * s;
			floatP w = z * z;
			floatP t1 = w * (floatP.FromRaw(LG2_U32) + w * floatP.FromRaw(LG4_U32));
			floatP t2 = z * (floatP.FromRaw(LG1_U32) + w * floatP.FromRaw(LG3_U32));
			floatP r = t2 + t1;
			floatP hfsq = (floatP)0.5f * f * f;
			floatP dk = (floatP)k;

			return s * (hfsq + r) + dk * floatP.FromRaw(LN2_LO_U32) - hfsq + f + dk * floatP.FromRaw(LN2_HI_U32);
		}

		/// <summary>
		/// Returns the natural logarithm (base e) of x
		/// </summary>
		public static floatP Log(floatP x, floatP e)
		{
			if (floatP.IsNaN(x))
			{
				return x; // IEEE 754-2008: NaN payload must be preserved
			}

			if (floatP.IsNaN(e))
			{
				return e; // IEEE 754-2008: NaN payload must be preserved
			}

			if (e == 1)
			{
				return floatP.NaN;
			}

			if ((x != 1) && ((e == 0) || double.IsPositiveInfinity(e)))
			{
				return floatP.NaN;
			}

			return Log(x) / Log(e);
		}

		/// <summary>
		/// Returns the base 2 logarithm of x
		/// </summary>
		public static floatP Log2(floatP x)
		{
			return Log(x, 2);
		}

		/// <summary>
		/// Returns the base 10 logarithm of x
		/// </summary>
		public static floatP Log10(floatP x)
		{
			return Log(x, 10);
		}

		/// <summary>
		/// Returns the sine of x
		/// </summary>
		public static floatP Sin(floatP x)
		{
			const uint pi_squared_times_five = 0x42456460; // 49.348022005446793094172454999381

			// https://en.wikipedia.org/wiki/Bhaskara_I%27s_sine_approximation_formula
			// sin(x) ~= (16x * (pi - x)) / (5pi^2 - 4x * (pi - x)) if 0 <= x <= pi

			// move x into range
			x %= floatP.FromRaw(Raw2Pi);
			if (x.IsNegative())
			{
				x += floatP.FromRaw(Raw2Pi);
			}

			bool negate;
			if (x > floatP.FromRaw(RawPi))
			{
				// pi < x <= 2pi, we need to move x to the 0 <= x <= pi range
				// also, we need to negate the result before returning it
				x = floatP.FromRaw(Raw2Pi) - x;
				negate = true;
			}
			else
			{
				negate = false;
			}

			floatP piMinusX = floatP.FromRaw(RawPi) - x;
			floatP result = ((floatP)16.0f * x * piMinusX) / (floatP.FromRaw(pi_squared_times_five) - (floatP)4.0f * x * piMinusX);
			return negate ? -result : result;
		}

		/// <summary>
		/// Returns the cosine of x
		/// </summary>
		public static floatP Cos(floatP x)
		{
			return Sin(x + floatP.FromRaw(RawPiOver2));
		}

		/// <summary>
		/// Returns the tangent of x
		/// </summary>
		public static floatP Tan(floatP x)
		{
			return Sin(x) / Cos(x);
		}

		/// <summary>
		/// Returns the hypothenuse between x & y -> square root of (x*x + y*y)
		/// </summary>
		public static floatP Hypothenuse(floatP x, floatP y)
		{
			return Sqrt(x * x + y * y);
		}

		/// <summary>
		/// Returns the arccosine of x
		/// </summary>
		public static floatP Acos(floatP x)
		{
			const uint PIO2_HI_U32 = 0x3fc90fda; // 1.5707962513e+00
			const uint PIO2_LO_U32 = 0x33a22168; // 7.5497894159e-08
			const uint P_S0_U32 = 0x3e2aaa75; // 1.6666586697e-01
			const uint P_S1_U32 = 0xbd2f13ba; // -4.2743422091e-02
			const uint P_S2_U32 = 0xbc0dd36b; // -8.6563630030e-03
			const uint Q_S1_U32 = 0xbf34e5ae; // - 7.0662963390e-01

			static floatP r(floatP z)
			{
				floatP p = z * (floatP.FromRaw(P_S0_U32) + z * (floatP.FromRaw(P_S1_U32) + z * floatP.FromRaw(P_S2_U32)));
				floatP q = (floatP)1.0f + z * floatP.FromRaw(Q_S1_U32);
				return p / q;
			}

			floatP x1p_120 = floatP.FromRaw(0x03800000); // 0x1p-120 === 2 ^ (-120)

			floatP z;
			floatP w;
			floatP s;

			uint hx = x.RawValue;
			uint ix = hx & 0x7fffffff;

			/* |x| >= 1 or nan */
			if (ix >= 0x3f800000)
			{
				if (ix == 0x3f800000)
				{
					if ((hx >> 31) != 0)
					{
						return (floatP)2.0f * floatP.FromRaw(PIO2_HI_U32) + x1p_120;
					}

					return floatP.Zero;
				}

				return floatP.NaN;
			}

			/* |x| < 0.5 */
			if (ix < 0x3f000000)
			{
				if (ix <= 0x32800000)
				{
					/* |x| < 2**-26 */
					return floatP.FromRaw(PIO2_HI_U32) + x1p_120;
				}

				return floatP.FromRaw(PIO2_HI_U32) - (x - (floatP.FromRaw(PIO2_LO_U32) - x * r(x * x)));
			}

			/* x < -0.5 */
			if ((hx >> 31) != 0)
			{
				z = ((floatP)1.0f + x) * (floatP)0.5f;
				s = Sqrt(z);
				w = r(z) * s - floatP.FromRaw(PIO2_LO_U32);
				return (floatP)2.0 * (floatP.FromRaw(PIO2_HI_U32) - (s + w));
			}

			/* x > 0.5 */
			z = ((floatP)1.0f - x) * (floatP)0.5f;
			s = Sqrt(z);
			hx = s.RawValue;
			floatP df = floatP.FromRaw(hx & 0xfffff000);
			floatP c = (z - df * df) / (s + df);
			w = r(z) * s + c;
			return (floatP)2.0f * (df + w);
		}

		/// <summary>
		/// Returns the arcsine of x
		/// </summary>
		public static floatP Asin(floatP x)
		{
			return floatP.FromRaw(RawPiOver2) - Acos(x);
		}

		/// <summary>
		/// Returns the arctangent of x
		/// </summary>
		public static floatP Atan(floatP x)
		{
			floatP z;

			uint ix = x.RawValue;
			bool sign = (ix >> 31) != 0;
			ix &= 0x7fffffff;

			if (ix >= 0x4c800000)
			{
				/* if |x| >= 2**26 */
				if (x.IsNaN())
				{
					return x;
				}

				floatP x1p_120 = floatP.FromRaw(0x03800000); // 0x1p-120 === 2 ^ (-120)
				z = floatP.FromRaw(_atanHi[3]) + x1p_120;
				return sign ? -z : z;
			}

			int id;
			if (ix < 0x3ee00000)
			{
				/* |x| < 0.4375 */
				if (ix < 0x39800000)
				{
					/* |x| < 2**-12 */
					//if (ix < 0x00800000)
					//{
					//    /* raise underflow for subnormal x */
					//    force_eval!(x * x);
					//}
					return x;
				}
				id = -1;
			}
			else
			{
				x = Abs(x);
				if (ix < 0x3f980000)
				{
					/* |x| < 1.1875 */
					if (ix < 0x3f300000)
					{
						/*  7/16 <= |x| < 11/16 */
						x = ((floatP)2.0f * x - (floatP)1.0f) / ((floatP)2.0f + x);
						id = 0;
					}
					else
					{
						/* 11/16 <= |x| < 19/16 */
						x = (x - (floatP)1.0f) / (x + (floatP)1.0f);
						id = 1;
					}
				}
				else if (ix < 0x401c0000)
				{
					/* |x| < 2.4375 */
					x = (x - (floatP)1.5f) / ((floatP)1.0f + (floatP)1.5f * x);
					id = 2;
				}
				else
				{
					/* 2.4375 <= |x| < 2**26 */
					x = (floatP)(-1.0f) / x;
					id = 3;
				}
			};

			/* end of argument reduction */
			z = x * x;
			floatP w = z * z;

			/* break sum from i=0 to 10 aT[i]z**(i+1) into odd and even poly */
			floatP s1 = z * (floatP.FromRaw(_aT[0]) + w * (floatP.FromRaw(_aT[2]) + w * floatP.FromRaw(_aT[4])));
			floatP s2 = w * (floatP.FromRaw(_aT[1]) + w * floatP.FromRaw(_aT[3]));
			if (id < 0)
			{
				return x - x * (s1 + s2);
			}

			z = floatP.FromRaw(_atanHi[id]) - ((x * (s1 + s2) - floatP.FromRaw(_atanLo[id])) - x);
			return sign ? -z : z;
		}

		/// <summary>
		/// Returns the signed angle between the positive x axis, and the direction (x, y)
		/// </summary>
		public static floatP Atan2(floatP y, floatP x)
		{
			if (x.IsNaN() || y.IsNaN())
			{
				return x + y;
			}

			uint ix = x.RawValue;
			uint iy = y.RawValue;

			if (ix == 0x3f800000)
			{
				/* x=1.0 */
				return Atan(y);
			}

			uint m = ((iy >> 31) & 1) | ((ix >> 30) & 2); /* 2*sign(x)+sign(y) */
			ix &= 0x7fffffff;
			iy &= 0x7fffffff;

			const uint PI_LO_U32 = 0xb3bbbd2e; // -8.7422776573e-08

			/* when y = 0 */
			if (iy == 0)
			{
				switch (m)
				{
					case 0:
					case 1:
						return y; /* atan(+-0,+anything)=+-0 */
					case 2:
						return floatP.FromRaw(RawPi); /* atan(+0,-anything) = pi */
					case 3:
					default:
						return -floatP.FromRaw(RawPi); /* atan(-0,-anything) =-pi */
				}
			}

			/* when x = 0 */
			if (ix == 0)
			{
				return (m & 1) != 0 ? -floatP.FromRaw(RawPiOver2) : floatP.FromRaw(RawPiOver2);
			}

			/* when x is INF */
			if (ix == 0x7f800000)
			{
				if (iy == 0x7f800000)
				{
					switch (m)
					{
						case 0:
							return floatP.FromRaw(RawPiOver4); /* atan(+INF,+INF) */
						case 1:
							return -floatP.FromRaw(RawPiOver4); /* atan(-INF,+INF) */
						case 2:
							return floatP.FromRaw(Raw3PiOver4); /* atan(+INF,-INF)*/
						case 3:
						default:
							return -floatP.FromRaw(Raw3PiOver4); /* atan(-INF,-INF)*/
					}
				}
				else
				{
					switch (m)
					{
						case 0:
							return floatP.Zero; /* atan(+...,+INF) */
						case 1:
							return -floatP.Zero; /* atan(-...,+INF) */
						case 2:
							return floatP.FromRaw(RawPi); /* atan(+...,-INF) */
						case 3:
						default:
							return -floatP.FromRaw(RawPi); /* atan(-...,-INF) */
					}
				}
			}

			/* |y/x| > 0x1p26 */
			if (ix + (26 << 23) < iy || iy == 0x7f800000)
			{
				return (m & 1) != 0 ? -floatP.FromRaw(RawPiOver2) : floatP.FromRaw(RawPiOver2);
			}

			/* z = atan(|y/x|) with correct underflow */
			floatP z = (m & 2) != 0 && iy + (26 << 23) < ix
				? floatP.Zero /*|y/x| < 0x1p-26, x < 0 */
				: Atan(Abs(y / x));

			switch (m)
			{
				case 0:
					return z; /* atan(+,+) */
				case 1:
					return -z; /* atan(-,+) */
				case 2:
					return floatP.FromRaw(RawPi) - (z - floatP.FromRaw(PI_LO_U32)); /* atan(+,-) */
				case 3:
				default:
					return (z - floatP.FromRaw(PI_LO_U32)) - floatP.FromRaw(RawPi); /* atan(-,-) */
			}
		}

		/// <summary>
		/// Returns x * 2^n computed efficiently.
		/// </summary>
		public static floatP ScaleB(floatP x, int n)
		{
			floatP x1p127 = floatP.FromRaw(0x7f000000); // 0x1p127f === 2 ^ 127
			floatP x1p_126 = floatP.FromRaw(0x800000); // 0x1p-126f === 2 ^ -126
			floatP x1p24 = floatP.FromRaw(0x4b800000); // 0x1p24f === 2 ^ 24

			if (n > 127)
			{
				x *= x1p127;
				n -= 127;
				if (n > 127)
				{
					x *= x1p127;
					n -= 127;
					if (n > 127)
					{
						n = 127;
					}
				}
			}
			else if (n < -126)
			{
				x *= x1p_126 * x1p24;
				n += 126 - 24;
				if (n < -126)
				{
					x *= x1p_126 * x1p24;
					n += 126 - 24;
					if (n < -126)
					{
						n = -126;
					}
				}
			}

			return x * floatP.FromRaw(((uint)(0x7f + n)) << 23);
		}

		/// <summary>
		/// The distance between given points
		/// </summary>
		public static long RawDistance(floatP f1, floatP f2)
		{
			if (!(floatP.IsFinite(f1) && floatP.IsFinite(f2)))
			{
				if (f1.Equals(f2))
					return 0;
				else
					return long.MaxValue;
			}
			else
			{
				uint sign1 = (uint)((int)f1.RawValue >> 31);
				int val1 = (int)(((f1.RawValue) ^ (sign1 & 0x7FFFFFFF)) - sign1);

				uint sign2 = (uint)((int)f2.RawValue >> 31);
				int val2 = (int)(((f2.RawValue) ^ (sign2 & 0x7FFFFFFF)) - sign2);

				return System.Math.Abs((long)val1 - (long)val2);
			}
		}


		private static readonly uint[] _atanHi = new uint[4]
		{
			0x3eed6338, // 4.6364760399e-01, /* atan(0.5)hi */
			0x3f490fda, // 7.8539812565e-01, /* atan(1.0)hi */
			0x3f7b985e, // 9.8279368877e-01, /* atan(1.5)hi */
			0x3fc90fda, // 1.5707962513e+00, /* atan(inf)hi */
		};

		private static readonly uint[] _atanLo = new uint[4]
		{
			0x31ac3769, // 5.0121582440e-09, /* atan(0.5)lo */
			0x33222168, // 3.7748947079e-08, /* atan(1.0)lo */
			0x33140fb4, // 3.4473217170e-08, /* atan(1.5)lo */
			0x33a22168, // 7.5497894159e-08, /* atan(inf)lo */
		};

		private static readonly uint[] _aT = new uint[5]
		{
			0x3eaaaaa9, // 3.3333328366e-01
			0xbe4cca98, // -1.9999158382e-01
			0x3e11f50d, // 1.4253635705e-01
			0xbdda1247, // -1.0648017377e-01
			0x3d7cac25  // 6.1687607318e-02
		};
	}
}
