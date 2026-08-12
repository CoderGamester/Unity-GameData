# Reactive UI Demo (uGUI)

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. GameData is pipeline-neutral. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates how to build **reactive user interfaces** using Unity's legacy UI system (uGUI) with the observable data types from GameLovers.GameData.

## Design Philosophy

Traditional UI code often becomes tangled with manual update calls. Reactive patterns solve this by:
1. **Data drives UI**: Views subscribe to data changes and update automatically.
2. **Decoupled architecture**: Data models know nothing about their views.
3. **Batched updates**: Multiple changes can be grouped to reduce UI thrashing.

## Sample Content

The sample showcases a player stats panel with reactive bindings:

### Health Bar
Bound to an `ObservableField<int>` that updates the slider and label automatically.

### Stats Panel
Displays `BaseDamage`, `WeaponBonus`, and a computed `TotalDamage` that updates when either source value changes.

### Inventory List
Bound to an `ObservableList<string>` that reacts to add/remove operations.

### Batch Update
Demonstrates `ObservableBatch` to group multiple changes into a single UI refresh.

## How to Use

1. **Import the sample** and open the `ReactiveUGuiDemo.unity` scene.
2. **Enter Play Mode** to see the reactive UI in action.
3. **Interact with the buttons**:
   - **Damage/Heal**: Modify health and watch the health bar update.
   - **+Base Damage / +Weapon Bonus**: See the computed TotalDamage update automatically.
   - **Add/Remove Item**: Watch the inventory list react to collection changes.
   - **Batch Update**: Apply multiple changes with a single UI refresh.

## Implementation Details

### Data Model
`PlayerData.cs` defines the observable properties:
- `ObservableField<int>`: Single values (Health, BaseDamage, WeaponBonus).
- `ObservableList<string>`: Collection (Inventory).
- `ComputedField<int>`: Derived value (TotalDamage = BaseDamage + WeaponBonus).

### View Components
MonoBehaviour components that bind to observables. See the `Scripts/` folder:
- `ReactiveHealthBar.cs`: Binds a Slider and Label to health.
- `ReactiveUGuiStatsPanel.cs`: Binds labels to damage stats.
- `ReactiveInventoryList.cs`: Binds a vertical layout to the inventory list.
