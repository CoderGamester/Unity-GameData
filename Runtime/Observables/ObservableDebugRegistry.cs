using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace GameLovers.GameData
{
	// ═══════════════════════════════════════════════════════════════════════════
	// EDITOR-ONLY: Observable Debug Window Support
	// ═══════════════════════════════════════════════════════════════════════════
	// This file implements an internal registry used by the Observable Debugger
	// window to discover and inspect observable instances without requiring any
	// manual registration calls in user code.
	//
	// Design notes:
	// - Uses weak references so tracked observables do not leak memory.
	// - Stores value/subscriber getters as delegates so the debug window can poll
	//   live data without reflection.
	// - Compiled out in player builds.
	// ═══════════════════════════════════════════════════════════════════════════

	/// <summary>
	/// Static registry used by the Observable Debugger window to discover and inspect observable instances.
	/// Observables automatically register themselves via the self-registration pattern implemented in each
	/// observable class's editor-only code block.
	/// </summary>
	/// <remarks>
	/// <para>Uses weak references so tracked observables do not prevent garbage collection.</para>
	/// <para>All members are compiled out in player builds via <c>#if UNITY_EDITOR</c>.</para>
	/// </remarks>
	public static class ObservableDebugRegistry
	{
#if UNITY_EDITOR
		/// <summary>Identity and creation site of a tracked observable. Editor introspection only — see AGENTS.md §4.</summary>
		internal readonly struct ObservableDebugInfo
		{
			public readonly int Id;
			public readonly string Name;
			public readonly string Kind;
			public readonly DateTime CreatedAt;
			public readonly string FilePath;
			public readonly int LineNumber;

			public ObservableDebugInfo(int id, string name, string kind, DateTime createdAt, string filePath, int lineNumber)
			{
				Id = id;
				Name = name;
				Kind = kind;
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
		private static readonly ConditionalWeakTable<object, Entry> _entries = new ConditionalWeakTable<object, Entry>();
		private static readonly List<WeakReference<object>> _refs = new List<WeakReference<object>>();

		/// <summary>
		/// Registers an observable instance with the debug registry, where <paramref name="kind"/> is one of
		/// Field, Computed, List, Dictionary or HashSet. Called automatically by observable constructors in
		/// editor builds.
		/// </summary>
		internal static void Register(
			object instance,
			string kind,
			Func<string> valueGetter,
			Func<int> subscriberCountGetter)
		{
			if (instance == null) return;

			if (_entries.TryGetValue(instance, out _))
			{
				return;
			}

			var (name, filePath, lineNumber) = GetAutoName(kind, instance.GetType());
			var info = new ObservableDebugInfo(_nextId++, name, kind, DateTime.UtcNow, filePath, lineNumber);
			_entries.Add(instance, new Entry(info, valueGetter, subscriberCountGetter));
			_refs.Add(new WeakReference<object>(instance));
		}

		/// <summary>
		/// Enumerates snapshots of all currently tracked observable instances.
		/// Automatically cleans up entries for garbage-collected instances.
		/// </summary>
		public static IEnumerable<EntrySnapshot> EnumerateSnapshots()
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

		/// <summary>
		/// Finds a snapshot for a specific observable instance if it is currently tracked.
		/// </summary>
		public static EntrySnapshot? FindByInstance(object instance)
		{
			if (instance == null) return null;
			if (_entries.TryGetValue(instance, out var entry))
			{
				return entry.ToSnapshot(instance);
			}
			return null;
		}

		private static (string name, string filePath, int lineNumber) GetAutoName(string kind, Type instanceType)
		{
			string filePath = null;
			int lineNumber = 0;
			string typeName = "Unknown";
			string memberName = kind;

			try
			{
				// Enable file info to capture source location
				var stack = new StackTrace(4, true);
				var frame = stack.GetFrame(0);
				var method = frame?.GetMethod();
				typeName = method?.DeclaringType?.Name ?? "Unknown";
				filePath = frame?.GetFileName();
				lineNumber = frame?.GetFileLineNumber() ?? 0;

				// Try to extract field/property name from source line
				if (!string.IsNullOrEmpty(filePath) && lineNumber > 0)
				{
					var extractedName = TryExtractMemberName(filePath, lineNumber);
					if (!string.IsNullOrEmpty(extractedName))
					{
						memberName = extractedName;
					}
				}
			}
			catch
			{
				// Fallback to basic name on any error
			}

			var name = $"{typeName}.{memberName}<{instanceType.Name}>";
			return (name, filePath, lineNumber);
		}

		// Reads the declaring line out of the source file, so the debugger can label an observable by the
		// member that holds it rather than by its type alone.
		private static string TryExtractMemberName(string filePath, int lineNumber)
		{
			try
			{
				if (!File.Exists(filePath)) return null;

				var lines = File.ReadAllLines(filePath);
				if (lineNumber <= 0 || lineNumber > lines.Length) return null;

				var line = lines[lineNumber - 1];

				// Pattern for property: "public ObservableField<int> Health { get; }"
				// Pattern for field: "private ObservableField<int> _health ="
				// Pattern for assignment: "Health = new ObservableField<int>()"

				// Try to match property declaration pattern
				var propertyMatch = Regex.Match(line, @"\b(\w+)\s*\{\s*get\s*;");
				if (propertyMatch.Success)
				{
					return propertyMatch.Groups[1].Value;
				}

				// Try to match field assignment pattern (name before = new)
				var assignmentMatch = Regex.Match(line, @"\b(\w+)\s*=\s*new\s+(?:Observable|Computed)");
				if (assignmentMatch.Success)
				{
					return assignmentMatch.Groups[1].Value;
				}

				// Try to match field declaration pattern (type followed by name)
				var fieldMatch = Regex.Match(line, @"(?:Observable|Computed)\w*<[^>]+>\s+(\w+)\s*[;=]");
				if (fieldMatch.Success)
				{
					return fieldMatch.Groups[1].Value;
				}
			}
			catch
			{
				// Silently fail - this is just for better naming, not critical
			}

			return null;
		}

		/// <summary>
		/// A point-in-time snapshot of an observable instance's debug information.
		/// </summary>
		public readonly struct EntrySnapshot
		{
			private readonly ObservableDebugInfo _info;

			/// <summary>Registry id assigned when the observable was first seen.</summary>
			public int Id => _info.Id;
			/// <summary>Display name derived from the declaring member.</summary>
			public string Name => _info.Name;
			/// <summary>Observable category: Field, Computed, List, Dictionary or HashSet.</summary>
			public string Kind => _info.Kind;
			/// <summary>When the observable was registered, in UTC.</summary>
			public DateTime CreatedAt => _info.CreatedAt;
			/// <summary>Source file the observable was created in, when known.</summary>
			public string FilePath => _info.FilePath;
			/// <summary>Line the observable was created on, when known.</summary>
			public int LineNumber => _info.LineNumber;
			/// <summary>File name of the creation site, or null when unknown.</summary>
			public string FileName => _info.FileName;
			/// <summary>Creation site as <c>file:line</c>, or null when unknown.</summary>
			public string SourceLocation => _info.SourceLocation;

			public readonly string Value;
			public readonly int Subscribers;
			public readonly Type RuntimeType;
			public readonly WeakReference<object> InstanceRef;

			internal EntrySnapshot(ObservableDebugInfo info, string value, int subscribers, Type runtimeType, WeakReference<object> instanceRef)
			{
				_info = info;
				Value = value;
				Subscribers = subscribers;
				RuntimeType = runtimeType;
				InstanceRef = instanceRef;
			}
		}

		private sealed class Entry
		{
			private readonly ObservableDebugInfo _info;
			private readonly Func<string> _valueGetter;
			private readonly Func<int> _subscriberCountGetter;

			public Entry(ObservableDebugInfo info, Func<string> valueGetter, Func<int> subscriberCountGetter)
			{
				_info = info;
				_valueGetter = valueGetter;
				_subscriberCountGetter = subscriberCountGetter;
			}

			public EntrySnapshot ToSnapshot(object instance)
			{
				string value;
				int subs;

				try { value = _valueGetter?.Invoke() ?? string.Empty; }
				catch (Exception ex) { value = $"<error: {ex.Message}>"; }

				try { subs = _subscriberCountGetter?.Invoke() ?? 0; }
				catch { subs = 0; }

				return new EntrySnapshot(_info, value, subs, instance.GetType(), new WeakReference<object>(instance));
			}
		}
#endif
	}
}

