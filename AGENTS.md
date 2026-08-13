# GameLovers GameData — Agent Guide

This guide adds package-specific rules to the host repository guide. Consumer usage belongs in `README.md`.

## Scope

- Package: `com.gamelovers.gamedata`; minimum Unity version and dependencies are authoritative in `package.json`.
- Runtime assembly: `Runtime/GameLovers.GameData.asmdef` with unsafe code enabled.
- Main areas: configuration storage/serialization, observables, deterministic `floatP` math, serialization helpers, and UI Toolkit editor tools.
- This package is render-pipeline-neutral. Do not add URP, HDRP, or Built-in rendering dependencies.

## Runtime invariants

- `ConfigsProvider` stores one role per config type. A type may be registered as a singleton or as an id-keyed collection, never both. Duplicate registrations and duplicate ids throw.
- Use `GetConfig<T>()` only for singleton configs and `GetConfig<T>(id)` for keyed configs. Missing containers are errors, not default-value lookups.
- Config versions are `ulong`; invalid serialized version strings deserialize as `0`.
- `ConfigsSerializer(TrustedOnly)` uses `ConfigTypesBinder` as an explicit type whitelist. Preserve the whitelist and `MaxDepth` protections when changing serialization. `Secure` mode disables type metadata and is not a round-trip mode.
- Trusted-only serialization auto-registers encountered types during `Serialize()` and supports explicit pre-registration through `RegisterAllowedTypes()`. Deserialization must never widen the binder implicitly from untrusted payload content.
- `ConfigsScriptableObject` keys must remain unique after deserialization.
- `ObservableDictionary` update flags determine key-specific versus global notification fan-out. Preserve subscription order and avoid allocations in observable hot paths.
- Operations targeting a specific observable key generally assume the key exists and may throw. Do not silently convert those contracts into default-value behavior.
- `EnumSelector<T>` may retain a serialized name that no longer exists after an enum rename. Consumers and tests use `HasValidSelection` before trusting the selected value.
- `SerializableType<T>.OnAfterDeserializeImpl()` exists to avoid mutating a boxed struct through `ISerializationCallbackReceiver`. Do not fold the implementation back into the explicit interface method.
- `Runtime/link.xml` protects serialization code from stripping. Treat reflection additions as IL2CPP/AOT work and add relevant tests.

## Editor and test boundaries

- Editor tooling stays under `Editor/`; runtime code must not reference `UnityEditor`.
- Internal test seams are exposed through `InternalsVisibleTo`. Prefer `EnumSelector<T>.SetSelectionString` for stale enum names, `SerializableType<T>.FromSerializedNames` for Unity deserialization order, and `UnitySerializedDictionary.SetSerializedLists` plus its read-only internal list accessors over private reflection.
- Resolver field behavior is intentionally tested alongside `ObservableField<T>` in `ObservableFieldTest`; resolver list and dictionary types have dedicated fixtures. Grep before adding apparent “missing” resolver coverage.
- Before changing anything under `Tests/`, read `Tests/AGENTS.md`.

## Samples and dependencies

- Samples live under `Samples~/`; their authoritative list is `package.json`.
- Sample UI code depends on TextMeshPro. Validate sample compilation after changes affecting public APIs or dependencies.
- Prefer local dependency sources under `Library/PackageCache/`, including Newtonsoft.Json and TextMeshPro.

## Verification and documentation

- Serialization changes require unit and security coverage; observable changes require subscribe/unsubscribe, ordering, and allocation checks where relevant.
- Update `README.md` for consumer-facing API or setup changes and `CHANGELOG.md` for notable behavior changes.
- Update this guide only when a durable package invariant, assembly boundary, or test convention changes.
