using UnityEngine;

namespace GameLovers.GameData.Samples.DesignerWorkflow
{
	/// <summary>
	/// Designer-editable enemy configs stored as key/value pairs (Id -> EnemyConfig).
	/// </summary>
	[CreateAssetMenu(fileName = "EnemyConfigs", menuName = "GameLovers GameData Samples/Designer Workflow/Enemy Configs")]
	public sealed class EnemyConfigsAsset : ConfigsScriptableObject<int, EnemyConfig>
	{
	}
}

