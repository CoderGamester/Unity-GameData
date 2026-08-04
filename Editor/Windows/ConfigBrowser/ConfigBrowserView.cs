using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.GameData.Editor
{
	/// <summary>
	/// Constructs and manages the entire Config Browser UI layout using UI Toolkit.
	/// Exposes events for user interactions (search, validate, export, tab switching, tree selection)
	/// that <see cref="ConfigBrowserWindow"/> subscribes to. This class is purely presentational.
	/// </summary>
	internal sealed class ConfigBrowserView
	{
		private Toolbar _toolbar;
		private ToolbarMenu _providerMenu;
		private ToolbarToggle _browseTab;
		private ToolbarToggle _migrationsTab;

		private Toolbar _browseActionBar;
		private ToolbarSearchField _searchField;
		private Button _validateAllButton;
		private Button _exportAllButton;

		private VisualElement _browseRoot;
		private VisualElement _migrationsRoot;
		private MigrationPanelElement _migrationPanel;

		private TreeView _treeView;
		private JsonViewerElement _jsonViewer;
		private Label _detailsHeader;
		private Button _validateSelectedButton;
		private Button _exportSelectedButton;

		private VisualElement _validationPanel;
		private Label _validationHeaderLabel;
		private Button _clearValidationFilterButton;
		private ScrollView _validationList;

		/// <summary>Raised as the user edits the search box.</summary>
		public event Action<string> SearchChanged;
		/// <summary>Raised by the validate-all button.</summary>
		public event Action ValidateAllRequested;
		/// <summary>Raised by the export-all button.</summary>
		public event Action ExportAllRequested;
		/// <summary>Raised by the validate-selection button.</summary>
		public event Action ValidateSelectedRequested;
		/// <summary>Raised by the export-selection button.</summary>
		public event Action ExportSelectedRequested;
		/// <summary>Raised when the user clears the validation filter.</summary>
		public event Action ClearValidationFilterRequested;
		/// <summary>Raised with the newly selected config nodes.</summary>
		public event Action<IEnumerable<object>> TreeSelectionChanged;
		/// <summary>Raised with the config name and optional row index of a clicked validation error.</summary>
		public event Action<string, int?> ValidationRowClicked;
		/// <summary>Raised when the user switches tab.</summary>
		public event Action<bool> TabChanged;

		public ConfigBrowserView(VisualElement root)
		{
			Build(root);
		}

		/// <summary>Returns true after <see cref="Build"/> has completed.</summary>
		public bool IsInitialized => _detailsHeader != null;

		/// <summary>The top-level toolbar element.</summary>
		public Toolbar Toolbar => _toolbar;
		/// <summary>The provider selection dropdown menu in the toolbar.</summary>
		public ToolbarMenu ProviderMenu => _providerMenu;
		/// <summary>Root container for the Browse tab content.</summary>
		public VisualElement BrowseRoot => _browseRoot;
		/// <summary>Root container for the Migrations tab content.</summary>
		public VisualElement MigrationsRoot => _migrationsRoot;
		/// <summary>The embedded <see cref="MigrationPanelElement"/> inside the Migrations tab.</summary>
		public MigrationPanelElement MigrationPanel => _migrationPanel;
		/// <summary>Current text in the search field.</summary>
		public string SearchText => _searchField?.value;

		/// <summary>
		/// Replaces the current provider dropdown menu in the toolbar with <paramref name="newMenu"/>.
		/// </summary>
		public void ReplaceProviderMenu(ToolbarMenu newMenu)
		{
			if (_toolbar == null || _providerMenu == null || newMenu == null) return;
			_toolbar.Insert(_toolbar.IndexOf(_providerMenu), newMenu);
			_toolbar.Remove(_providerMenu);
			_providerMenu = newMenu;
		}

		/// <summary>
		/// Switches the visible tab. When <paramref name="isBrowse"/> is true the Browse tab is shown;
		/// otherwise the Migrations tab is shown.
		/// </summary>
		public void SetActiveTab(bool isBrowse)
		{
			_browseRoot.style.display = isBrowse ? DisplayStyle.Flex : DisplayStyle.None;
			_migrationsRoot.style.display = isBrowse ? DisplayStyle.None : DisplayStyle.Flex;
			_browseTab.SetValueWithoutNotify(isBrowse);
			_migrationsTab.SetValueWithoutNotify(!isBrowse);
		}

		/// <summary>
		/// Replaces the tree view root items and rebuilds the visual tree.
		/// </summary>
		public void SetTreeItems(IList<TreeViewItemData<ConfigNode>> items)
		{
			_treeView.SetRootItems(items);
			_treeView.Rebuild();
		}

		/// <summary>
		/// Programmatically selects the tree item with the given <paramref name="itemId"/> and scrolls it into view.
		/// </summary>
		public void SelectTreeItem(int itemId)
		{
			_treeView.SetSelection(new List<int> { itemId });
			_treeView.ScrollToItem(itemId);
		}

		/// <summary>
		/// Updates the details panel to display the given <paramref name="selection"/>.
		/// Shows the config <paramref name="displayName"/> in the header and the serialized <paramref name="json"/> in the viewer.
		/// </summary>
		public void SetSelection(ConfigSelection selection, string displayName, string json)
		{
			if (!selection.IsValid)
			{
				_detailsHeader.text = "No selection";
				_validateSelectedButton.SetEnabled(false);
				_exportSelectedButton.SetEnabled(false);
				_jsonViewer.SetJson(string.Empty);
				return;
			}

			_detailsHeader.text = displayName;
			_validateSelectedButton.SetEnabled(true);
			_exportSelectedButton.SetEnabled(true);
			_jsonViewer.SetJson(json);
		}

		/// <summary>
		/// Populates the validation panel with the given <paramref name="errors"/> list,
		/// updating the header to reflect the active <paramref name="filter"/> scope.
		/// When <paramref name="providerExists"/> is false, shows an informational message.
		/// </summary>
		public void SetValidationResults(List<ValidationErrorInfo> errors, ValidationFilter filter, bool providerExists)
		{
			_validationList.Clear();

			var showing = filter.IsAll ? "All" : $"{filter.ConfigType?.Name} (ID:{filter.ConfigId})";
			_validationHeaderLabel.text = $"Validation Results  Showing: {showing}  Errors: {errors.Count}";
			_clearValidationFilterButton.SetEnabled(!filter.IsAll);

			if (!providerExists)
			{
				_validationList.Add(new HelpBox("No provider selected.\nEnter Play Mode and create a ConfigsProvider.", HelpBoxMessageType.Info));
				return;
			}

			if (errors.Count == 0)
			{
				_validationList.Add(new HelpBox("No validation errors.", HelpBoxMessageType.Info));
				return;
			}

			for (int i = 0; i < errors.Count; i++)
			{
				var e = errors[i];
				var row = new ValidationErrorElement();
				row.Bind(e.ConfigTypeName, e.ConfigId, e.FieldName, e.Message);
				row.Clicked += (typeName, configId) => ValidationRowClicked?.Invoke(typeName, configId);
				_validationList.Add(row);
			}
		}

		/// <summary>
		/// Removes all elements from the validation result list.
		/// </summary>
		public void ClearValidationList()
		{
			_validationList.Clear();
		}

		private void Build(VisualElement root)
		{
			root.Clear();
			root.style.flexGrow = 1;

			// Global toolbar: Provider + Tabs
			root.Add(BuildToolbar());

			// Tab content areas
			_browseRoot = BuildBrowseRoot();
			_migrationsRoot = BuildMigrationsRoot();

			var content = new VisualElement { style = { flexGrow = 1 } };
			content.Add(_browseRoot);
			content.Add(_migrationsRoot);
			root.Add(content);
		}

		private VisualElement BuildToolbar()
		{
			_toolbar = new Toolbar();

			_providerMenu = new ToolbarMenu { text = "No providers" };
			_providerMenu.style.minWidth = 280;

			// Tabs integrated into the global toolbar
			_browseTab = new ToolbarToggle { text = "Browse", value = true };
			_migrationsTab = new ToolbarToggle { text = "Migrations", value = false };

			_browseTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					_migrationsTab.SetValueWithoutNotify(false);
					TabChanged?.Invoke(true);
				}
			});

			_migrationsTab.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					_browseTab.SetValueWithoutNotify(false);
					TabChanged?.Invoke(false);
				}
			});

			// Spacer to push tabs to the left after provider menu
			var spacer = new VisualElement { style = { flexGrow = 1 } };

			_toolbar.Add(_providerMenu);
			_toolbar.Add(_browseTab);
			_toolbar.Add(_migrationsTab);
			_toolbar.Add(spacer);

			return _toolbar;
		}

		private VisualElement BuildBrowseActionBar()
		{
			_browseActionBar = new Toolbar();

			_searchField = new ToolbarSearchField();
			_searchField.style.flexGrow = 1;
			_searchField.RegisterValueChangedCallback(evt => SearchChanged?.Invoke(evt.newValue));

			_validateAllButton = new Button(() => ValidateAllRequested?.Invoke())
			{
				text = "Validate All"
			};

			_exportAllButton = new Button(() => ExportAllRequested?.Invoke())
			{
				text = "Export All"
			};

			_browseActionBar.Add(_searchField);
			_browseActionBar.Add(_validateAllButton);
			_browseActionBar.Add(_exportAllButton);

			return _browseActionBar;
		}

		private VisualElement BuildBrowseRoot()
		{
			var root = new VisualElement { style = { flexGrow = 1 } };

			// Browse action bar: Search + Validate All + Export All
			root.Add(BuildBrowseActionBar());

			// Horizontal split: tree view (left) + details panel (right)
			var horizontalSplit = new TwoPaneSplitView(0, 260, TwoPaneSplitViewOrientation.Horizontal);
			horizontalSplit.viewDataKey = "ConfigBrowser_HorizontalSplit_v2"; // Changed key to reset persisted state
			horizontalSplit.style.flexGrow = 1;
			horizontalSplit.style.minHeight = 200;

			_treeView = new TreeView
			{
				selectionType = SelectionType.Single,
				showBorder = true,
				style = { flexGrow = 1, minWidth = 200, minHeight = 100 },
				makeItem = () => new Label { style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 4 } },
				bindItem = (element, index) =>
				{
					var node = _treeView.GetItemDataForIndex<ConfigNode>(index);
					var label = (Label)element;
					label.text = node.DisplayName;

					// Style based on node kind
					label.style.unityFontStyleAndWeight = node.Kind == ConfigNodeKind.Header
						? FontStyle.Bold
						: FontStyle.Normal;
				}
			};
			_treeView.selectionChanged += selection => TreeSelectionChanged?.Invoke(selection);

			// Wrap TreeView in a container with minimum dimensions to prevent collapse
			var treeContainer = new VisualElement { style = { flexGrow = 1, minWidth = 200 } };
			treeContainer.Add(_treeView);
			horizontalSplit.Add(treeContainer);
			horizontalSplit.Add(BuildDetailsPanel());

			// Vertical split: content (top) + validation panel (bottom)
			var verticalSplit = new TwoPaneSplitView(1, 180, TwoPaneSplitViewOrientation.Vertical);
			verticalSplit.viewDataKey = "ConfigBrowser_BrowseVerticalSplit_v2"; // Changed key to reset persisted state
			verticalSplit.style.flexGrow = 1;

			verticalSplit.Add(horizontalSplit);
			verticalSplit.Add(BuildValidationPanel());

			root.Add(verticalSplit);
			return root;
		}

		private VisualElement BuildMigrationsRoot()
		{
			var root = new VisualElement { style = { flexGrow = 1 } };
			_migrationPanel = new MigrationPanelElement();
			root.Add(_migrationPanel);
			return root;
		}

		private VisualElement BuildDetailsPanel()
		{
			var panel = new VisualElement
			{
				style =
				{
					flexGrow = 1,
					paddingLeft = 8,
					paddingRight = 8,
					paddingTop = 6,
					paddingBottom = 6
				}
			};

			var header = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					justifyContent = Justify.SpaceBetween,
					marginBottom = 6
				}
			};

			_detailsHeader = new Label("No selection");
			_detailsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;

			// Button container for per-config actions
			var buttonContainer = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center
				}
			};

			_validateSelectedButton = new Button(() => ValidateSelectedRequested?.Invoke())
			{
				text = "Validate"
			};
			_validateSelectedButton.SetEnabled(false);

			_exportSelectedButton = new Button(() => ExportSelectedRequested?.Invoke())
			{
				text = "Export"
			};
			_exportSelectedButton.SetEnabled(false);

			buttonContainer.Add(_validateSelectedButton);
			buttonContainer.Add(_exportSelectedButton);

			header.Add(_detailsHeader);
			header.Add(buttonContainer);

			_jsonViewer = new JsonViewerElement();

			panel.Add(header);
			panel.Add(_jsonViewer);

			return panel;
		}

		private VisualElement BuildValidationPanel()
		{
			_validationPanel = new VisualElement
			{
				style =
				{
					flexGrow = 1,
					minHeight = 100,
					borderTopWidth = 1,
					borderTopColor = new StyleColor(new Color(0f, 0f, 0f, 0.2f))
				}
			};

			var header = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					justifyContent = Justify.SpaceBetween,
					paddingLeft = 8,
					paddingRight = 8,
					paddingTop = 4,
					paddingBottom = 4
				}
			};

			_validationHeaderLabel = new Label("Validation Results");
			_validationHeaderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

			_clearValidationFilterButton = new Button(() => ClearValidationFilterRequested?.Invoke())
			{
				text = "Clear"
			};

			header.Add(_validationHeaderLabel);
			header.Add(_clearValidationFilterButton);

			_validationList = new ScrollView(ScrollViewMode.Vertical);
			_validationList.style.flexGrow = 1;

			_validationPanel.Add(header);
			_validationPanel.Add(_validationList);

			return _validationPanel;
		}
	}
}
