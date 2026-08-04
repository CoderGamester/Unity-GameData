using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.GameData.Editor
{
	/// <summary>
	/// Minimal dependency view for computed observables.
	/// Currently displays the dependency list of <see cref="ComputedField{T}"/> via reflection.
	/// </summary>
	public sealed class DependencyGraphElement : VisualElement
	{
		private readonly Label _header;
		private readonly ScrollView _list;

		public DependencyGraphElement()
		{
			style.flexGrow = 1;
			style.paddingLeft = 8;
			style.paddingRight = 8;
			style.paddingTop = 6;
			style.paddingBottom = 6;
			style.borderTopWidth = 1;
			style.borderTopColor = new StyleColor(new Color(0f, 0f, 0f, 0.2f));

			_header = new Label("Dependency Graph");
			_header.style.unityFontStyleAndWeight = FontStyle.Bold;
			_header.style.marginBottom = 4;

			_list = new ScrollView(ScrollViewMode.Vertical);
			_list.style.flexGrow = 1;

			Add(_header);
			Add(_list);
		}

		/// <summary>
		/// Sets the target observable snapshot to display dependencies for.
		/// Only <see cref="ComputedField{T}"/> instances have dependencies; a default snapshot clears the view.
		/// </summary>
		public void SetTarget(ObservableDebugRegistry.EntrySnapshot snapshot)
		{
			_list.Clear();

			if (snapshot.Name == null || snapshot.Kind != "Computed")
			{
				_header.text = "Dependency Graph (select a Computed observable)";
				return;
			}

			if (snapshot.InstanceRef == null || !snapshot.InstanceRef.TryGetTarget(out var instance) || instance == null)
			{
				_header.text = "Dependency Graph (instance collected)";
				return;
			}

			_header.text = $"Dependency Graph: {FormatName(snapshot)}";

			var deps = TryReadDependencies(instance);
			if (deps == null)
			{
				_list.Add(new Label("No dependencies found (or not supported)."));
				return;
			}

			var count = 0;
			foreach (var d in deps)
			{
				count++;
				var depSnapshot = ObservableDebugRegistry.FindByInstance(d);
				if (depSnapshot.HasValue)
				{
					var s = depSnapshot.Value;
					var label = new Label($"- {FormatName(s)} = {s.Value}");
					_list.Add(label);
				}
				else
				{
					_list.Add(new Label($"- {d?.GetType().Name ?? "null"} (untracked)"));
				}
			}

			if (count == 0)
			{
				_list.Add(new Label("No dependencies registered yet."));
			}
		}

		private static string FormatName(ObservableDebugRegistry.EntrySnapshot s)
		{
			// Strip backtick-number patterns (e.g., `1, `2) from generic names
			var formattedName = Regex.Replace(s.Name ?? string.Empty, @"`\d+", "");

			// Append source location if available
			var sourceLocation = s.SourceLocation;
			if (!string.IsNullOrEmpty(sourceLocation))
			{
				return $"{formattedName} ({sourceLocation})";
			}

			// Fallback to ID if no source location
			return $"{formattedName}#{s.Id}";
		}

		private static IEnumerable TryReadDependencies(object computedFieldInstance)
		{
			// ComputedField<T> stores dependencies in a private field named "_dependencies".
			var type = computedFieldInstance.GetType();
			var field = type.GetField("_dependencies", BindingFlags.Instance | BindingFlags.NonPublic);
			return field?.GetValue(computedFieldInstance) as IEnumerable;
		}
	}
}

