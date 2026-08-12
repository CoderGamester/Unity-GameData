# GameLovers GameData

Typed game configuration, observable state, controlled JSON serialization, deterministic `floatP` math, and Editor data tools for Unity 6.

[![Unity](https://img.shields.io/badge/Unity-6000.0%20%7C%206000.3%20%7C%206000.5-blue.svg)](https://unity.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)
[![Version](https://img.shields.io/github/v/tag/CoderGamester/Unity-GameData?label=version)](CHANGELOG.md)

## When to use it

Use GameData when game data needs typed lookup, Inspector authoring, change notifications, migration, or controlled JSON serialization. It is pipeline-neutral. It does not provide network synchronization, persistence transport, retries, or a backend API.

## Unity compatibility

| Item | Current policy |
| --- | --- |
| Minimum Unity version | `6000.0` |
| Reference streams | `6000.0.x`, `6000.3.x`, `6000.5.x` |
| Reference editors | `6000.0.81f1`, `6000.3.21f1`, `6000.5.7f1` (primary) |
| Render pipeline | Pipeline-neutral |
| Validation status | Compatibility target; see the repository validation runner before treating a stream as validated. |

## Install

Add this released package to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gamelovers.gamedata": "https://github.com/CoderGamester/Unity-GameData.git#1.0.3"
  }
}
```

Unity resolves the package's registry dependencies (`Newtonsoft.Json`, TextMeshPro, and the performance test framework) from its manifest. The latter two are currently included in the resolved graph; do not describe the install as dependency-free.

## First success

Register keyed and singleton configuration data, then retrieve it by its declared type:

```csharp
using System.Collections.Generic;
using GameLovers.GameData;

var provider = new ConfigsProvider();
provider.AddConfigs(item => item.Id, new List<ItemConfig>
{
    new() { Id = 1, Name = "Potion" }
});

ItemConfig potion = provider.GetConfig<ItemConfig>(1);
```

Choose either a keyed collection or a singleton configuration for a type. Duplicate registrations are errors; validate the authored data before shipping.

## Main concepts

| Area | Use |
| --- | --- |
| `ConfigsProvider` / config containers | Typed keyed and singleton configuration lookup |
| `ObservableField`, `ObservableList`, `ObservableDictionary` | Notify consumers when state changes; dispose subscriptions you own |
| `ComputedField` | Derived observable values from other observables |
| `ConfigsSerializer` / `ConfigTypesBinder` | Controlled JSON payload serialization and deserialization |
| `floatP` / `MathfloatP` | Fixed-point-style deterministic math APIs |
| `ConfigsScriptableObject`, `UnitySerializedDictionary`, `EnumSelector` | Inspector-authored configuration and editor support |

`EnumSelector` stores enum names, so numeric reordering survives; renaming or deleting a member does not. Check `HasValidSelection` before relying on the selected value.

### Serialization security

Register allowed types before deserializing any untrusted input. `TrustedOnly` is appropriate only when the payload is wholly trusted. `Secure` avoids type metadata and is serialize-only; it is not a transport or authentication layer.

```csharp
var serializer = new ConfigsSerializer();
serializer.RegisterAllowedTypes(new[] { typeof(ItemConfig) });
```

## Editor tools and samples

Tools live under `Tools/GameLovers/Game Data/`, including Config Browser and Observable Debugger.

| Sample | What it demonstrates |
| --- | --- |
| Reactive UI Demo (uGUI) | Observable values and collections bound to uGUI |
| Reactive UI Demo (UI Toolkit) | Observable values and collections bound to UI Toolkit |
| Designer Workflow | ScriptableObject configuration, dictionaries, and enum selection |
| Migration | Previewing and applying schema migrations |

Import a sample from Package Manager and read its local README before assuming a migration or UI update happens in one callback; collection changes can produce distinct final callbacks.

## Help and changes

Read [CHANGELOG.md](CHANGELOG.md), open an [issue](https://github.com/CoderGamester/Unity-GameData/issues), and contribute through the repository. The package is MIT licensed; see [LICENSE.md](LICENSE.md).
