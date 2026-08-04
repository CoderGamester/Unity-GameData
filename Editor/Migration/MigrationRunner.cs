using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GameLoversEditor.GameData
{
	/// <summary>
	/// Information about an available migration.
	/// </summary>
	public readonly struct MigrationInfo
	{
		public readonly Type ConfigType;
		public readonly ulong FromVersion;
		public readonly ulong ToVersion;
		public readonly Type MigrationType;

		public MigrationInfo(Type configType, ulong fromVersion, ulong toVersion, Type migrationType)
		{
			ConfigType = configType;
			FromVersion = fromVersion;
			ToVersion = toVersion;
			MigrationType = migrationType;
		}

		/// <inheritdoc />
		public override string ToString() => $"{ConfigType.Name}: v{FromVersion} → v{ToVersion}";
	}

	/// <summary>
	/// Result of a migration operation.
	/// </summary>
	public readonly struct MigrationResult
	{
		public readonly bool Success;
		public readonly string Message;
		public readonly int MigrationsApplied;

		public MigrationResult(bool success, string message, int migrationsApplied = 0)
		{
			Success = success;
			Message = message;
			MigrationsApplied = migrationsApplied;
		}

		/// <summary>A successful run that applied <paramref name="count"/> migrations.</summary>
		public static MigrationResult Ok(int count) => new MigrationResult(true, $"Applied {count} migration(s)", count);
		/// <summary>A successful run that found nothing to do.</summary>
		public static MigrationResult NoMigrations() => new MigrationResult(true, "No migrations needed", 0);
		/// <summary>A failed run carrying the reason.</summary>
		public static MigrationResult Error(string message) => new MigrationResult(false, message, 0);
	}

	/// <summary>
	/// Discovers and runs config migrations in the Editor.
	/// Migrations are used when config schemas change between versions, allowing
	/// existing config data to be transformed to match the new schema.
	/// 
	/// <example>
	/// Creating a migration:
	/// <code>
	/// [ConfigMigration(typeof(EnemyConfig), fromVersion: 1, toVersion: 2)]
	/// public class EnemyConfigMigration_v1_v2 : IConfigMigration
	/// {
	///     public ulong FromVersion => 1;
	///     public ulong ToVersion => 2;
	///     
	///     public void Migrate(JObject configJson)
	///     {
	///         // Add new field with default
	///         configJson["Armor"] = 10;
	///     }
	/// }
	/// </code>
	/// 
	/// Running migrations:
	/// <code>
	/// // Query available migrations
	/// var migrations = MigrationRunner.GetAvailableMigrations&lt;EnemyConfig&gt;();
	/// 
	/// // Migrate JSON data
	/// var json = JObject.Parse(jsonString);
	/// MigrationRunner.Migrate(typeof(EnemyConfig), json, currentVersion: 1, targetVersion: 2);
	/// 
	/// // Or migrate a ScriptableObject
	/// var result = MigrationRunner.MigrateScriptableObject(myConfigSO, fromVersion: 1, toVersion: 2);
	/// </code>
	/// </example>
	/// </summary>
	public static class MigrationRunner
	{
		private static readonly Dictionary<Type, List<(IConfigMigration Migration, Type MigrationType)>> _migrations = 
			new Dictionary<Type, List<(IConfigMigration, Type)>>();
		
		private static bool _initialized;

		/// <summary>
		/// Ensures migrations are discovered. Called automatically on first use.
		/// Can be called explicitly to force re-discovery (e.g., after domain reload).
		/// </summary>
		public static void Initialize(bool force = false)
		{
			if (_initialized && !force) return;
			
			_migrations.Clear();

			var types = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(s =>
				{
					try { return s.GetTypes(); }
					catch (ReflectionTypeLoadException) { return Array.Empty<Type>(); }
				})
				.Where(p => typeof(IConfigMigration).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

			foreach (var type in types)
			{
				var attrs = type.GetCustomAttributes<ConfigMigrationAttribute>();
				foreach (var attr in attrs)
				{
					if (!_migrations.TryGetValue(attr.ConfigType, out var list))
					{
						list = new List<(IConfigMigration, Type)>();
						_migrations.Add(attr.ConfigType, list);
					}
					
					var instance = (IConfigMigration)Activator.CreateInstance(type);
					list.Add((instance, type));
				}
			}

			_initialized = true;
		}

		/// <summary>
		/// Gets all config types that have registered migrations.
		/// </summary>
		public static IReadOnlyCollection<Type> GetConfigTypesWithMigrations()
		{
			Initialize();
			return _migrations.Keys;
		}

		/// <summary>
		/// Gets information about all available migrations for a config type.
		/// </summary>
		public static IReadOnlyList<MigrationInfo> GetAvailableMigrations<T>()
		{
			return GetAvailableMigrations(typeof(T));
		}

		/// <summary>
		/// Gets information about all available migrations for a config type.
		/// </summary>
		public static IReadOnlyList<MigrationInfo> GetAvailableMigrations(Type configType)
		{
			Initialize();
			
			if (!_migrations.TryGetValue(configType, out var list))
			{
				return Array.Empty<MigrationInfo>();
			}

			return list
				.Select(m => new MigrationInfo(configType, m.Migration.FromVersion, m.Migration.ToVersion, m.MigrationType))
				.OrderBy(m => m.FromVersion)
				.ToList();
		}

		/// <summary>
		/// Gets the latest target version available for a config type.
		/// </summary>
		public static ulong GetLatestVersion(Type configType)
		{
			Initialize();
			
			if (!_migrations.TryGetValue(configType, out var list) || list.Count == 0)
			{
				return 0;
			}

			return list.Max(m => m.Migration.ToVersion);
		}

		/// <summary>
		/// Migrates a config JSON object from one version to another.
		/// Applies all applicable migrations in order.
		/// </summary>
		public static int Migrate(Type configType, JObject configJson, ulong currentVersion, ulong targetVersion)
		{
			Initialize();
			
			if (!_migrations.TryGetValue(configType, out var migrations))
			{
				return 0;
			}

			var applicableMigrations = migrations
				.Where(m => m.Migration.FromVersion >= currentVersion && m.Migration.ToVersion <= targetVersion)
				.OrderBy(m => m.Migration.FromVersion)
				.ToList();

			foreach (var (migration, _) in applicableMigrations)
			{
				migration.Migrate(configJson);
			}

			return applicableMigrations.Count;
		}

		/// <summary>
		/// Migrates a ScriptableObject's serialized data.
		/// Converts to JSON, applies migrations, and updates the object.
		/// </summary>
		public static MigrationResult MigrateScriptableObject<T>(
			T scriptableObject, 
			Type configType, 
			ulong fromVersion, 
			ulong toVersion = 0) where T : ScriptableObject
		{
			Initialize();

			if (toVersion == 0)
			{
				toVersion = GetLatestVersion(configType);
			}

			if (fromVersion >= toVersion)
			{
				return MigrationResult.NoMigrations();
			}

			try
			{
				// Serialize to JSON
				var json = JsonConvert.SerializeObject(scriptableObject);
				var jObject = JObject.Parse(json);

				// Apply migrations
				var count = Migrate(configType, jObject, fromVersion, toVersion);

				if (count == 0)
				{
					return MigrationResult.NoMigrations();
				}

				// Deserialize back
				JsonConvert.PopulateObject(jObject.ToString(), scriptableObject);

				return MigrationResult.Ok(count);
			}
			catch (Exception ex)
			{
				return MigrationResult.Error($"Migration failed: {ex.Message}");
			}
		}
	}
}
