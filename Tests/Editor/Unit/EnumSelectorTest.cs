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

		private EnumSelectorExample _enumSelector;

		[SetUp]
		public void Init()
		{
			_enumSelector = new EnumSelectorExample(EnumExample.Value1);
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
	}
}