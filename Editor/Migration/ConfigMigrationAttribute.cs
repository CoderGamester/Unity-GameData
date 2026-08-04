using System;

namespace GameLoversEditor.GameData
{
	/// <summary>
	/// Attribute to mark a class as a config migration handler for a specific config type.
	/// Migrations are discovered and run in the Editor when config schemas change.
	/// Version information is obtained from the <see cref="IConfigMigration"/> interface implementation.
	/// 
	/// <example>
	/// <code>
	/// [ConfigMigration(typeof(EnemyConfig))]
	/// public class EnemyConfigMigration_v1_v2 : IConfigMigration
	/// {
	///     public ulong FromVersion => 1;
	///     public ulong ToVersion => 2;
	///     
	///     public void Migrate(JObject configJson)
	///     {
	///         configJson["Armor"] = 10; // Add new field
	///     }
	/// }
	/// </code>
	/// </example>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class ConfigMigrationAttribute : Attribute
	{
		/// <summary>
		/// The config type this migration applies to.
		/// </summary>
		public Type ConfigType { get; }

		public ConfigMigrationAttribute(Type configType)
		{
			ConfigType = configType;
		}
	}
}
