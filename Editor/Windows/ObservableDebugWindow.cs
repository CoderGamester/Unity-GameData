using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.GameData.Editor
{
	/// <summary>
	/// Editor window that displays all active observable instances tracked by <see cref="ObservableDebugRegistry"/>.
	/// Access via <c>Tools/GameLovers/Game Data/Observable Debugger</c>.
	/// </summary>
	/// <remarks>
	/// <para>Observables automatically register themselves when constructed in the editor via the self-registration pattern.</para>
	/// <para>Features filtering by name, type (Field/Computed/List/etc.), and subscriber activity.</para>
	/// <para>Selecting a Computed observable shows its dependency graph in the bottom panel.</para>
	/// </remarks>
	public sealed class ObservableDebugWindow : EditorWindow
	{
		private static readonly List<string> _kinds = new List<string>
		{
			"All",
			"Field",
			"Computed",
			"List",
			"Dictionary",
			"HashSet"
		};

		private ToolbarSearchField _filterField;
		private ToolbarMenu _kindMenu;
		private Toggle _activeOnlyToggle;
		private ToolbarButton _refreshButton;
		private MultiColumnListView _listView;
		private readonly List<ObservableDebugRegistry.EntrySnapshot> _rows = new List<ObservableDebugRegistry.EntrySnapshot>();
		private Label _headerLabel;
		private DependencyGraphElement _dependencyGraph;

		/// <summary>
		/// Opens the Observable Debugger window.
		/// </summary>
		[MenuItem("Tools/GameLovers/Game Data/Observable Debugger", priority = 200)]
		public static void ShowWindow()
		{
			var window = GetWindow<ObservableDebugWindow>("Observable Debugger");
			window.minSize = new Vector2(720, 420);
			window.Show();
		}

		/// <summary>
		/// Creates the UI Toolkit GUI when the window is opened or reloaded.
		/// </summary>
		public void CreateGUI()
		{
			rootVisualElement.Clear();
			rootVisualElement.style.flexGrow = 1;

			rootVisualElement.Add(BuildToolbar());

			var split = new TwoPaneSplitView(1, 180, TwoPaneSplitViewOrientation.Vertical);
			split.style.flexGrow = 1;
			split.Add(BuildList());

			_dependencyGraph = new DependencyGraphElement();
			split.Add(_dependencyGraph);

			rootVisualElement.Add(split);

			// Poll periodically while window is open (editor-only registry, no playmode requirement).
			rootVisualElement.schedule.Execute(RefreshData).Every(250);
			RefreshData();
		}

		private static void OpenSourceFile(ObservableDebugRegistry.EntrySnapshot snapshot)
		{
			var filePath = snapshot.FilePath;
			var lineNumber = snapshot.LineNumber;

			if (string.IsNullOrEmpty(filePath) || lineNumber <= 0)
			{
				return;
			}

			// Use Unity's internal utility to open the file at the specific line
			// This works like clicking on a console log entry
			InternalEditorUtility.OpenFileAtLineExternal(filePath, lineNumber);
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

		private VisualElement BuildToolbar()
		{
			var toolbar = new Toolbar();
			toolbar.style.flexWrap = Wrap.Wrap; // Allow wrapping on small screens
			toolbar.style.alignItems = Align.Center; // Vertically center all items

			_headerLabel = new Label("Observables");
			_headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			_headerLabel.style.marginRight = 8;
			_headerLabel.style.flexShrink = 0; // Don't shrink the count label

			_filterField = new ToolbarSearchField();
			_filterField.style.flexGrow = 1;
			_filterField.style.flexShrink = 1;
			_filterField.style.minWidth = 120;
			_filterField.RegisterValueChangedCallback(_ => RefreshData());

			// Use ToolbarMenu for compact dropdown (no label gap)
			_kindMenu = new ToolbarMenu { text = "All" };
			foreach (var kind in _kinds)
			{
				_kindMenu.menu.AppendAction(kind, a =>
				{
					_kindMenu.text = a.name;
					RefreshData();
				}, a => a.name == _kindMenu.text ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
			}

			_activeOnlyToggle = new Toggle("Active only");
			_activeOnlyToggle.labelElement.style.minWidth = 0; // Remove fixed label width
			_activeOnlyToggle.style.marginLeft = 4;
			_activeOnlyToggle.style.alignSelf = Align.Center; // Ensure toggle is centered
			_activeOnlyToggle.RegisterValueChangedCallback(_ => RefreshData());

			_refreshButton = new ToolbarButton(RefreshData)
			{
				text = "↻",
				tooltip = "Refresh"
			};

			toolbar.Add(_headerLabel);
			toolbar.Add(_filterField);
			toolbar.Add(_kindMenu);
			toolbar.Add(_activeOnlyToggle);
			toolbar.Add(_refreshButton);
			return toolbar;
		}

		private VisualElement BuildList()
		{
			_listView = new MultiColumnListView
			{
				columns =
				{
					new Column { name = "name", title = "Observable", width = 280, stretchable = true },
					new Column { name = "value", title = "Value", width = 120 },
					new Column { name = "subs", title = "Subs", width = 50 },
					new Column { name = "kind", title = "Type", width = 80 }
				},
				sortingMode = ColumnSortingMode.Default,
				selectionType = SelectionType.Single,
				showBorder = true,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				style = { flexGrow = 1 }
			};

			_listView.columns["name"].makeCell = () => new Label();
			_listView.columns["name"].bindCell = (e, i) =>
			{
				var s = _rows[i];
				var label = (Label)e;
				label.text = FormatName(s);
				ApplyRowStyle(e, s);
			};

			_listView.columns["value"].makeCell = () => new Label();
			_listView.columns["value"].bindCell = (e, i) =>
			{
				var s = _rows[i];
				((Label)e).text = s.Value;
				ApplyRowStyle(e, s);
			};

			_listView.columns["subs"].makeCell = () => new Label { style = { unityTextAlign = TextAnchor.MiddleRight } };
			_listView.columns["subs"].bindCell = (e, i) =>
			{
				var s = _rows[i];
				((Label)e).text = s.Subscribers.ToString();
				ApplyRowStyle(e, s);
			};

			_listView.columns["kind"].makeCell = () => new Label { style = { unityTextAlign = TextAnchor.MiddleRight } };
			_listView.columns["kind"].bindCell = (e, i) =>
			{
				var s = _rows[i];
				((Label)e).text = s.Kind;
				ApplyRowStyle(e, s);
			};

			_listView.itemsSource = _rows;
			_listView.selectionChanged += OnSelectionChanged;
			_listView.itemsChosen += OnItemsChosen; // Double-click to open source
			_listView.columnSortingChanged += () => RefreshData();

			return _listView;
		}

		private void OnItemsChosen(IEnumerable<object> items)
		{
			var first = items.FirstOrDefault();
			if (first is ObservableDebugRegistry.EntrySnapshot snapshot)
			{
				OpenSourceFile(snapshot);
			}
		}

		private void ApplyRowStyle(VisualElement element, ObservableDebugRegistry.EntrySnapshot snapshot)
		{
			// MultiColumnListView cells are wrapped in a row container.
			// We can style the parent row based on activity.
			var row = element.parent;
			if (row != null)
			{
				row.style.backgroundColor = snapshot.Subscribers > 0
					? new StyleColor(new Color(0f, 1f, 0f, 0.05f))
					: new StyleColor(StyleKeyword.Null);
			}
		}

		private void RefreshData()
		{
			_rows.Clear();

			var filter = _filterField?.value?.Trim();
			var hasFilter = !string.IsNullOrEmpty(filter);
			var filterLower = hasFilter ? filter.ToLowerInvariant() : string.Empty;

			var kind = _kindMenu?.text ?? "All";
			var activeOnly = _activeOnlyToggle != null && _activeOnlyToggle.value;

			foreach (var s in ObservableDebugRegistry.EnumerateSnapshots())
			{
				if (activeOnly && s.Subscribers <= 0)
				{
					continue;
				}

				if (kind != "All" && !string.Equals(s.Kind, kind, StringComparison.Ordinal))
				{
					continue;
				}

				if (hasFilter)
				{
					var observableName = s.Name ?? string.Empty;
					if (!observableName.ToLowerInvariant().Contains(filterLower))
					{
						continue;
					}
				}

				_rows.Add(s);
			}

			SortRows();

			_headerLabel.text = $"Observables: {_rows.Count}";
			_listView.RefreshItems();

			if (_rows.Count == 0)
			{
				_dependencyGraph.SetTarget(default);
			}
		}

		private void SortRows()
		{
			var sortDesc = _listView.sortedColumns.FirstOrDefault();
			if (sortDesc == null)
			{
				_rows.Sort((a, b) =>
				{
					var c = string.CompareOrdinal(a.Kind, b.Kind);
					return c != 0 ? c : string.CompareOrdinal(a.Name, b.Name);
				});
				return;
			}

			var dir = sortDesc.direction == SortDirection.Ascending ? 1 : -1;
			_rows.Sort((a, b) =>
			{
				int result;
				switch (sortDesc.columnName)
				{
					case "name": result = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase); break;
					case "value": result = string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase); break;
					case "subs": result = a.Subscribers.CompareTo(b.Subscribers); break;
					case "kind": result = string.Compare(a.Kind, b.Kind, StringComparison.OrdinalIgnoreCase); break;
					default: result = 0; break;
				}
				return result * dir;
			});
		}

		private void OnSelectionChanged(IEnumerable<object> selected)
		{
			var first = selected.FirstOrDefault();
			if (first is ObservableDebugRegistry.EntrySnapshot snapshot)
			{
				_dependencyGraph.SetTarget(snapshot);
			}
			else
			{
				_dependencyGraph.SetTarget(default);
			}
		}
	}
}
