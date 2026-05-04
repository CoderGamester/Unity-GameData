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
		public void CreateInstance_PublicCtor_ReturnsNewInstance()
		{
			// Activator.CreateInstance happy path: public parameterless ctor.
			var obj = typeof(PublicCtorClass).CreateInstance();

			Assert.IsNotNull(obj);
			Assert.IsInstanceOf<PublicCtorClass>(obj);
			Assert.AreEqual(42, ((PublicCtorClass)obj).Value);
		}

		[Test]
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
		public void FindFieldByName_OnDerivedType_WalksBaseTypeChain()
		{
			var first = typeof(DerivedNoField).FindFieldByName("_baseField");
			var second = typeof(DerivedNoField).FindFieldByName("_baseField");

			Assert.IsNotNull(first);
			Assert.AreEqual("_baseField", first.Name);
			Assert.AreEqual(typeof(BaseWithField), first.DeclaringType);
			Assert.AreSame(first, second);
		}
	}
}
