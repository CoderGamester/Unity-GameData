# GameLovers GameData

[![Unity Version](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/github/v/tag/CoderGamester/com.gamelovers.gamedata?label=version)](CHANGELOG.md)

> **Quick Links**: [Installation](#installation) | [Features](#features-documentation) | [Editor Tools](#editor-tools) | [Contributing](#contributing)

## Why Use This Package?

Managing game data in Unity often leads to fragmented solutions: scattered config files, tight coupling between data and logic, and cross-platform inconsistencies. This **GameData** package addresses these challenges:

| Problem | Solution |
|---------|----------|
| **Scattered config management** | Type-safe `ConfigsProvider` with O(1) lookups and versioning |
| **Tight coupling to data changes** | Observable types (`ObservableField`, `ObservableList`, `ObservableDictionary`) for reactive programming |
| **Manual derived state updates** | `ComputedField` for auto-updating calculated values with dependency tracking |
| **Cross-platform float inconsistencies** | Deterministic `floatP` type for reproducible calculations across all platforms |
| **Backend sync complexity** | Built-in JSON serialization with `ConfigsSerializer` for client/server sync |
| **Dictionary Inspector editing** | `UnitySerializedDictionary` for seamless Inspector support |
| **Fragile enum serialization** | `EnumSelector` stores enum names (not values) to survive enum changes |

**Built for production:** Minimal dependencies. Zero per-frame allocations in observable types. Used in real games.

---

## System Requirements

- **[Unity](https://unity.com/download)** 6000.0+ (Unity 6)
- **[Newtonsoft.Json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html)** (com.unity.nuget.newtonsoft-json v3.2.1) — automatically resolved
- **[TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html)** (com.unity.textmeshpro v3.0.6) — used by **Samples~** UI scripts only

| Unity Version | Status |
|---|---|
| 6000.0+ (Unity 6) | ✅ Fully Tested |
| 2022.3 LTS | ⚠️ Untested |

## Installation

### Via Unity Package Manager (Recommended)

1. Open Unity Package Manager (`Window` → `Package Manager`)
2. Click `+` → `Add package from git URL`
3. Enter: `https://github.com/CoderGamester/com.gamelovers.gamedata.git`

### Via manifest.json

```json
{
  "dependencies": {
    "com.gamelovers.gamedata": "https://github.com/CoderGamester/com.gamelovers.gamedata.git"
  }
}
```

---

## Key Components

| Component | Responsibility |
|-----------|----------------|
| **ConfigsProvider** | Type-safe config storage with O(1) lookups and versioning |
| **ConfigsSerializer** | JSON serialization for client/server config synchronization |
| **ConfigTypesBinder** | Whitelist-based type binder for secure deserialization |
| **ObservableField** | Reactive wrapper for single values with change callbacks |
| **ObservableList** | Reactive wrapper for lists with add/remove/update callbacks |
| **ObservableDictionary** | Reactive wrapper for dictionaries with key-based callbacks |
| **ComputedField** | Auto-updating derived values that track dependencies |
| **floatP** | Deterministic floating-point type for cross-platform math |
| **MathfloatP** | Math functions (Sin, Cos, Sqrt, etc.) for `floatP` |
| **EnumSelector** | Enum dropdown that survives enum value changes |
| **UnitySerializedDictionary** | Dictionary type visible in Unity Inspector |

---

## Editor Tools

| Tool | Menu | Purpose |
|------|------|---------|
| **Config Browser** | `Tools > Game Data > Config Browser` | Browse configs, validate, export JSON, preview migrations |
| **Observable Debugger** | `Tools > Game Data > Observable Debugger` | Inspect live observables in play mode |
| **ConfigsScriptableObject Inspector** | Inspector (automatic) | Inline duplicate-key validation and Validate All action |

---

## Features Documentation

### ConfigsProvider

Type-safe, high-performance configuration storage.

```csharp
var provider = new ConfigsProvider();
provider.AddConfigs(item => item.Id, itemConfigs);
provider.AddSingletonConfig(new GameSettings { Difficulty = 2 });

var item     = provider.GetConfig<ItemConfig>(42);
var settings = provider.GetConfig<GameSettings>();

// Zero-allocation enumeration
foreach (var enemy in provider.EnumerateConfigs<EnemyConfig>())
    ProcessEnemy(enemy);
```

### ConfigsSerializer

JSON serialization with security modes.

```csharp
var serializer = new ConfigsSerializer(); // TrustedOnly by default
string json    = serializer.Serialize(provider, "123");
var restored   = serializer.Deserialize<ConfigsProvider>(json);
serializer.RegisterAllowedTypes(new[] { typeof(EnemyConfig) });
```

### ObservableField

```csharp
var score = new ObservableField<int>(0);
score.Observe((prev, curr) => UpdateScoreUI(curr));
score.InvokeObserve((prev, curr) => UpdateScoreUI(curr)); // invokes immediately too
score.Value = 100;
score.StopObservingAll(this);
```

### ComputedField

```csharp
var baseHp  = new ObservableField<int>(100);
var bonus   = new ObservableField<int>(25);
var totalHp = new ComputedField<int>(() => baseHp.Value + bonus.Value);
totalHp.Observe((prev, curr) => Debug.Log($"HP: {curr}"));
baseHp.Value = 120; // totalHp auto-updates to 145
totalHp.Dispose();
```

### ObservableList / ObservableDictionary

```csharp
var inventory = new ObservableList<string>(new List<string>());
inventory.Observe((index, prev, curr, type) => RefreshUI(index, curr));
inventory.Add("Sword");

var stats = new ObservableDictionary<string, int>(new Dictionary<string, int>());
stats.Observe("health", (key, prev, curr, type) => Debug.Log($"{key}: {curr}"));
stats.Add("health", 100);
```

### Deterministic floatP

```csharp
floatP a      = 3.14f;
floatP sum    = a + 2.0f;
float result  = (float)sum;
uint raw      = a.RawValue;           // bit-exact for determinism
floatP copy   = floatP.FromRaw(raw);
```

### UnitySerializedDictionary / EnumSelector

```csharp
[Serializable]
public class StringIntDictionary : UnitySerializedDictionary<string, int> { }

[Serializable]
public class ItemTypeSelector : EnumSelector<ItemType>
{
    public ItemTypeSelector() : base(ItemType.Weapon) { }
}
// ItemType type = selector; — implicit conversion
// bool ok = selector.HasValidSelection();
```

---

## Samples

Import via **Package Manager → GameLovers GameData → Samples**

| Sample | Demonstrates |
|--------|-------------|
| **Reactive UI Demo** | `ObservableField`, `ObservableList`, `ComputedField`, batched updates — uGUI and UI Toolkit |
| **Designer Workflow** | `ConfigsScriptableObject`, `UnitySerializedDictionary`, `EnumSelector` with PropertyDrawer |
| **Migration** | `IConfigMigration`, `MigrationRunner`, Config Browser migration workflow |

---

## Contributing

Contributions are welcome! Report bugs or request features via [GitHub Issues](https://github.com/CoderGamester/com.gamelovers.gamedata/issues). For development setup, architecture, coding standards, and test placement, see [AGENTS.md](AGENTS.md).

---

## Related docs

| Document | Purpose |
|---|---|
| [AGENTS.md](AGENTS.md) | Contributor/agent guide (architecture, gotchas, workflows) |
| [CHANGELOG.md](CHANGELOG.md) | Version history |

## Support

- **Issues**: [Report bugs or request features](https://github.com/CoderGamester/com.gamelovers.gamedata/issues)
- **Discussions**: [Ask questions and share ideas](https://github.com/CoderGamester/com.gamelovers.gamedata/discussions)

## License

MIT — see [LICENSE.md](LICENSE.md).
