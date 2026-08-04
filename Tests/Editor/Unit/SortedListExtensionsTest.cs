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
		// ADMIT: the single-argument InsertIntoSortedList<T> overload must delegate with an ASCENDING comparison
		// (`a.CompareTo(b)`), which is the ordering every caller of the IComparable form assumes.
		// RCR: SortedListExtensions.cs InsertIntoSortedList<T>(IList<T>, T) — flip the delegated lambda to
		// `(a, b) => b.CompareTo(a)` → RED (the non-decreasing assertion trips). Also reddens the duplicate-input
		// sibling, which uses the same overload. 2026-08-02
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
		// ADMIT: InsertIntoSortedList's equal-element branch must insert AT the matched middle index — that is the
		// only position that keeps a run of duplicates contiguous and the list sorted.
		// RCR: SortedListExtensions.cs InsertIntoSortedList<T>(IList<T>, T, Comparison<T>) — change the
		// `compareToResult == 0` branch to `list.Insert(0, value);` → RED (a duplicate lands before smaller
		// elements). Distinct inputs never reach this branch, so the sibling tests stay green. 2026-08-02
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
		// ADMIT: the Comparison<T> overload must honour the CALLER's comparison sign, so a descending comparison
		// produces a descending list.
		// RCR: SortedListExtensions.cs InsertIntoSortedList<T>(IList<T>, T, Comparison<T>) — negate
		// `comparison(middleValue, value)` → RED (the list comes out ascending). Also reddens the two siblings that
		// route through this same overload. 2026-08-02
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
		// ADMIT: the IComparer<T> overload is a separate binary search from the Comparison<T> one and must use the
		// comparer's sign directly.
		// RCR: SortedListExtensions.cs InsertIntoSortedList<T>(IList<T>, T, IComparer<T>) — negate
		// `comparer.Compare(middleValue, value)` → RED (the non-decreasing assertion trips). 2026-08-02
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
