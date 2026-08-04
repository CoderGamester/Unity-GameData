using System;
using System.Collections.Generic;
using GameLovers.GameData;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.GameData.Tests
{
	[TestFixture]
	public class JsonConvertersTest
	{
		private JsonSerializerSettings _settings;

		[SetUp]
		public void Setup()
		{
			_settings = new JsonSerializerSettings
			{
				Converters = new List<JsonConverter>
				{
					new ColorJsonConverter(),
					new Vector2JsonConverter(),
					new Vector3JsonConverter(),
					new Vector4JsonConverter(),
					new QuaternionJsonConverter()
				}
			};
		}

		[Test]
		// ADMIT: ColorJsonConverter.ReadJson must return the parsed colour from the hex-string branch, not the
		// Color.white fallback reserved for unrecognised tokens.
		// RCR: ColorJsonConverter.cs ReadJson — return `Color.white` from inside the successful
		// TryParseHtmlString branch → RED (all four channel assertions fail). Also reddens the two serializer-level
		// Unity-type round trips. 2026-08-02
		public void Color_RoundTrip()
		{
			var color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
			var json = JsonConvert.SerializeObject(color, _settings);
			var result = JsonConvert.DeserializeObject<Color>(json, _settings);
			
			// Hex color format (#RRGGBBAA) has 8-bit precision per channel,
			// so tolerance needs to account for 1/255 ≈ 0.004 quantization error
			Assert.AreEqual(color.r, result.r, 0.01f);
			Assert.AreEqual(color.g, result.g, 0.01f);
			Assert.AreEqual(color.b, result.b, 0.01f);
			Assert.AreEqual(color.a, result.a, 0.01f);
		}

		[Test]
		// ADMIT: Vector2JsonConverter.ReadJson must rebuild the Vector2 from the deserialized payload rather than
		// yielding the struct default.
		// RCR: VectorJsonConverters.cs Vector2JsonConverter.ReadJson — `return default;` → RED (x/y come back 0).
		// 2026-08-02
		public void Vector2_RoundTrip()
		{
			var vec = new Vector2(1.1f, 2.2f);
			var json = JsonConvert.SerializeObject(vec, _settings);
			var result = JsonConvert.DeserializeObject<Vector2>(json, _settings);
			
			Assert.AreEqual(vec.x, result.x, 0.0001f);
			Assert.AreEqual(vec.y, result.y, 0.0001f);
		}

		[Test]
		// ADMIT: Vector3JsonConverter.WriteJson must emit the real z component — the write side is where a dropped
		// component silently survives a round trip that only checks x and y.
		// RCR: VectorJsonConverters.cs Vector3JsonConverter.WriteJson — `writer.WriteValue(0f);` for z → RED (z
		// comes back 0, not 3.3). Also reddens ConfigsSerializerTest.RoundTrip_UnityTypes_PreservesValues.
		// 2026-08-02
		public void Vector3_RoundTrip()
		{
			var vec = new Vector3(1.1f, 2.2f, 3.3f);
			var json = JsonConvert.SerializeObject(vec, _settings);
			var result = JsonConvert.DeserializeObject<Vector3>(json, _settings);
			
			Assert.AreEqual(vec.x, result.x, 0.0001f);
			Assert.AreEqual(vec.y, result.y, 0.0001f);
			Assert.AreEqual(vec.z, result.z, 0.0001f);
		}

		[Test]
		// ADMIT: Vector4JsonConverter.ReadJson must rebuild the Vector4 from the deserialized payload rather than
		// yielding the struct default.
		// RCR: VectorJsonConverters.cs Vector4JsonConverter.ReadJson — `return default;` → RED (all four components
		// come back 0). 2026-08-02
		public void Vector4_RoundTrip()
		{
			var vec = new Vector4(1.1f, 2.2f, 3.3f, 4.4f);
			var json = JsonConvert.SerializeObject(vec, _settings);
			var result = JsonConvert.DeserializeObject<Vector4>(json, _settings);
			
			Assert.AreEqual(vec.x, result.x, 0.0001f);
			Assert.AreEqual(vec.y, result.y, 0.0001f);
			Assert.AreEqual(vec.z, result.z, 0.0001f);
			Assert.AreEqual(vec.w, result.w, 0.0001f);
		}

		[Test]
		// ADMIT: QuaternionJsonConverter.ReadJson reuses Vector4Serializable and must convert it back to a
		// Quaternion, not yield the struct default.
		// RCR: VectorJsonConverters.cs QuaternionJsonConverter.ReadJson — `return default;` → RED (all four
		// components come back 0). 2026-08-02
		public void Quaternion_RoundTrip()
		{
			var quat = new Quaternion(0.1f, 0.2f, 0.3f, 0.4f);
			var json = JsonConvert.SerializeObject(quat, _settings);
			var result = JsonConvert.DeserializeObject<Quaternion>(json, _settings);
			
			Assert.AreEqual(quat.x, result.x, 0.0001f);
			Assert.AreEqual(quat.y, result.y, 0.0001f);
			Assert.AreEqual(quat.z, result.z, 0.0001f);
			Assert.AreEqual(quat.w, result.w, 0.0001f);
		}
	}
}
