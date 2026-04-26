using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameLovers.GameData.Editor
{
	/// <summary>
	/// Editor window that provides a unified interface for browsing, validating, and migrating config data.
	/// Access via <c>Tools/GameLovers/Game Data/Config Browser</c>.
	/// </summary>
	/// <remarks>
	/// <para>The Browse tab displays a tree view of all configs in the assigned provider with JSON preview and validation.</para>
	/// <para>The Migrations tab (visible only when migrations exist) shows migration status and provides in-memory preview.</para>
	/// </remarks>
	public sealed class ConfigBrowserWindow : EditorWindow
	{
		private const int SingleConfigId = 0;

		private ConfigBrowserView _view;
		private ProviderMenuController _providerMenuController;

		// Change detection: track provider data fingerprint to auto-refresh when data changes.
		private int _lastConfigTypeCount = -1;
		private int _lastTotalConfigCount = -1;

		private ValidationFilter _validationFilter = ValidationFilter.All();
		private ConfigSelection _selection;

		/// <summary>
		/// Opens the Config Browser window.
		/// </summary>
		[MenuItem("Tools/GameLovers/Game Data/Config Browser", priority = 100)]
		public static void ShowWindow()
		{
			var window = GetWindow<ConfigBrowserWindow>("Config Browser");
			window.minSize = new Vector2(720, 420);
			window.Show();
		}

		private void OnEnable()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		/// <summary>
		/// Creates the UI Toolkit GUI when the window is opened or reloaded.
		/// </summary>
		public void CreateGUI()
		{
			_view = new ConfigBrowserView(rootVisualElement);
			_providerMenuController = new ProviderMenuController(_view);
			_providerMenuController.ProviderChanged += OnProviderChanged;

			_view.SearchChanged += _ => RefreshTree();
			_view.ValidateAllRequested += OnValidateAllRequested;
			_view.ExportAllRequested += ExportAllJson;
			_view.ValidateSelectedRequested += OnValidateSelectedRequested;
			_view.ExportSelectedRequested += ExportSelectedJson;
			_view.ClearValidationFilterRequested += OnClearValidationFilterRequested;
			_view.TreeSelectionChanged += OnTreeSelectionChanged;
			_view.ValidationRowClicked += OnValidationRowClicked;
			_view.TabChanged += SetActiveTab;

			rootVisualElement.schedule.Execute(RefreshProviderList).Every(250);
			RefreshProviderList();
			RefreshAll();
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			var isGuiInitialized = _view != null && _view.IsInitialized;

			// Clear selection when entering or exiting play mode to avoid stale references
			if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
			{
				_providerMenuController?.ClearSelection();
				_lastConfigTypeCount = -1;
				_lastTotalConfigCount = -1;

				if (isGuiInitialized)
				{
					RefreshAll();
				}
			}
			else if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
			{
				if (isGuiInitialized)
				{
					// Refresh to pick up newly registered providers or clear stale state
					RefreshProviderList();
					RefreshAll();
				}
			}
		}

		private void OnProviderChanged()
		{
			_lastConfigTypeCount = -1;
			_lastTotalConfigCount = -1;
			if (_view != null && _view.IsInitialized)
			{
				RefreshAll();
			}
		}

		private void RefreshProviderList()
		{
			_providerMenuController.RefreshSnapshots();

			// Detect data changes within the current provider and refresh tree if needed.
			var provider = _providerMenuController.Provider;
			if (provider != null)
			{
				var (typeCount, totalCount) = ConfigsEditorUtil.ComputeConfigCounts(provider);
				if (typeCount != _lastConfigTypeCount || totalCount != _lastTotalConfigCount)
				{
					_lastConfigTypeCount = typeCount;
					_lastTotalConfigCount = totalCount;
					RefreshTree();
				}
			}
		}

		private void SetActiveTab(bool isBrowse)
		{
			_view.SetActiveTab(isBrowse);

			// Keep migration panel updated when switching.
			if (!isBrowse)
			{
				_view.MigrationPanel.SetProvider(_providerMenuController.Provider);
			}
		}

		private void RefreshAll()
		{
			_selection = ConfigSelection.None();
			_view.SetSelection(_selection, "No selection", string.Empty);

			RefreshTree();
			_view.SetValidationResults(new List<ValidationErrorInfo>(), _validationFilter, _providerMenuController.Provider != null);
			RefreshMigrationsVisibility();
			SetActiveTab(isBrowse: true);
		}

		private void RefreshMigrationsVisibility()
		{
			// Migrations tab is always visible; empty state is handled by MigrationPanelElement.
			_view.MigrationPanel.SetProvider(_providerMenuController.Provider);
		}

		private void RefreshTree()
		{
			var items = ConfigTreeBuilder.BuildTreeItems(_providerMenuController.Provider, _view.SearchText);
			_view.SetTreeItems(items);
		}

		private void OnTreeSelectionChanged(IEnumerable<object> selected)
		{
			var first = selected.FirstOrDefault();
			if (first is not ConfigNode node || node.Kind != ConfigNodeKind.Entry)
			{
				_selection = ConfigSelection.None();
				_view.SetSelection(_selection, "No selection", string.Empty);
				return;
			}

			_selection = new ConfigSelection(node.ConfigType, node.ConfigId, node.Value);
			_view.SetSelection(_selection, node.DisplayName, ConfigExportService.ToJson(node.Value));
		}

		private void OnValidateAllRequested()
		{
			_validationFilter = ValidationFilter.All();
			var errors = ConfigValidationService.ValidateAll(_providerMenuController.Provider);
			_view.SetValidationResults(errors, _validationFilter, _providerMenuController.Provider != null);
		}

		private void OnValidateSelectedRequested()
		{
			if (!_selection.IsValid || _providerMenuController.Provider == null) return;
			_validationFilter = ValidationFilter.Single(_selection.ConfigType, _selection.ConfigId);
			var errors = ConfigValidationService.ValidateSingle(_selection);
			_view.SetValidationResults(errors, _validationFilter, _providerMenuController.Provider != null);
		}

		private void OnClearValidationFilterRequested()
		{
			_validationFilter = ValidationFilter.All();
			var errors = ConfigValidationService.ValidateAll(_providerMenuController.Provider);
			_view.SetValidationResults(errors, _validationFilter, _providerMenuController.Provider != null);
		}

		private void OnValidationRowClicked(string configTypeName, int? configId)
		{
			// Best-effort: select matching node in tree by scanning visible items.
			var provider = _providerMenuController.Provider;
			if (provider == null) return;
			var targetType = provider.GetAllConfigs().Keys.FirstOrDefault(t => t.Name == configTypeName);
			if (targetType == null) return;

			// Rebuild tree item data so we can search it deterministically.
			var roots = ConfigTreeBuilder.BuildTreeItems(provider, _view.SearchText);
			_view.SetTreeItems(roots);

			var itemId = ConfigTreeBuilder.FindTreeItemIdForEntry(roots, targetType, configId ?? SingleConfigId);
			if (itemId.HasValue)
			{
				_view.SelectTreeItem(itemId.Value);
			}
		}

		private void ExportAllJson()
		{
			var provider = _providerMenuController.Provider;
			if (provider == null)
			{
				EditorUtility.DisplayDialog("Export JSON", "No provider selected.\nEnter Play Mode and create a ConfigsProvider.", "OK");
				return;
			}

			var json = ConfigExportService.ExportProviderToJson(provider);
			var path = EditorUtility.SaveFilePanel("Export All Configs JSON", Application.dataPath, "configs.json", "json");
			if (string.IsNullOrWhiteSpace(path))
			{
				return;
			}

			System.IO.File.WriteAllText(path, json);
			EditorUtility.RevealInFinder(path);
		}

		private void ExportSelectedJson()
		{
			if (!_selection.IsValid)
			{
				EditorUtility.DisplayDialog("Export JSON", "No config selected.", "OK");
				return;
			}

			var typeName = _selection.ConfigType.Name;
			var configId = _selection.ConfigId;
			var isSingleton = configId == SingleConfigId;
			var fileName = isSingleton ? $"{typeName}.json" : $"{typeName}_{configId}.json";

			var json = ConfigExportService.ToJson(_selection.Value);
			var path = EditorUtility.SaveFilePanel("Export Config JSON", Application.dataPath, fileName, "json");
			if (string.IsNullOrWhiteSpace(path))
			{
				return;
			}

			System.IO.File.WriteAllText(path, json);
			EditorUtility.RevealInFinder(path);
		}
	}
}
