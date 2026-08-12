# Reactive UI Demo (UI Toolkit)

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. GameData is pipeline-neutral. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates how to build **reactive user interfaces** using Unity's **UI Toolkit** with the observable data types from GameLovers.GameData.

## Design Philosophy

UI Toolkit provides a modern, CSS-like approach to UI development. This sample shows how to integrate reactive data binding:
1. **Element queries**: Find elements by name and bind data to them.
2. **View helpers**: Encapsulate subscription logic in reusable classes.
3. **Clean disposal**: Properly clean up subscriptions when views are destroyed.

## Sample Content

The sample showcases a player stats panel with reactive bindings (same as the uGUI demo, but using UI Toolkit):

### Health Bar
Bound to an `ObservableField<int>` that updates a ProgressBar and Label automatically.

### Stats Panel
Displays `BaseDamage`, `WeaponBonus`, and a computed `TotalDamage` that updates when either source value changes.

### Inventory List
Bound to an `ObservableList<string>` that reacts to add/remove operations in a ScrollView.

### Batch Update
Demonstrates `ObservableBatch` to group multiple changes into a single UI refresh.

## How to Use

1. **Import the sample** and open the `ReactiveToolkitDemo.unity` scene.
2. **Enter Play Mode** to see the reactive UI in action.
3. **Interact with the buttons**:
   - **Damage/Heal**: Modify health and watch the progress bar update.
   - **+Base Damage / +Weapon Bonus**: See the computed TotalDamage update automatically.
   - **Add/Remove Item**: Watch the inventory ScrollView react to collection changes.
   - **Batch Update**: Apply multiple changes with a single UI refresh.

## Implementation Details

### Data Model
`PlayerData.cs` defines the observable properties:
- `ObservableField<int>`: Single values (Health, BaseDamage, WeaponBonus).
- `ObservableList<string>`: Collection (Inventory).
- `ComputedField<int>`: Derived value (TotalDamage = BaseDamage + WeaponBonus).

### View Helpers
Plain C# classes that implement `IDisposable` for clean subscription management. See the `Scripts/` folder:
- `ReactiveToolkitHealthBar.cs`: Binds a ProgressBar and Label to health.
- `ReactiveToolkitStatsPanel.cs`: Binds labels to damage stats.
- `ReactiveToolkitInventoryList.cs`: Binds a ScrollView to the inventory list.

### UI Assets
The `UI/` folder contains:
- `ReactiveToolkitDemo.uxml`: UI layout definition.
- `ReactiveToolkitDemo.uss`: Stylesheet for visual styling.
