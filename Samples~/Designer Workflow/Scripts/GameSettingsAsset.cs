using UnityEngine;

namespace GameLovers.GameData.Samples.DesignerWorkflow
{
	/// <summary>
	/// Designer-editable game settings stored as a single entry (Id 0 -> GameSettingsConfig).
	/// This uses <see cref="ConfigsScriptableObject{TId,TAsset}"/> to demonstrate the workflow.
	/// </summary>
	[CreateAssetMenu(fileName = "GameSettings", menuName = "GameLovers GameData Samples/Designer Workflow/Game Settings")]
	public sealed class GameSettingsAsset : ConfigsScriptableObject<int, GameSettingsConfig>
	{
		public const int SingletonKey = 0;
	}
}

