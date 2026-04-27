using System;
using System.Collections.Generic;
using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class SortedListExtensionsTest
	{
		[Test]
		public void InsertIntoSortedList_RandomInputs_PreservesNonDecreasingOrdering()
		{
			var list = new List<int>();
			int[] values = { 5, 1, 9, 3, 7, 2, 8, 4, 6, 0 };

			foreach (var v in values)
			{
				list.InsertIntoSortedList(v);
				AssertNonDecreasing(list);
			}

			Assert.AreEqual(values.Length, list.Count);
		}

		[Test]
		public void InsertIntoSortedList_DuplicateInputs_PreservesNonDecreasingOrdering()
		{
			var list = new List<int>();
			int[] values = { 5, 5, 5, 1, 9, 5, 1, 9 };

			foreach (var v in values)
			{
				list.InsertIntoSortedList(v);
				AssertNonDecreasing(list);
			}

			Assert.AreEqual(values.Length, list.Count);
		}

		[Test]
		public void InsertIntoSortedList_WithComparison_DescendingComparison_PreservesNonIncreasingOrdering()
		{
			var list = new List<int>();
			Comparison<int> descending = (a, b) => b.CompareTo(a);
			int[] values = { 5, 1, 9, 3, 7 };

			foreach (var v in values)
			{
				list.InsertIntoSortedList(v, descending);
			}

			for (int i = 1; i < list.Count; i++)
			{
				Assert.IsTrue(list[i - 1] >= list[i],
					$"Expected non-increasing at index {i}: {list[i - 1]} should be >= {list[i]}");
			}
		}

		[Test]
		public void InsertIntoSortedList_WithComparer_PreservesNonDecreasingOrdering()
		{
			var list = new List<int>();
			int[] values = { 5, 1, 9, 3, 7 };

			foreach (var v in values)
			{
				list.InsertIntoSortedList(v, Comparer<int>.Default);
				AssertNonDecreasing(list);
			}

			Assert.AreEqual(values.Length, list.Count);
		}

		private static void AssertNonDecreasing(IList<int> list)
		{
			for (int i = 1; i < list.Count; i++)
			{
				Assert.IsTrue(list[i - 1] <= list[i],
					$"Expected non-decreasing at index {i}: {list[i - 1]} should be <= {list[i]}");
			}
		}
	}
}
