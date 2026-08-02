using System.Collections.Generic;
using GameLovers.GameData;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ObjectExtensionsTest
	{
		[Test]
		// ADMIT: ObjectExtensions.GetValid<T> must return a true C# null — not Unity's fake-null wrapper — for a
		// destroyed UnityEngine.Object, or `?.` chains on the result silently break.
		// RCR: ObjectExtensions.cs GetValid — change `return obj != null ? o : default;` to `return o;` → RED
		// (`go.GetValid()?.name` throws MissingReferenceException instead of evaluating to null). 2026-08-01
		public void GetValid_OnDestroyedUnityObject_ReturnsNullSoNullConditionalWorks()
		{
			var go = new GameObject("Temp");
			Object.DestroyImmediate(go);

			var valid = go.GetValid();

			Assert.IsNull(valid);
			Assert.DoesNotThrow(() =>
			{
				var _ = valid?.name;
			});
		}

		[Test]
		// ADMIT: ObjectExtensions.Dispose(GameObject, bool) must return ObjectDisposeResult.None via TryGetValid
		// for an already-destroyed GameObject rather than calling Destroy on it a second time.
		// RCR: ObjectExtensions.cs Dispose — delete the `if (!gameObject.TryGetValid(out gameObject))` early
		// return → RED (falls through to a second Destroy attempt; the result is no longer None). 2026-08-01
		public void Dispose_OnDestroyedUnityObject_ReturnsNoneAndDoesNotThrow()
		{
			var go = new GameObject("Temp");
			Object.DestroyImmediate(go);

			var result = ObjectExtensions.ObjectDisposeResult.Destroyed;

			Assert.DoesNotThrow(() => result = go.Dispose(forceDestroy: true));
			Assert.AreEqual(ObjectExtensions.ObjectDisposeResult.None, result);
		}

		[Test]
		public void IsValid_NullAndLiveReferences_ReturnsCorrectly()
		{
			object live = new object();
			object nullRef = null;

			Assert.IsFalse(nullRef.IsValid());
			Assert.IsTrue(live.IsValid());

			Assert.IsNull(((string)null).GetValid());
			Assert.AreEqual("x", "x".GetValid());

			Assert.IsFalse(((string)null).TryGetValid(out _));
			Assert.IsTrue("x".TryGetValid(out var valid));
			Assert.AreEqual("x", valid);
		}

		[Test]
		public void GetDisplayString_GenericAndArrayTypes_FormatsReadable()
		{
			var listType = typeof(List<int>);

			var first = listType.GetDisplayString();
			var second = listType.GetDisplayString();

			StringAssert.Contains("List", first);
			StringAssert.Contains("Int32", first);
			Assert.AreEqual(first, second);
		}
	}
}
