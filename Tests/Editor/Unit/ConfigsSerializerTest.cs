using System;
using System.Collections.Generic;
using GameLovers.GameData;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class ConfigsSerializerTest
	{
		private ConfigsSerializer _serializer;
		private ConfigsProvider _provider;

		[Serializable]
		public struct TestConfig
		{
			public int Id;
			public string Name;
		}

		[Serializable]
		[IgnoreServerSerialization]
		public struct IgnoredConfig
		{
			public int Value;
		}

		public struct NonSerializableConfig
		{
			public int Value;
		}

		[Serializable]
		public struct UnityTypesConfig
		{
			public Color Color;
			public Vector2 Vec2;
			public Vector3 Vec3;
			public Vector4 Vec4;
			public Quaternion Quat;
		}

		public enum TestEnum
		{
			Value1,
			Value2
		}

		[Serializable]
		public struct EnumConfig
		{
			public TestEnum Selection;
		}

		[SetUp]
		public void Setup()
		{
			_serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			_provider = new ConfigsProvider();
		}

		[Test]
		// ADMIT: ConfigsSerializer.Serialize must emit the caller's version string verbatim into the payload's
		// `Version` property.
		// RCR: ConfigsSerializer.cs Serialize — change `Version = version,` to `version + "!"` → RED (the
		// `"Version":"1"` substring assertion fails). Also reddens the two round-trip version assertions. 2026-08-02
		public void Serialize_ValidProvider_ReturnsJsonString()
		{
			_provider.AddSingletonConfig(new TestConfig { Id = 1, Name = "Test" });
			var json = _serializer.Serialize(_provider, "1");

			Assert.IsNotEmpty(json);
			Assert.IsTrue(json.Contains("TestConfig"));
			Assert.IsTrue(json.Contains("\"Version\":\"1\""));
		}

		[Test]
		// ADMIT: ConfigsSerializer.Serialize must skip config types carrying [IgnoreServerSerialization] so they are
		// never shipped to a server.
		// RCR: ConfigsSerializer.cs Serialize — disable the CustomAttributes `continue` guard → RED (IgnoredConfig
		// appears in the JSON). Also reddens Integration.FullWorkflow_AddSerializeDeserializeAccess. 2026-08-02
		public void Serialize_IgnoreServerSerialization_ExcludesMarkedTypes()
		{
			_provider.AddSingletonConfig(new TestConfig { Id = 1 });
			_provider.AddSingletonConfig(new IgnoredConfig { Value = 100 });
			
			var json = _serializer.Serialize(_provider, "1");

			Assert.IsTrue(json.Contains("TestConfig"));
			Assert.IsFalse(json.Contains("IgnoredConfig"));
		}

		[Test]
		// ADMIT: ConfigsSerializer.Serialize must refuse a config type lacking [Serializable] rather than emitting
		// a payload the server cannot read back.
		// RCR: ConfigsSerializer.cs Serialize — disable the `!type.IsSerializable` guard → RED (no exception is
		// thrown). 2026-08-02
		public void Serialize_NonSerializableWithoutAttribute_ThrowsException()
		{
			// Note: ConfigsProvider allows adding non-serializable types, 
			// but ConfigsSerializer.Serialize will check type.IsSerializable
			_provider.AddSingletonConfig(new NonSerializableConfig { Value = 1 });
			
			Assert.Throws<Exception>(() => _serializer.Serialize(_provider, "1"));
		}

		[Test]
		public void Serialize_UnityTypes_SerializesCorrectly()
		{
			var config = new UnityTypesConfig
			{
				Color = Color.red,
				Vec2 = Vector2.one,
				Vec3 = Vector3.up,
				Vec4 = new Vector4(1, 2, 3, 4),
				Quat = Quaternion.identity
			};
			_provider.AddSingletonConfig(config);

			var json = _serializer.Serialize(_provider, "1");

			// Color is serialized as hex string by ColorJsonConverter
			Assert.IsTrue(json.Contains("#FF0000FF") || json.Contains("\"Color\":"));
			// Vectors are serialized as objects with x,y,z,w properties
			Assert.IsTrue(json.Contains("\"x\":"));
		}

		[Test]
		// ADMIT: ConfigsSerializer's settings must include StringEnumConverter, so enum config fields ship as
		// readable names instead of ordinals that shift when the enum is reordered.
		// RCR: ConfigsSerializer.cs ctor — remove `new StringEnumConverter(),` from the Converters list → RED (the
		// `"Selection":"Value2"` substring assertion fails; the payload carries `1`). 2026-08-02
		public void Serialize_EnumValues_SerializedAsStrings()
		{
			_provider.AddSingletonConfig(new EnumConfig { Selection = TestEnum.Value2 });
			var json = _serializer.Serialize(_provider, "1");

			Assert.IsTrue(json.Contains("\"Selection\":\"Value2\""));
		}

		[Test]
		// ADMIT: ConfigsSerializer.Deserialize must forward the parsed version to IConfigsAdder.UpdateTo, not a
		// constant.
		// RCR: ConfigsSerializer.cs Deserialize — change `cfg.UpdateTo(versionNumber, ...)` to `cfg.UpdateTo(0, ...)`
		// → RED (Version is 0, not 5). Also reddens RoundTrip_AllConfigTypes and Integration.BackendSync. 2026-08-02
		public void Deserialize_ValidJson_IntoExistingProvider()
		{
			// First serialize to get the correct format, then deserialize
			var sourceProvider = new ConfigsProvider();
			sourceProvider.AddSingletonConfig(new TestConfig { Id = 10, Name = "Deserialized" });
			var json = _serializer.Serialize(sourceProvider, "5");
			
			_serializer.Deserialize(json, _provider);

			Assert.AreEqual(5, (int)_provider.Version);
			Assert.AreEqual(10, _provider.GetConfig<TestConfig>().Id);
			Assert.AreEqual("Deserialized", _provider.GetConfig<TestConfig>().Name);
		}

		[Test]
		public void Deserialize_MalformedJson_ThrowsJsonException()
		{
			var json = "{ invalid json }";
			Assert.Throws<JsonReaderException>(() => _serializer.Deserialize(json, _provider));
		}

		[Test]
		// ADMIT: ConfigsSerializer.Deserialize's `ulong.TryParse` guard must fall back to 0 only when the parse
		// FAILS — a well-formed numeric version has to survive the round trip.
		// RCR: ConfigsSerializer.cs Deserialize — invert the guard to `if (ulong.TryParse(...))` → RED (Version is 0,
		// not 10). Also reddens Deserialize_ValidJson and Integration.BackendSync. 2026-08-02
		public void RoundTrip_AllConfigTypes_PreservesData()
		{
			// Use a collection that includes a "singleton-like" entry at ID 0
			// Note: ConfigsProvider doesn't support both singleton AND collection of same type
			_provider.AddConfigs(c => c.Id, new List<TestConfig> 
			{ 
				new TestConfig { Id = 0, Name = "First" },
				new TestConfig { Id = 2, Name = "Collection1" },
				new TestConfig { Id = 3, Name = "Collection2" }
			});

			var json = _serializer.Serialize(_provider, "10");
			var newProvider = new ConfigsProvider();
			_serializer.Deserialize(json, newProvider);

			Assert.AreEqual(10, (int)newProvider.Version);
			Assert.AreEqual("First", newProvider.GetConfig<TestConfig>(0).Name);
			Assert.AreEqual("Collection1", newProvider.GetConfig<TestConfig>(2).Name);
			Assert.AreEqual("Collection2", newProvider.GetConfig<TestConfig>(3).Name);
		}

		[Test]
		public void SecureMode_TypeNameHandlingNone_Verified()
		{
			var secureSerializer = new ConfigsSerializer(SerializationSecurityMode.Secure);
			_provider.AddSingletonConfig(new TestConfig { Id = 1 });
			
			var json = secureSerializer.Serialize(_provider, "1");

			// In secure mode, type names shouldn't be present in the value part of JSON
			// depending on how it's structured. Actually SerializedConfigs uses Dictionary<Type, IEnumerable>
			// which might still include type keys. Let's check.
			Assert.IsFalse(json.Contains("$type"));
		}

		[Test]
		// ADMIT: Vector3JsonConverter.ReadJson must rebuild the Vector3 from the serialized payload instead of
		// yielding the struct default.
		// RCR: VectorJsonConverters.cs Vector3JsonConverter.ReadJson — `return default;` → RED (Vec3 comes back
		// (0,0,0) instead of (3.3,4.4,5.5)). 2026-08-02
		public void RoundTrip_UnityTypes_PreservesValues()
		{
			var config = new UnityTypesConfig
			{
				Color = new Color(0.1f, 0.2f, 0.3f, 0.4f),
				Vec2 = new Vector2(1.1f, 2.2f),
				Vec3 = new Vector3(3.3f, 4.4f, 5.5f),
				Vec4 = new Vector4(6.6f, 7.7f, 8.8f, 9.9f),
				Quat = new Quaternion(0.1f, 0.2f, 0.3f, 0.4f)
			};
			_provider.AddSingletonConfig(config);

			var json = _serializer.Serialize(_provider, "1");
			var newProvider = new ConfigsProvider();
			_serializer.Deserialize(json, newProvider);

			var result = newProvider.GetConfig<UnityTypesConfig>();
			// Color has some precision loss due to hex string representation (8-bit per channel)
			Assert.AreEqual(config.Color.r, result.Color.r, 0.01f);
			Assert.AreEqual(config.Color.g, result.Color.g, 0.01f);
			Assert.AreEqual(config.Color.b, result.Color.b, 0.01f);
			Assert.AreEqual(config.Color.a, result.Color.a, 0.01f);
			Assert.AreEqual(config.Vec2, result.Vec2);
			Assert.AreEqual(config.Vec3, result.Vec3);
			Assert.AreEqual(config.Vec4, result.Vec4);
			// Quaternion equality can be tricky with floating point, but these are exact round-trips
			Assert.AreEqual(config.Quat.x, result.Quat.x, 0.0001f);
			Assert.AreEqual(config.Quat.y, result.Quat.y, 0.0001f);
			Assert.AreEqual(config.Quat.z, result.Quat.z, 0.0001f);
			Assert.AreEqual(config.Quat.w, result.Quat.w, 0.0001f);
		}
	}
}
