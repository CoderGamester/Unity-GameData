using GameLovers.GameData;
using NUnit.Framework;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ReflectionExtensionsTest
	{
		public class PublicCtorClass
		{
			public int Value = 42;
			public PublicCtorClass() { }
		}

		public class PrivateCtorClass
		{
			public int Value = 99;
			private PrivateCtorClass() { }
		}

		[Test]
		// ADMIT: ReflectionExtensions.CreateInstance takes the Activator happy path for a public parameterless
		// constructor and must return the constructed instance.
		// RCR: ReflectionExtensions.cs CreateInstance — `return null;` from the try block → RED (IsNotNull fails).
		// The private-ctor sibling stays green: it reaches the catch fallback instead. 2026-08-02
		public void CreateInstance_PublicCtor_ReturnsNewInstance()
		{
			// Activator.CreateInstance happy path: public parameterless ctor.
			var obj = typeof(PublicCtorClass).CreateInstance();

			Assert.IsNotNull(obj);
			Assert.IsInstanceOf<PublicCtorClass>(obj);
			Assert.AreEqual(42, ((PublicCtorClass)obj).Value);
		}

		[Test]
		// ADMIT: ReflectionExtensions.CreateInstance falls back to a NonPublic GetConstructor when Activator throws,
		// so types with a private parameterless constructor are still constructible.
		// RCR: ReflectionExtensions.cs CreateInstance — `return null;` from the catch block → RED (IsNotNull fails).
		// The public-ctor sibling stays green: it never enters the catch. 2026-08-02
		public void CreateInstance_PrivateCtor_ReturnsNewInstance()
		{
			// Falls through to GetConstructor(BindingFlags.NonPublic | ...) when Activator throws
			// because the parameterless ctor is not public.
			var obj = typeof(PrivateCtorClass).CreateInstance();

			Assert.IsNotNull(obj);
			Assert.IsInstanceOf<PrivateCtorClass>(obj);
			Assert.AreEqual(99, ((PrivateCtorClass)obj).Value);
		}

		public class BaseWithField
		{
#pragma warning disable CS0414 // field accessed via reflection by FindFieldByName
			private int _baseField = 7;
#pragma warning restore CS0414
		}

		public class DerivedNoField : BaseWithField
		{
		}

		[Test]
		// ADMIT: ReflectionExtensions.FindFieldByName must walk up BaseType to find a private field declared on an
		// ancestor — `type.GetField` alone never sees it.
		// RCR: ReflectionExtensions.cs FindFieldByName — change the base-walk to `while (false)` → RED (the returned
		// FieldInfo is null). 2026-08-02
		public void FindFieldByName_OnDerivedType_WalksBaseTypeChain()
		{
			var first = typeof(DerivedNoField).FindFieldByName("_baseField");
			var second = typeof(DerivedNoField).FindFieldByName("_baseField");

			Assert.IsNotNull(first);
			Assert.AreEqual("_baseField", first.Name);
			Assert.AreEqual(typeof(BaseWithField), first.DeclaringType);
			Assert.AreSame(first, second);
		}

		public class BaseWithMethod
		{
#pragma warning disable IDE0051, CS0414 // method accessed via reflection by FindMethodByName
			private int GetSecret() => 7;
#pragma warning restore IDE0051, CS0414
		}

		public class DerivedNoMethod : BaseWithMethod
		{
		}

		[Test]
		// ADMIT: ReflectionExtensions.FindMethodByName walks the base type chain for a private method and caches
		// the result in MethodsByNameFromType.
		// RCR: ReflectionExtensions.cs FindMethodByName — delete `methods.Add(hash, methodInfo);` → RED (the
		// second call re-runs Type.GetMethod and returns a different instance, failing Assert.AreSame). 2026-08-01
		public void FindMethodByName_OnDerivedType_WalksBaseTypeChainAndCaches()
		{
			var first = typeof(DerivedNoMethod).FindMethodByName("GetSecret");
			var second = typeof(DerivedNoMethod).FindMethodByName("GetSecret");

			Assert.IsNotNull(first);
			Assert.AreEqual("GetSecret", first.Name);
			Assert.AreEqual(typeof(BaseWithMethod), first.DeclaringType);
			Assert.AreSame(first, second);
		}
	}
}
