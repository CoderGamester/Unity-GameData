using UnityEngine;

namespace GameLovers.GameData.Samples.DesignerWorkflow
{
	/// <summary>
	/// ScriptableObject wrapper around a <see cref="LootTable"/> so designers can edit drop rates in the Inspector.
	/// </summary>
	[CreateAssetMenu(fileName = "LootTable", menuName = "GameLovers GameData Samples/Designer Workflow/Loot Table")]
	public sealed class LootTableAsset : ScriptableObject
	{
		[SerializeField] private LootTable _dropRates = new LootTable();

		public LootTable DropRates => _dropRates;
	}
}

