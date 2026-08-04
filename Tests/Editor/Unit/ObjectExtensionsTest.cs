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
		// ADMIT: ObjectExtensions.IsValid must report false for a plain (non-UnityEngine.Object) null reference,
		// which is the branch every non-Unity caller takes.
		// RCR: ObjectExtensions.cs IsValid — change the non-Unity `return o != null;` to `return true;` → RED
		// (IsFalse(nullRef.IsValid()) fails). 2026-08-02
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
		// ADMIT: ObjectExtensions.GetDisplayString must append the generic ARGUMENT names, not just the open type's
		// name — that is what makes `List<int>` distinguishable from `List<string>` in the Config Browser.
		// RCR: ObjectExtensions.cs GetDisplayString — delete `stringBuilder.Append(types[0].Name);` from
		// appendGenericParameters → RED ("Int32" is missing from the output). 2026-08-02
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
