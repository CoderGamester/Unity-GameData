using System;
using System.Collections.Generic;
using GameLovers.GameData;
using Newtonsoft.Json;
using NUnit.Framework;

namespace GameLovers.GameData.Tests.Security
{
	/// <summary>
	/// Security tests for ConfigsSerializer verifying protection against:
	/// - Type injection attacks via $type metadata
	/// - Malformed JSON payloads
	/// - Stack overflow from deeply nested JSON
	/// </summary>
	[TestFixture]
	public class ConfigsSerializerSecurityTest
	{
		[Serializable]
		public class BaseConfig { public int Id; }
		[Serializable]
		public class DerivedConfig : BaseConfig { public string Extra; }
		
		// A type that should NEVER be allowed during deserialization
		[Serializable]
		public class MaliciousConfig { public string Payload; }

		private ConfigsProvider _provider;

		[SetUp]
		public void Setup()
		{
			_provider = new ConfigsProvider();
		}

		[Test]
		public void SecureMode_TypeNameHandlingNone_Verified()
		{
			var serializer = new ConfigsSerializer(SerializationSecurityMode.Secure);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 1, Extra = "Data" });
			
			var json = serializer.Serialize(_provider, "1");
			
			// Should NOT contain $type
			Assert.IsFalse(json.Contains("$type"));
		}

		[Test]
		// ADMIT: ConfigsSerializer in TrustedOnly mode must select TypeNameHandling.Auto, or the polymorphic $type
		// metadata the round trip depends on is never written.
		// RCR: ConfigsSerializer.cs ctor — collapse the TypeNameHandling ternary to `TypeNameHandling.None` → RED
		// (no $type in the payload). Broad: the whole TrustedOnly round-trip suite depends on this line. 2026-08-02
		public void TrustedOnlyMode_TypeNameHandlingAuto_Verified()
		{
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 1, Extra = "Data" });
			
			var json = serializer.Serialize(_provider, "1");
			
			// SHOULD contain $type for polymorphic serialization
			Assert.IsTrue(json.Contains("$type"));
		}

		[Test]
		// ADMIT: ConfigTypesBinder.BindToType must reject a resolvable-but-unregistered type with its own
		// specific message — asserting only that the text contains the payload's own type name is unfalsifiable (D1).
		// RCR: ConfigTypesBinder.cs BindToType — reword the rejection message → RED (Assert.AreEqual on the exact
		// message text). 2026-08-01
		public void TrustedOnlyMode_BinderBlocksUnregisteredTypes()
		{
			// Create serializer and serialize DerivedConfig (which registers it)
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 10, Extra = "Data" });
			var json = serializer.Serialize(_provider, "1");

			// Now try to inject a different type by modifying the JSON
			// Replace DerivedConfig type reference with MaliciousConfig
			// Use FullName because Newtonsoft doesn't serialize with full AssemblyQualifiedName
			var maliciousJson = json.Replace(
				typeof(DerivedConfig).FullName,
				typeof(MaliciousConfig).FullName);

			// The binder should reject MaliciousConfig because it was never registered
			var newProvider = new ConfigsProvider();
			var ex = Assert.Throws<JsonSerializationException>(() =>
				serializer.Deserialize(maliciousJson, newProvider));

			// The replace above swaps DerivedConfig for MaliciousConfig inside the $type metadata of the
			// singleton's Dictionary<int, TConfig> wrapper, so the type actually resolved and rejected by
			// the binder is the closed generic Dictionary<int, MaliciousConfig>, not bare MaliciousConfig.
			// Newtonsoft wraps the binder's JsonSerializationException with positional context; the
			// binder's own, precise message survives unmodified as the InnerException.
			var expectedMessage = $"Type '{typeof(Dictionary<int, MaliciousConfig>).FullName}' is not allowed for deserialization. " +
				"Only whitelisted config types are permitted for security reasons.";

			Assert.AreEqual(expectedMessage, ex.InnerException?.Message);
		}

		[Test]
		// ADMIT: ConfigsSerializer.Serialize must auto-register each serialized type on its binder in TrustedOnly
		// mode, or its own payload is rejected on the way back in.
		// RCR: ConfigsSerializer.cs Serialize — change the `_securityMode == TrustedOnly` auto-register guard to
		// `if (false)` → RED (JsonSerializationException naming the un-whitelisted type). Also reddens the
		// ConfigsSerializerTest round trips. 2026-08-02
		public void TrustedOnlyMode_RoundTrip_Works()
		{
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 10, Extra = "Data" });
			
			var json = serializer.Serialize(_provider, "1");
			
			var newProvider = new ConfigsProvider();
			serializer.Deserialize(json, newProvider);
			
			var cfg = newProvider.GetConfig<DerivedConfig>();
			Assert.AreEqual(10, cfg.Id);
			Assert.AreEqual("Data", cfg.Extra);
		}

		[Test]
		// ADMIT: ConfigsSerializer in Secure mode must select TypeNameHandling.None — no $type is emitted, and the
		// documented consequence is that the payload cannot be read back.
		// RCR: ConfigsSerializer.cs ctor — collapse the TypeNameHandling ternary to `TypeNameHandling.Auto` → RED
		// (both halves fail: $type appears and the deserialize no longer throws). 2026-08-02
		public void SecureMode_CannotRoundTrip_ExpectedLimitation()
		{
			// Secure mode uses TypeNameHandling.None, which means the internal
			// Dictionary<Type, IEnumerable> structure cannot be round-tripped
			// because the deserializer doesn't know the concrete type.
			//
			// This is a documented limitation - Secure mode is for serialize-only
			// scenarios (e.g., sending configs TO untrusted clients).
			
			var secureSerializer = new ConfigsSerializer(SerializationSecurityMode.Secure);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 10, Extra = "Data" });
			
			var secureJson = secureSerializer.Serialize(_provider, "1");
			
			// Verify no $type metadata
			Assert.IsFalse(secureJson.Contains("$type"), "Secure mode should NOT include $type metadata");
			
			// Verify deserialization fails (expected limitation)
			var secureProvider = new ConfigsProvider();
			Assert.Throws<JsonSerializationException>(() => secureSerializer.Deserialize(secureJson, secureProvider),
				"Secure mode cannot round-trip because IEnumerable requires $type to know concrete type");
		}

		[Test]
		// ADMIT: ConfigsSerializer.RegisterAllowedTypes must actually feed the supplied types to the binder — it is
		// the only way a serializer that never called Serialize can accept a foreign payload.
		// RCR: ConfigsSerializer.cs RegisterAllowedTypes — change the loop source to `Enumerable.Empty<Type>()` →
		// RED (JsonSerializationException naming DerivedConfig). Also reddens MaxDepth_PreventsStackOverflow.
		// 2026-08-02
		public void RegisterAllowedTypes_AllowsTypesForDeserialization()
		{
			// Create a serializer and manually register types
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			serializer.RegisterAllowedTypes(new[] { typeof(DerivedConfig) });
			
			// Serialize using another serializer
			var otherSerializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 5, Extra = "Test" });
			var json = otherSerializer.Serialize(_provider, "1");
			
			// Should be able to deserialize because DerivedConfig was registered
			var newProvider = new ConfigsProvider();
			serializer.Deserialize(json, newProvider);
			
			var cfg = newProvider.GetConfig<DerivedConfig>();
			Assert.AreEqual(5, cfg.Id);
		}

		[Test]
		// ADMIT: ConfigsSerializer.Deserialize must let Newtonsoft's JsonReaderException escape rather than swallow
		// a truncated payload and continue with a null result.
		// RCR: ConfigsSerializer.cs Deserialize — wrap the DeserializeObject call in
		// `try { ... } catch (JsonReaderException) { }` → RED (NullReferenceException arrives instead of the expected
		// JsonReaderException). 2026-08-02
		public void Deserialize_MalformedJson_ThrowsGracefully()
		{
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			var json = "{\"Version\":\"1\", \"Configs\": { unclosed";
			
			Assert.Throws<JsonReaderException>(() => serializer.Deserialize(json, _provider));
		}

		[Test]
		public void Deserialize_UnexpectedType_ThrowsOrIgnores()
		{
			var serializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			var json = "{\"Version\":\"1\", \"Configs\": \"NotADictionary\"}";
			
			Assert.Throws<JsonSerializationException>(() => serializer.Deserialize(json, _provider));
		}

		[Test]
		// ADMIT: ConfigsSerializer's constructor must pass MaxDepth to Newtonsoft so JsonReader.Push rejects
		// deeply-nested payloads. The probe nests inside an UNDECLARED property so MissingMemberHandling.Ignore
		// skips it — still traversing depth — instead of failing an earlier type conversion that would throw
		// regardless of MaxDepth and make this test a tautology.
		// RCR: ConfigsSerializer.cs ctor — delete `MaxDepth = maxDepth,` → RED (the maxDepth:8 case throws
		// nothing). The maxDepth:256 negative control below must stay green either way. 2026-08-02
		public void MaxDepth_PreventsStackOverflow()
		{
			// Build a valid, Type-keyed payload with a generously high max depth first, so the JSON's
			// Type key and Dictionary<int, DerivedConfig> wrapper are well-formed and registered.
			var templateSerializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly, maxDepth: 256);
			_provider.AddSingletonConfig(new DerivedConfig { Id = 1, Extra = "Data" });
			var validJson = templateSerializer.Serialize(_provider, "1");

			// Append an extra, UNRECOGNIZED property nested far deeper than the low maxDepth below.
			// `SerializedConfigs` only declares `Version`/`Configs`, so this property is skipped rather
			// than populated into any concrete-typed field — the only thing that can trip is depth.
			var deepOpen = new string('[', 20);
			var deepClose = new string(']', 20);
			var deeplyNestedJson = validJson.Substring(0, validJson.Length - 1)
				+ $",\"UnusedProbe\":{deepOpen}1{deepClose}}}";

			var lowDepthSerializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly, maxDepth: 8);
			// Pre-register DerivedConfig on THIS serializer's own binder (auto-registration only happens
			// on the serializer instance that actually calls Serialize) so the $type metadata is accepted
			// and the deeply-nested probe is what trips the guard, not an unrelated binder rejection.
			lowDepthSerializer.RegisterAllowedTypes(new[] { typeof(DerivedConfig) });

			Assert.Throws<JsonReaderException>(() => lowDepthSerializer.Deserialize(deeplyNestedJson, _provider));

			// Negative control: the identical 20-level payload must NOT throw when maxDepth is generously
			// high. Without this half, a serializer that threw on ANY nested array regardless of maxDepth
			// would still pass the assertion above — this is what proves genuine sensitivity to the
			// parameter under test, not just "some exception, for some reason."
			var highDepthSerializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly, maxDepth: 256);
			highDepthSerializer.RegisterAllowedTypes(new[] { typeof(DerivedConfig) });

			Assert.DoesNotThrow(() => highDepthSerializer.Deserialize(deeplyNestedJson, _provider));
		}

		[Test]
		// ADMIT: ConfigsSerializer.RegisterAllowedTypesFromProvider — the production path for pre-populating the
		// allowlist from a live IConfigsProvider — was untested; only the RegisterAllowedTypes overload had coverage.
		// RCR: ConfigsSerializer.cs RegisterAllowedTypesFromProvider — change the loop source to
		// Enumerable.Empty<Type>() → RED (DerivedConfig rejected). Mutating the AddAllowedType call alone does NOT
		// redden: ConfigTypesBinder re-adds the bare type recursively via the Dictionary<int,T> registration. 2026-08-01
		public void Deserialize_TypesRegisteredFromProvider_AcceptsThemAndTheirIntKeyedDictionary()
		{
			var sourceProvider = new ConfigsProvider();
			sourceProvider.AddSingletonConfig(new DerivedConfig { Id = 42, Extra = "FromProvider" });

			// Produce valid JSON naming DerivedConfig and its Dictionary<int, DerivedConfig> wrapper.
			var producingSerializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			var json = producingSerializer.Serialize(sourceProvider, "3");

			// A brand-new serializer whose binder has never seen DerivedConfig via its own Serialize()
			// call. RegisterAllowedTypesFromProvider is the ONLY thing that should let it accept the
			// payload.
			var consumingSerializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			consumingSerializer.RegisterAllowedTypesFromProvider(sourceProvider);

			var targetProvider = new ConfigsProvider();
			consumingSerializer.Deserialize(json, targetProvider);

			var cfg = targetProvider.GetConfig<DerivedConfig>();
			Assert.AreEqual(42, cfg.Id);
			Assert.AreEqual("FromProvider", cfg.Extra);
		}

		[Test]
		// ADMIT: ConfigsSerializer.RegisterAllowedTypesFromProvider must reject a type absent from the source
		// provider with the same security message pinned in TrustedOnlyMode_BinderBlocksUnregisteredTypes.
		// RCR: ConfigsSerializer.cs RegisterAllowedTypesFromProvider — replace the loop body with an
		// allow-everything shortcut → RED (no throw; MaliciousConfig gets constructed). 2026-08-01
		public void Deserialize_TypeNotRegisteredByProvider_ThrowsAndDoesNotConstructIt()
		{
			// sourceProvider only ever knows about DerivedConfig; MaliciousConfig is never part of it.
			var sourceProvider = new ConfigsProvider();
			sourceProvider.AddSingletonConfig(new DerivedConfig { Id = 1, Extra = "Data" });

			var producingSerializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			var json = producingSerializer.Serialize(sourceProvider, "1");
			var maliciousJson = json.Replace(
				typeof(DerivedConfig).FullName,
				typeof(MaliciousConfig).FullName);

			var consumingSerializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			consumingSerializer.RegisterAllowedTypesFromProvider(sourceProvider);

			var targetProvider = new ConfigsProvider();
			var ex = Assert.Throws<JsonSerializationException>(() =>
				consumingSerializer.Deserialize(maliciousJson, targetProvider));

			var expectedMessage = $"Type '{typeof(Dictionary<int, MaliciousConfig>).FullName}' is not allowed for deserialization. " +
				"Only whitelisted config types are permitted for security reasons.";

			Assert.AreEqual(expectedMessage, ex.InnerException?.Message);
		}

		[Test]
		// ADMIT: ConfigsSerializer.SecurityMode must report the mode the instance was constructed with — callers
		// branch on it to decide whether a round trip is possible.
		// RCR: ConfigsSerializer.cs SecurityMode — hard-code `=> SerializationSecurityMode.Secure;` → RED (the
		// TrustedOnly instance reports Secure). 2026-08-02
		public void SecurityMode_Property_ReturnsCorrectMode()
		{
			var trustedSerializer = new ConfigsSerializer(SerializationSecurityMode.TrustedOnly);
			var secureSerializer = new ConfigsSerializer(SerializationSecurityMode.Secure);
			
			Assert.AreEqual(SerializationSecurityMode.TrustedOnly, trustedSerializer.SecurityMode);
			Assert.AreEqual(SerializationSecurityMode.Secure, secureSerializer.SecurityMode);
		}
	}
}
