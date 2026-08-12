# Schema Migration Sample

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. GameData is pipeline-neutral. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates the real developer workflow for migrating configuration schemas using the **Config Browser** and the `MigrationRunner` API.

## Design Philosophy

Config schemas evolve over time as new features are added. This package provides a robust way to transform legacy data to match current class definitions without losing information or manually editing files.

The sample teaches by letting you explore the real tooling:
1. **Explore**: See how migrations are registered and discovered.
2. **Preview**: Visualize transformations before they are applied.
3. **Apply**: Execute migrations individually or in a chain.

## How Migration Chaining Works

The `MigrationRunner` **automatically chains migrations** based on version numbers. When you click "Preview" on a migration, all migrations from the provider's current version to the target version are applied in sequence.

For example, with provider at version 1:
- Clicking **v1→v2**: Applies only v1→v2
- Clicking **v2→v3**: Applies **both** v1→v2 **and** v2→v3 (chained)

This means you don't need to write explicit "v1→v3" migrations - the runner handles the chain automatically.

## Schema Evolution (SampleEnemyConfig)

The sample follows the evolution of a combat unit config across three versions:

### Version 1 (Original)
- `Id`: int
- `Name`: string
- `Health`: int
- `Damage`: int

### Version 2 (Refactoring)
- **Rename**: `Damage` → `AttackDamage` (more descriptive)
- **New Field**: `ArmorType` (string)
- **Derived Logic**: `ArmorType` is automatically set to "Heavy" if `Health` ≥ 100, otherwise "Medium" or "Light".

### Version 3 (Complexity)
- **Split**: `Health` is split into `BaseHealth` (80%) and `BonusHealth` (20%).
- **New Object**: `Stats` (nested object) containing:
  - `DamageReduction`: derived from `ArmorType`.
  - `CritChance`: derived from `AttackDamage`.
  - `MoveSpeedMultiplier`: derived from `ArmorType`.
- **New Array**: `Abilities` (initialized as empty).

## How to Use

1. **Import the sample** and open the `Migration.unity` scene.
2. **Enter Play Mode**. This initializes a `ConfigsProvider` and sets its internal version to 1.
3. **Open Config Browser** via the button in the scene or `Tools > Game Data > Config Browser`.
4. Select the active provider in the browser.
5. Navigate to the **Migrations** tab.
6. You will see two pending migrations:
   - `SampleEnemyConfigMigration_v1_v2` (State: **Current**)
   - `SampleEnemyConfigMigration_v2_v3` (State: **Pending**)

### Preview Workflow (testing with custom JSON)

7. **Copy the v1 sample JSON** from the scene's output panel (displayed at runtime).
8. **Paste it into the "Custom Input JSON" field** in the Config Browser's Migrations tab.
9. **Select the target version** from the dropdown and click **Preview**:
   - The **Input** panel shows the v1 JSON you pasted
   - The **Output** panel shows the result after applying migrations up to the selected target version

### Apply Workflow (updating provider data)

10. **Select the target version** from the dropdown and click **Apply Migration**:
    - This applies the migration to the actual provider data
    - The provider version updates (e.g., 1 → 2)
    - The **State** column updates to reflect the new version
11. After applying v1→v2, notice:
    - v1→v2 becomes **Applied**
    - v2→v3 becomes **Current** (ready to apply next)

### Sample v1 JSON

You can also use this JSON directly:

```json
{
  "Id": 1,
  "Name": "Orc Warlord",
  "Health": 150,
  "Damage": 25
}
```

This represents a v1-schema config. Paste it into the Custom Input JSON field to see migrations transform it.

## Implementation Details

### Migration Classes
Migrations are implemented by classes inheriting from `IConfigMigration` and marked with the `[ConfigMigration]` attribute. See the `Editor/` folder for examples:
- `SampleEnemyConfigMigration_v1_v2.cs`: Demonstrates renaming and conditional defaults.
- `SampleEnemyConfigMigration_v2_v3.cs`: Demonstrates splitting fields, nested objects, and arrays.

### API Reference
- `MigrationRunner.GetAvailableMigrations<T>()`: Discovers registered migrations for a type.
- `MigrationRunner.Migrate()`: Applies transformations to a `JObject`.
- `MigrationRunner.MigrateScriptableObject()`: High-level helper for `ScriptableObject` assets.
