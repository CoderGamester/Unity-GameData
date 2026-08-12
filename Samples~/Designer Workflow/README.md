# Designer Workflow Sample

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. GameData is pipeline-neutral. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates how game designers can author configuration data using **ScriptableObjects** in the Unity Editor, which are then loaded into the runtime `ConfigsProvider` system.

## Design Philosophy

Game designers need visual, Inspector-friendly tools to edit game data without touching code. This sample bridges that gap by:
1. **ScriptableObject Assets**: Designer-editable assets stored in the project.
2. **ConfigsProvider Integration**: Runtime loading into the typed config system.
3. **Custom Property Drawers**: Enhanced Inspector UX with dropdowns.

## Sample Content

The sample includes three types of configuration patterns:

### Singleton Config (GameSettings)
Global settings with a single value, such as difficulty and master volume.

### ID-Keyed Collection (EnemyConfigs)
Multiple configs indexed by ID, containing enemy stats like health and damage.

### Dictionary Config (LootTable)
Key-value pairs using `UnitySerializedDictionary` for drop rates per item type. Uses `EnumSelector` for stable enum serialization by name.

## How to Use

1. **Import the sample** and open the `DesignerWorkflow.unity` scene.
2. **Enter Play Mode** to see the configs loaded and displayed.
3. **Edit the assets** in `Assets/Resources/` via the Inspector:
   - Modify enemy stats in `SampleEnemyConfigs`
   - Adjust difficulty/volume in `SampleGameSettings`
   - Edit drop rates in `SampleLootTable`
4. **Click Reload** in the scene to see changes reflected at runtime.

## Implementation Details

### Config Assets
ScriptableObject wrappers that store config data. See the `Scripts/` folder:
- `GameSettingsAsset.cs`: Singleton config using `AddSingletonConfig<T>()`.
- `EnemyConfigsAsset.cs`: ID-keyed configs using `AddConfigs<T>()`.
- `LootTableAsset.cs`: Dictionary config using `UnitySerializedDictionary`.

### EnumSelector
`ItemTypeSelector` extends `EnumSelector<ItemType>` to store enum values by **name** rather than numeric value, preventing data corruption when enum values are reordered.

### Custom Property Drawer
`ItemTypeSelectorPropertyDrawer.cs` in the `Editor/` folder shows how to create dropdown UIs for `EnumSelector` subclasses.
