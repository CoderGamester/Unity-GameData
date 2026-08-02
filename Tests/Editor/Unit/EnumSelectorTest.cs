using System;
using GameLovers.GameData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class EnumSelectorTest
	{
		public enum EnumExample
		{
			Value1,
			Value2
		}

		public class EnumSelectorExample : EnumSelector<EnumExample>
		{
			public EnumSelectorExample() : base(EnumExample.Value1)
			{
			}

			public EnumSelectorExample(EnumExample data) : base(data)
			{
			}

			public static implicit operator EnumSelectorExample(EnumExample value)
			{
				return new EnumSelectorExample(value);
			}
		}

		// Non-contiguous, negative-inclusive enum: declaration-order index (0,1,2) never coincides with
		// the underlying int value (-3,0,7), unlike EnumExample{0,1} above.
		public enum EnumSparse
		{
			Legacy = -3,
			None = 0,
			Ready = 7
		}

		public class EnumSparseSelectorExample : EnumSelector<EnumSparse>
		{
			public EnumSparseSelectorExample() : base(EnumSparse.None)
			{
			}

			public EnumSparseSelectorExample(EnumSparse data) : base(data)
			{
			}
		}

		private EnumSelectorExample _enumSelector;
		private EnumSparseSelectorExample _sparseEnumSelector;

		[SetUp]
		public void Init()
		{
			_enumSelector = new EnumSelectorExample(EnumExample.Value1);
			_sparseEnumSelector = new EnumSparseSelectorExample(EnumSparse.None);
		}

		[Test]
		public void ValueCheck()
		{
			Assert.AreEqual(EnumExample.Value1, _enumSelector.GetSelection());
			Assert.AreEqual((int)EnumExample.Value1, _enumSelector.GetSelectedIndex());
			Assert.AreEqual(EnumExample.Value1.ToString(), _enumSelector.GetSelectionString());
			Assert.IsTrue(_enumSelector.HasValidSelection());
		}

		[Test]
		public void ValueSetCheck()
		{
			_enumSelector.SetSelection(EnumExample.Value2);

			Assert.AreEqual(EnumExample.Value2, _enumSelector.GetSelection());
			Assert.AreEqual((int)EnumExample.Value2, _enumSelector.GetSelectedIndex());
			Assert.AreEqual(EnumExample.Value2.ToString(), _enumSelector.GetSelectionString());
			Assert.IsTrue(_enumSelector.HasValidSelection());
		}

		[Test]
		public void GetSelection_InvalidSelection_ReturnsFirstValue()
		{
			_enumSelector.SetSelectionString("InvalidValue");

			LogAssert.Expect(LogType.Error, "Could not load enum for string: InvalidValue");

			// EnumSelector returns the first enum value when selection is invalid
			Assert.AreEqual(EnumExample.Value1, _enumSelector.GetSelection());
		}

		[Test]
		public void HasValidSelection_RemovedEnumValue_ReturnsFalse()
		{
			_enumSelector.SetSelectionString("NonExistentValue");

			LogAssert.Expect(LogType.Error, "Could not load enum for string: NonExistentValue");

			Assert.IsFalse(_enumSelector.HasValidSelection());
		}

		[Test]
		public void HasValidSelection_EmptyString_ReturnsFalse()
		{
			_enumSelector.SetSelectionString("");

			LogAssert.Expect(LogType.Error, "Could not load enum for string: ");

			Assert.IsFalse(_enumSelector.HasValidSelection());
		}

		[Test]
		public void ImplicitConversion_ToEnum_Works()
		{
			EnumExample val = _enumSelector;
			Assert.AreEqual(EnumExample.Value1, val);
		}

		[Test]
		// ADMIT: EnumSelector<T>.GetSelection resolves through the name-keyed EnumDictionary, not by casting a
		// declaration-order index — which differs only for a non-contiguous enum like EnumSparse.
		// RCR: EnumSelector.cs static ctor — change `EnumDictionary[EnumNames[i]] = EnumValues[i];` to
		// `= (T)(object)i;` → RED (returns (EnumSparse)2, not Ready). EnumExample{0,1} stays green: index == value. 2026-08-01
		public void GetSelection_NonContiguousEnum_ReturnsAuthoredValue()
		{
			_sparseEnumSelector.SetSelection(EnumSparse.Ready);

			Assert.AreEqual(EnumSparse.Ready, _sparseEnumSelector.GetSelection());
		}

		[Test]
		// ADMIT: EnumSelector<T>.GetSelectedIndex returns Array.IndexOf(EnumValues, value) against the authored
		// value from EnumDictionary. Enum.GetValues sorts by UNSIGNED bit pattern, so the negative member Legacy
		// sorts LAST — the test computes the expected position rather than assuming declaration order.
		// RCR: EnumSelector.cs static ctor — change `EnumDictionary[EnumNames[i]] = EnumValues[i];` to
		// `= (T)(object)i;` → RED (resolves to (EnumSparse)2, which matches no member, so IndexOf returns -1
		// instead of 2). 2026-08-02
		public void GetSelectedIndex_NonContiguousEnumWithNegativeMember_ReturnsCorrectArrayPosition()
		{
			_sparseEnumSelector.SetSelection(EnumSparse.Legacy);

			var expectedIndex = Array.IndexOf(EnumSparseSelectorExample.EnumValues, EnumSparse.Legacy);
			Assert.AreEqual(expectedIndex, _sparseEnumSelector.GetSelectedIndex());
		}
	}
}