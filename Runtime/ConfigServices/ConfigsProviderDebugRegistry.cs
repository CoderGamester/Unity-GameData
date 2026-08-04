using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GameLovers.GameData
{
	/// <summary>
	/// Static registry used by the Config Browser window to discover and inspect <see cref="IConfigsProvider"/> instances.
	/// Providers automatically register themselves via the self-registration pattern implemented in the constructor.
	/// </summary>
	/// <remarks>
	/// <para>Uses weak references so tracked providers do not prevent garbage collection.</para>
	/// <para>All members are compiled out in player builds via <c>#if UNITY_EDITOR</c>.</para>
	/// </remarks>
	public static class ConfigsProviderDebugRegistry
	{
#if UNITY_EDITOR
		/// <summary>Identity and creation site of a tracked configs provider. Editor introspection only — see AGENTS.md §4.</summary>
		internal readonly struct ProviderDebugInfo
		{
			public readonly int Id;
			public readonly string Name;
			public readonly DateTime CreatedAt;
			public readonly string FilePath;
			public readonly int LineNumber;

			public ProviderDebugInfo(int id, string name, DateTime createdAt, string filePath, int lineNumber)
			{
				Id = id;
				Name = name;
				CreatedAt = createdAt;
				FilePath = filePath;
				LineNumber = lineNumber;
			}

			/// <summary>File name of the creation site, or null when unknown.</summary>
			public string FileName => string.IsNullOrEmpty(FilePath) ? null : Path.GetFileName(FilePath);
			/// <summary>Creation site as <c>file:line</c>, or null when unknown.</summary>
			public string SourceLocation => string.IsNullOrEmpty(FilePath) ? null : $"{FileName}:{LineNumber}";
		}

		private static int _nextId = 1;
		private static readonly ConditionalWeakTable<IConfigsProvider, ProviderEntry> _entries = new ConditionalWeakTable<IConfigsProvider, ProviderEntry>();
		private static readonly List<WeakReference<IConfigsProvider>> _refs = new List<WeakReference<IConfigsProvider>>();

		/// <summary>
		/// Registers a provider instance with the debug registry.
		/// Called automatically by <see cref="ConfigsProvider"/> constructor in editor builds.
		/// </summary>
		internal static void Register(IConfigsProvider instance)
		{
			if (instance == null) return;

			if (_entries.TryGetValue(instance, out _))
			{
				return;
			}

			var (autoName, filePath, lineNumber) = GetAutoName();
			var info = new ProviderDebugInfo(_nextId++, autoName, DateTime.UtcNow, filePath, lineNumber);
			
			_entries.Add(instance, new ProviderEntry(info));
			_refs.Add(new WeakReference<IConfigsProvider>(instance));
		}

		/// <summary>
		/// Enumerates snapshots of all currently tracked provider instances.
		/// Automatically cleans up entries for garbage-collected instances.
		/// </summary>
		public static IEnumerable<ProviderSnapshot> EnumerateSnapshots()
		{
			for (int i = _refs.Count - 1; i >= 0; i--)
			{
				if (_refs[i].TryGetTarget(out var instance) && _entries.TryGetValue(instance, out var entry))
				{
					yield return entry.ToSnapshot(instance);
				}
				else
				{
					_refs.RemoveAt(i);
				}
			}
		}

		private static (string name, string filePath, int lineNumber) GetAutoName()
		{
			string filePath = null;
			int lineNumber = 0;
			string typeName = "Unknown";

			try
			{
				// Frame 0: GetAutoName
				// Frame 1: Register
				// Frame 2: ConfigsProvider constructor
				// Frame 3: Caller of the constructor
				var stack = new StackTrace(3, true);
				var frame = stack.GetFrame(0);
				var method = frame?.GetMethod();
				typeName = method?.DeclaringType?.Name ?? "Unknown";
				filePath = frame?.GetFileName();
				lineNumber = frame?.GetFileLineNumber() ?? 0;
			}
			catch
			{
				// Fallback
			}

			var name = string.IsNullOrEmpty(filePath) ? $"ConfigsProvider#{_nextId}" : $"{typeName}";
			return (name, filePath, lineNumber);
		}

		/// <summary>
		/// A point-in-time snapshot of a provider instance's debug information.
		/// </summary>
		public readonly struct ProviderSnapshot
		{
			private readonly ProviderDebugInfo _info;

			/// <summary>Registry id assigned when the provider was first seen.</summary>
			public int Id => _info.Id;
			/// <summary>Display name derived from the creation site.</summary>
			public string Name => _info.Name;
			/// <summary>When the provider was registered, in UTC.</summary>
			public DateTime CreatedAt => _info.CreatedAt;
			/// <summary>Source file the provider was created in, when known.</summary>
			public string FilePath => _info.FilePath;
			/// <summary>Line the provider was created on, when known.</summary>
			public int LineNumber => _info.LineNumber;
			/// <summary>File name of the creation site, or null when unknown.</summary>
			public string FileName => _info.FileName;
			/// <summary>Creation site as <c>file:line</c>, or null when unknown.</summary>
			public string SourceLocation => _info.SourceLocation;

			public readonly int ConfigTypeCount;
			public readonly WeakReference<IConfigsProvider> ProviderRef;

			internal ProviderSnapshot(ProviderDebugInfo info, int configTypeCount, IConfigsProvider instance)
			{
				_info = info;
				ConfigTypeCount = configTypeCount;
				ProviderRef = new WeakReference<IConfigsProvider>(instance);
			}
		}

		private sealed class ProviderEntry
		{
			private readonly ProviderDebugInfo _info;

			public ProviderEntry(ProviderDebugInfo info)
			{
				_info = info;
			}

			public ProviderSnapshot ToSnapshot(IConfigsProvider instance)
			{
				var count = 0;
				try { count = instance.GetAllConfigs().Count; } catch { }
				return new ProviderSnapshot(_info, count, instance);
			}
		}
#endif
	}
}
