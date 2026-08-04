using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.GameData.Editor
{
	/// <summary>
	/// UI Toolkit view that renders the migration panel layout: a list of migration rows,
	/// a preview section with before/after JSON viewers, and controls for previewing and applying migrations.
	/// This view is purely presentational; logic lives in <see cref="MigrationPanelController"/>.
	/// </summary>
	internal sealed class MigrationPanelView : VisualElement
	{
		private Label _header;
		private HelpBox _emptyState;
		private VisualElement _contentContainer;
		private ListView _listView;
		private List<MigrationRow> _rows = new List<MigrationRow>();

		private TextField _customJsonInput;
		private DropdownField _migrationDropdown;
		private Button _previewButton;
		private Button _applyButton;
		private JsonViewerElement _inputJson;
		private JsonViewerElement _outputJson;
		private Label _logLabel;

		/// <summary>Raised by the preview button.</summary>
		public event Action PreviewRequested;
		/// <summary>Raised by the apply button.</summary>
		public event Action ApplyRequested;

		public MigrationPanelView()
		{
			style.flexGrow = 1;
			style.paddingLeft = 8;
			style.paddingRight = 8;
			style.paddingTop = 6;
			style.paddingBottom = 6;

			_header = new Label("Migrations");
			_header.style.unityFontStyleAndWeight = FontStyle.Bold;
			_header.style.marginBottom = 6;

			_emptyState = new HelpBox("No migrations available in this project.", HelpBoxMessageType.Info);
			_emptyState.style.display = DisplayStyle.None;

			// Vertical split: migrations list (top) + preview section (bottom)
			var verticalSplit = new TwoPaneSplitView(1, 220, TwoPaneSplitViewOrientation.Vertical);
			verticalSplit.viewDataKey = "ConfigBrowser_MigrationVerticalSplit";
			verticalSplit.style.flexGrow = 1;

			verticalSplit.Add(BuildMigrationsList());
			verticalSplit.Add(BuildPreview());

			_contentContainer = new VisualElement { style = { flexGrow = 1 } };
			_contentContainer.Add(verticalSplit);

			Add(_header);
			Add(_emptyState);
			Add(_contentContainer);
		}

		/// <summary>
		/// Gets the trimmed custom JSON input provided by the user, or null when empty.
		/// </summary>
		public string CustomJson
		{
			get
			{
				var value = _customJsonInput?.value;
				return value == null ? null : value.Trim();
			}
		}

		/// <summary>
		/// Gets the currently selected migration dropdown index, or -1 if nothing is selected.
		/// </summary>
		public int SelectedIndex => _migrationDropdown?.index ?? -1;

		/// <summary>
		/// Updates the header label text.
		/// </summary>
		public void SetHeader(string text)
		{
			_header.text = text;
		}

		/// <summary>
		/// Toggles between the empty-state help box and the content container.
		/// </summary>
		public void SetEmptyStateVisible(bool visible)
		{
			_emptyState.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
			_contentContainer.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
		}

		/// <summary>
		/// Replaces the displayed migration rows and refreshes the dropdown choices.
		/// </summary>
		public void SetRows(List<MigrationRow> rows)
		{
			_rows = rows ?? new List<MigrationRow>();
			_listView.itemsSource = _rows;
			_listView.RefreshItems();

			var choices = _rows
				.Select(r => $"{r.ConfigType.Name}: v{r.FromVersion} → v{r.ToVersion}")
				.ToList();

			_migrationDropdown.choices = choices;
			if (choices.Count > 0)
			{
				_migrationDropdown.index = 0;
			}
		}

		/// <summary>
		/// Enables or disables the preview and apply buttons.
		/// </summary>
		public void SetButtonsEnabled(bool hasChoices, bool canApply)
		{
			_previewButton.SetEnabled(hasChoices);
			_applyButton.SetEnabled(canApply);
		}

		/// <summary>
		/// Displays the given JSON string in the input (before) viewer.
		/// </summary>
		public void SetInputJson(string json)
		{
			_inputJson.SetJson(json);
		}

		/// <summary>
		/// Displays the given JSON string in the output (after) viewer.
		/// </summary>
		public void SetOutputJson(string json)
		{
			_outputJson.SetJson(json);
		}

		/// <summary>
		/// Sets the migration log message displayed below the preview.
		/// </summary>
		public void SetLog(string message)
		{
			_logLabel.text = message ?? string.Empty;
		}

		private VisualElement BuildMigrationsList()
		{
			var container = new VisualElement { style = { flexGrow = 1, minHeight = 50 } };

			_listView = new ListView
			{
				selectionType = SelectionType.Single,
				showBorder = true,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				style = { flexGrow = 1 }
			};

			_listView.makeItem = MakeRow;
			_listView.bindItem = (e, i) => BindRow(e, i);

			container.Add(_listView);
			return container;
		}

		private VisualElement BuildPreview()
		{
			var container = new VisualElement
			{
				style =
				{
					flexGrow = 1,
					minHeight = 60,
					borderTopWidth = 1,
					borderTopColor = new StyleColor(new Color(0f, 0f, 0f, 0.2f)),
					paddingTop = 4
				}
			};

			var title = new Label("Migration Preview");
			title.style.unityFontStyleAndWeight = FontStyle.Bold;
			title.style.marginBottom = 4;

			// Custom JSON input section
			var customInputSection = new VisualElement { style = { marginBottom = 6 } };
			var customInputLabel = new Label("Custom Input JSON (paste legacy-schema JSON here):");
			customInputLabel.style.marginBottom = 2;

			_customJsonInput = new TextField
			{
				multiline = true,
				style =
				{
					minHeight = 40,
					maxHeight = 100
				}
			};
			_customJsonInput.AddToClassList("unity-text-field__input");

			// Migration selection and preview button row
			var previewControlsRow = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					marginTop = 4
				}
			};

			var dropdownLabel = new Label("Target Version:") { style = { marginRight = 4 } };
			_migrationDropdown = new DropdownField { style = { minWidth = 120, marginRight = 8 } };
			_previewButton = new Button(() => PreviewRequested?.Invoke()) { text = "Preview" };
			_applyButton = new Button(() => ApplyRequested?.Invoke()) { text = "Apply Migration" };
			_applyButton.style.marginLeft = 8;

			previewControlsRow.Add(dropdownLabel);
			previewControlsRow.Add(_migrationDropdown);
			previewControlsRow.Add(_previewButton);
			previewControlsRow.Add(_applyButton);

			customInputSection.Add(customInputLabel);
			customInputSection.Add(_customJsonInput);
			customInputSection.Add(previewControlsRow);

			var split = new TwoPaneSplitView(0, 360, TwoPaneSplitViewOrientation.Horizontal);
			split.style.flexGrow = 1;
			split.style.minHeight = 60;

			var left = new VisualElement { style = { flexGrow = 1 } };
			left.Add(new Label("Input"));
			_inputJson = new JsonViewerElement();
			left.Add(_inputJson);

			var right = new VisualElement { style = { flexGrow = 1 } };
			right.Add(new Label("Output"));
			_outputJson = new JsonViewerElement();
			right.Add(_outputJson);

			split.Add(left);
			split.Add(right);

			_logLabel = new Label();
			_logLabel.style.marginTop = 4;
			_logLabel.style.whiteSpace = WhiteSpace.Normal;

			container.Add(title);
			container.Add(customInputSection);
			container.Add(split);
			container.Add(_logLabel);
			return container;
		}

		private static VisualElement MakeRow()
		{
			var row = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					paddingLeft = 6,
					paddingRight = 6,
					paddingTop = 2,
					paddingBottom = 2
				}
			};

			row.Add(new Label { name = "Type", style = { minWidth = 160 } });
			row.Add(new Label { name = "Migration", style = { minWidth = 100 } });
			row.Add(new Label { name = "State", style = { minWidth = 90 } });

			return row;
		}

		private void BindRow(VisualElement row, int index)
		{
			if (index < 0 || index >= _rows.Count) return;
			var data = _rows[index];

			row.Q<Label>("Type").text = data.ConfigType.Name;
			row.Q<Label>("Migration").text = $"v{data.FromVersion} -> v{data.ToVersion}";
			row.Q<Label>("State").text = data.State.ToString();
		}
	}
}
