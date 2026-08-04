using System;
using Newtonsoft.Json;
using UnityEngine;

namespace GameLovers.GameData
{
	/// <summary>
	/// JSON converter for Unity's Vector2 struct.
	/// </summary>
	public class Vector2JsonConverter : JsonConverter<Vector2>
	{
		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("x");
			writer.WriteValue(value.x);
			writer.WritePropertyName("y");
			writer.WriteValue(value.y);
			writer.WriteEndObject();
		}

		/// <inheritdoc />
		public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var obj = serializer.Deserialize<Vector2Serializable>(reader);
			return (Vector2)obj;
		}
	}

	/// <summary>
	/// JSON converter for Unity's Vector3 struct.
	/// </summary>
	public class Vector3JsonConverter : JsonConverter<Vector3>
	{
		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("x");
			writer.WriteValue(value.x);
			writer.WritePropertyName("y");
			writer.WriteValue(value.y);
			writer.WritePropertyName("z");
			writer.WriteValue(value.z);
			writer.WriteEndObject();
		}

		/// <inheritdoc />
		public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var obj = serializer.Deserialize<Vector3Serializable>(reader);
			return (Vector3)obj;
		}
	}

	/// <summary>
	/// JSON converter for Unity's Vector4 struct.
	/// </summary>
	public class Vector4JsonConverter : JsonConverter<Vector4>
	{
		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("x");
			writer.WriteValue(value.x);
			writer.WritePropertyName("y");
			writer.WriteValue(value.y);
			writer.WritePropertyName("z");
			writer.WriteValue(value.z);
			writer.WritePropertyName("w");
			writer.WriteValue(value.w);
			writer.WriteEndObject();
		}

		/// <inheritdoc />
		public override Vector4 ReadJson(JsonReader reader, Type objectType, Vector4 existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var obj = serializer.Deserialize<Vector4Serializable>(reader);
			return (Vector4)obj;
		}
	}

	/// <summary>
	/// JSON converter for Unity's Quaternion struct.
	/// </summary>
	public class QuaternionJsonConverter : JsonConverter<Quaternion>
	{
		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("x");
			writer.WriteValue(value.x);
			writer.WritePropertyName("y");
			writer.WriteValue(value.y);
			writer.WritePropertyName("z");
			writer.WriteValue(value.z);
			writer.WritePropertyName("w");
			writer.WriteValue(value.w);
			writer.WriteEndObject();
		}

		/// <inheritdoc />
		public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var obj = serializer.Deserialize<Vector4Serializable>(reader);
			return (Quaternion)obj;
		}
	}
}
