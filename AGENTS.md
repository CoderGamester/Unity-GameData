# GameLovers.GameData - AI Agent Guide

## 1. Package Overview
- **Package**: `com.gamelovers.gamedata`
- **Unity**: 6000.0+ (Unity 6)
- **Runtime asmdef**: `Runtime/GameLovers.GameData.asmdef` (**allowUnsafeCode = true**)
- **Dependencies**
  - `com.unity.nuget.newtonsoft-json` (3.2.1): runtime JSON serializer + editor tools/tests
  - `com.cysharp.unitask` (2.5.10): async API in `Runtime/ConfigServices/Interfaces/IConfigBackendService.cs`
  - `com.unity.textmeshpro` (3.0.6): used by **Samples~** UI scripts (`using TMPro;`)

This file is for **agents/contributors**. User-facing usage lives in `README.md`.

## 2. What the package provides (map)
- **Configs**: `ConfigsProvider`, `ConfigsSerializer`, `ConfigsScriptableObject` (`Runtime/ConfigServices/*`)
- **Observables**: field/list/dictionary/computed + resolver variants (`Runtime/Observables/*`)
- **Deterministic math**: `floatP`, `MathfloatP` (`Runtime/Math/*`)
- **Serialization helpers**: `UnitySerializedDictionary`, `SerializableType<T>`, JSON converters (`Runtime/Serialization/*`)
- **Editor tooling (UI Toolkit)**: Config Browser, Observable Debugger, inspectors, migrations (`Editor/*`)

## 3. Key entry points (common navigation)
- **Configs**: `Runtime/ConfigServices/ConfigsProvider.cs`, `Runtime/ConfigServices/ConfigsSerializer.cs`
- **Security**: `Runtime/ConfigServices/ConfigTypesBinder.cs` (whitelist binder for safe deserialization)
- **Interfaces**: `Runtime/ConfigServices/Interfaces/*` (notably `IConfigsProvider`, `IConfigsAdder`, `IConfigBackendService`)
- **ScriptableObject containers**: `Runtime/ConfigServices/ConfigsScriptableObject.cs` (+ `Interfaces/IConfigsContainer.cs`)
- **Editor windows**: `Editor/Windows/ConfigBrowserWindow.cs`, `Editor/Windows/ObservableDebugWindow.cs`
- **Migrations**: `Editor/Migration/*` (`MigrationRunner`, `IConfigMigration`, preview helpers)
- **Inspector/UI Toolkit elements**: `Editor/Inspectors/*`, `Editor/Elements/*`
- **Observable core types**: `Runtime/Observables/ObservableField.cs`, `ObservableList.cs`, `ObservableDictionary.cs`, `ComputedField.cs`
- **Deterministic math**: `Runtime/Math/floatP.cs`, `Runtime/Math/MathfloatP.cs`
- **Serialization helpers**: `Runtime/Serialization/*` (Unity dict, type serialization, converters)
- **Tests**: `Tests/Editor/*` (Unit/Integration/Regression/Security/Performance/Smoke/Boundary)

## 4. Important behaviors / gotchas (keep in sync with code)
- **Singleton vs id-keyed**: `GetConfig<T>()` only for singleton; use `GetConfig<T>(int)` for id-keyed.
- **Duplicate keys throw**: `AddSingletonConfig<T>()` / `AddConfigs<T>()` throw on duplicate ids.
- **One role per type**: `ConfigsProvider._configs` is a `Dictionary<Type, IEnumerable>`. A type `T` can be registered EITHER as a singleton (`AddSingletonConfig<T>`) OR as a keyed collection (`AddConfigs<T>`), never both. Calling the second after the first raises `ArgumentException` from `Dictionary.Add`. When a test or caller genuinely needs both singleton-shaped and collection-shaped validation on the same schema, declare two sibling types (see `MockValidatableConfig` / `MockValidatableConfigAlt` in the test fixtures).
- **Missing container throws**: `GetConfigsDictionary<T>()` assumes `T` was added.
- **Versioning is `ulong`**: `ConfigsSerializer.Deserialize` parses `Version` with `ulong.TryParse`; non-numeric strings become `0`.
- **Security** (see `ConfigsSerializer`, `ConfigTypesBinder`):
  - `ConfigsSerializer(TrustedOnly)` uses `TypeNameHandling.Auto` with a `ConfigTypesBinder` whitelist.
  - Types are auto-registered during `Serialize()` and can be pre-registered via `RegisterAllowedTypes()`.
  - `ConfigTypesBinder` blocks any type not explicitly whitelisted, preventing type injection attacks.
  - `ConfigsSerializer(Secure)` disables `TypeNameHandling` entirely (serialize-only, cannot round-trip).
  - `MaxDepth` (default: 128) prevents stack overflow from deeply nested JSON.
- **Editor-only registries**: `ConfigsProvider` registers in editor builds (used by Config Browser).
- **ConfigsScriptableObject keys must be unique**: duplicate keys throw during `OnAfterDeserialize()`.
- **ObservableDictionary update flags**: update flags change which subscribers are notified (key-only vs global vs both).
- **Missing key calls can throw**: methods that target a specific key generally assume the key exists.
- **EnumSelector stability**: check validity (`HasValidSelection`) if enums were changed/renamed.
- **No manual `.meta` edits**: Unity owns `*.meta` generation.

## 5. Editor tools (menu paths)
- **Config Browser**: `Tools > Game Data > Config Browser` (browse/validate/export/migrations)
- **Observable Debugger**: `Tools > Game Data > Observable Debugger` (inspect live observables)
- **Config migrations**: shown inside Config Browser when migrations exist

## 6. Tests (how to run / where to add)
- **Run**: Unity Test Runner → **EditMode** (tests live under `Tests/Editor/*`)
- **Placement**:
  - `Unit/` for pure logic (preferred)
  - `Integration/` for editor/tooling interactions
  - `Security/` for serializer safety expectations
  - `Performance/` only when measuring allocations/hot paths

## 7. Common change workflows
- **Add config type**: `[Serializable]` (or `[IgnoreServerSerialization]`), decide singleton vs id-keyed, add tests.
- **Change serialization**: update `ConfigsSerializer` + converters; adjust `Security/*` tests.
- **Change observables**: keep hot paths allocation-free; test subscribe/unsubscribe and update ordering.
- **Editor UX**: keep editor-only code under `Editor/` and avoid runtime `UnityEditor` refs.

## 8. Samples (maintenance rule)
- Package samples live in `Samples~/...`.
- In the **host Unity project**, imported samples live under `Assets/Samples/...` and should be edited there when validating sample behavior.
- Current sample set (see `package.json`): Reactive UI Demo (uGUI), Reactive UI Demo (UI Toolkit), Designer Workflow, Migration.

## 9. Release checklist (docs + versioning)
- Bump `package.json` version
- Update `CHANGELOG.md` (include upgrade notes when breaking)
- Ensure samples compile (TextMeshPro is required by sample scripts)
- Run EditMode tests
- Update `README.md` if public API / behavior changed

## 10. External package sources (preferred for API lookups)
- Newtonsoft: `Library/PackageCache/com.unity.nuget.newtonsoft-json/`
- UniTask: `Library/PackageCache/com.cysharp.unitask/`
- TextMeshPro: `Library/PackageCache/com.unity.textmeshpro/`

## 11. Coding standards / assembly boundaries
- **C#**: C# 9.0 syntax; explicit namespaces; no global usings.
- **Runtime vs Editor**: runtime code must not reference `UnityEditor`; editor tooling stays under `Editor/`.
- **Performance**: avoid allocations in observable hot paths; prefer tests for allocation regressions when changing core types.

## 12. IL2CPP / AOT / stripping
- `Runtime/link.xml` exists to prevent stripping of core serialization logic.
- Keep `SerializableType<T>` and serializer-related reflection IL2CPP-safe; avoid adding reflection-heavy APIs without tests.

## 13. When to update docs
- Update `AGENTS.md` when behavior/entry points change (configs, serializer security, observables, editor tools, tests layout).
- Update `README.md` when public API/usage changes (installation, examples, requirements).
- Update `CHANGELOG.md` for notable changes, especially breaking upgrades.

## 14. Quick verification (before shipping changes)
- Run **EditMode** tests and ensure no new warnings/errors.
- Import samples once in a host project and confirm they compile (TextMeshPro).
- If touching serialization, check `Tests/Editor/Security/*` and confirm untrusted payload expectations still hold.
