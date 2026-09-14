using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.GameData.Editor
{
	/// <summary>
	/// UI Toolkit inspector for any concrete type deriving from <see cref="ConfigsScriptableObject{TId,TAsset}"/>.
	/// Shows per-entry status (duplicate keys / validation errors) and provides a "Validate All" action.
	/// </summary>
	[CustomEditor(typeof(ConfigsScriptableObject<,>), true)]
	public sealed class ConfigsScriptableObjectInspector : UnityEditor.Editor
	{
		private const string ConfigsFieldName = "_configs";
		private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		private static readonly Color _colorOk = new Color(0.25f, 0.6f, 0.3f);
		private static readonly Color _colorWarning = new Color(0.7f, 0.55f, 0.15f);
		private static readonly Color _colorError = new Color(0.8f, 0.25f, 0.25f);
		private static readonly Color _colorBorder = new Color(0.2f, 0.2f, 0.2f, 0.5f);

		private readonly Dictionary<int, EntryStatus> _statusByIndex = new Dictionary<int, EntryStatus>();
		private readonly HashSet<object> _seenKeys = new HashSet<object>();

		private ListView _listView;
		private Label _entriesLabel;
		private Label _statsLabel;
		private VisualElement _statusIndicator;

		/// <inheritdoc />
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();
			var configsProp = serializedObject.FindProperty(ConfigsFieldName);

			if (configsProp == null || !configsProp.isArray)
			{
				root.Add(new HelpBox($"Expected serialized array field '{ConfigsFieldName}' on {target.GetType().Name}.", HelpBoxMessageType.Error));
				InspectorElement.FillDefaultInspector(root, serializedObject, this);
				return root;
			}

			root.Add(BuildHeader(configsProp));
			root.Add(BuildListView(configsProp));
			root.TrackSerializedObjectValue(serializedObject, _ => Revalidate(configsProp, showFeedback: false));
			Revalidate(configsProp, showFeedback: false);

			return root;
		}

		private VisualElement BuildHeader(SerializedProperty configsProp)
		{
			var header = CreateFlexRow(Justify.SpaceBetween);
			header.style.marginBottom = 6;

			var leftContainer = CreateFlexRow();
			_statusIndicator = CreateCircleIndicator(12, 6);
			_statusIndicator.style.marginRight = 8;
			_statusIndicator.style.marginTop = 3;

			var statsContainer = new VisualElement();
			_entriesLabel = CreateBoldLabel();
			_statsLabel = CreateBoldLabel();
			statsContainer.Add(_entriesLabel);
			statsContainer.Add(_statsLabel);

			leftContainer.Add(_statusIndicator);
			leftContainer.Add(statsContainer);

			header.Add(leftContainer);
			header.Add(new Button(() => ValidateAllWithFeedback(configsProp)) { text = "Validate All" });

			return header;
		}

		private VisualElement BuildListView(SerializedProperty configsProp)
		{
			_listView = new ListView
			{
				selectionType = SelectionType.Single,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				reorderable = true,
				showBorder = true,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				style = { flexGrow = 1, minHeight = 200 }
			};

			_listView.itemsSource = CreateIndexSource(configsProp.arraySize);
			_listView.makeItem = MakeRow;
			_listView.bindItem = (e, i) => BindRow(e, configsProp, i);
			_listView.unbindItem = (e, _) => UnbindRow(e);
			_listView.itemsChosen += _ => Revalidate(configsProp, showFeedback: false);

			return _listView;
		}

		private static VisualElement MakeRow()
		{
			var row = CreateFlexRow();
			row.style.paddingLeft = row.style.paddingRight = row.style.paddingTop = row.style.paddingBottom = 4;
			row.style.borderBottomWidth = 1;
			row.style.borderBottomColor = _colorBorder;

			var keyField = new PropertyField { name = "KeyField" };
			SetFixedWidth(keyField, 120);
			keyField.style.marginRight = 8;

			var valueContainer = new Foldout { name = "ValueContainer", text = "Value", value = true };
			valueContainer.style.flexGrow = 1;
			valueContainer.style.flexShrink = 1;

			var statusLabel = new Label { name = "StatusLabel" };
			SetFixedWidth(statusLabel, 0, 80);
			statusLabel.style.unityTextAlign = TextAnchor.MiddleRight;
			statusLabel.style.marginLeft = 8;

			row.Add(keyField);
			row.Add(valueContainer);
			row.Add(statusLabel);

			return row;
		}

		private void BindRow(VisualElement row, SerializedProperty configsProp, int index)
		{
			if (index >= configsProp.arraySize) 
				return;

			var elementProp = configsProp.GetArrayElementAtIndex(index);
			var keyProp = elementProp.FindPropertyRelative("Key");
			var valueProp = elementProp.FindPropertyRelative("Value");

			var keyField = row.Q<PropertyField>("KeyField");
			var valueContainer = row.Q<Foldout>("ValueContainer");
			var statusLabel = row.Q<Label>("StatusLabel");

			keyField.label = $"[{index}] Key";
			if (keyProp != null) keyField.BindProperty(keyProp);
			if (valueProp != null) PopulateValueContainer(valueContainer, valueProp);

			statusLabel.text = GetStatusText(index, out var level);
			statusLabel.style.color = GetStatusColor(level);
		}

		private static void PopulateValueContainer(Foldout container, SerializedProperty valueProp)
		{
			container.contentContainer.Clear();

			if (!valueProp.hasVisibleChildren)
			{
				// Simple type - bind directly to preserve label
				var simpleField = new PropertyField { label = string.Empty };
				simpleField.BindProperty(valueProp);
				container.contentContainer.Add(simpleField);
				return;
			}

			// Complex type - iterate through child properties
			var iterator = valueProp.Copy();
			var endProperty = valueProp.GetEndProperty();

			if (iterator.NextVisible(true))
			{
				do
				{
					if (SerializedProperty.EqualContents(iterator, endProperty)) 
						break;

					// Use BindProperty with a copy to preserve the label
					var childField = new PropertyField { label = ObjectNames.NicifyVariableName(iterator.name) };
					childField.BindProperty(iterator.Copy());
					container.contentContainer.Add(childField);
				}
				while (iterator.NextVisible(false));
			}
		}

		private static void UnbindRow(VisualElement row)
		{
			row.Q<PropertyField>("KeyField")?.Unbind();
			var valueContainer = row.Q<Foldout>("ValueContainer");
      
			if (valueContainer == null) 
				return;

			foreach (var child in valueContainer.contentContainer.Children())
			{
				if (child is PropertyField pf) pf.Unbind();
			}
		}

		private void ValidateAllWithFeedback(SerializedProperty configsProp)
		{
			var (errorCount, duplicateCount) = Revalidate(configsProp, showFeedback: true);
			var entryCount = configsProp.arraySize;
			var hasIssues = errorCount > 0 || duplicateCount > 0;

			var logMessage = hasIssues
				? $"[{target.name}] Validation completed: {entryCount} entries, {errorCount} errors, {duplicateCount} duplicates"
				: $"[{target.name}] Validation passed: {entryCount} entries, no issues found";

			if (hasIssues) Debug.LogWarning(logMessage, target);
			else Debug.Log(logMessage, target);

			var dialogTitle = hasIssues ? "Validation Issues Found" : "Validation Passed";
			var dialogMessage = hasIssues
				? $"Found {errorCount} error(s) and {duplicateCount} duplicate(s) in {entryCount} entries.\n\nCheck the console for details."
				: $"All {entryCount} entries passed validation successfully.";

			EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "OK");
		}

		private (int errorCount, int duplicateCount) Revalidate(SerializedProperty configsProp, bool showFeedback)
		{
			serializedObject.Update();
			_statusByIndex.Clear();
			_seenKeys.Clear();

			var list = TryGetConfigsList(target);
			var errorCount = 0;
			var duplicateCount = 0;

			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					var pair = list[i];
					if (pair == null)
					{
						_statusByIndex[i] = EntryStatus.Error("Null entry");
						errorCount++;
						continue;
					}

					var pairType = pair.GetType();
					var key = pairType.GetField("Key")?.GetValue(pair);
					var value = pairType.GetField("Value")?.GetValue(pair);
					var isDuplicate = key != null && !_seenKeys.Add(key);
					var validationErrors = ValidateObject(value);

					if (isDuplicate) duplicateCount++;
					errorCount += validationErrors.Count;

					_statusByIndex[i] = isDuplicate
						? EntryStatus.DuplicateKey(validationErrors.Count)
						: validationErrors.Count > 0 ? EntryStatus.Errors(validationErrors.Count) : EntryStatus.Ok();
				}
			}

			_entriesLabel.text = $"Entries: {configsProp.arraySize}";
			_statsLabel.text = $"Errors: {errorCount} | Duplicates: {duplicateCount}";
			_statusIndicator.style.backgroundColor = (errorCount > 0 || duplicateCount > 0) ? _colorError : _colorOk;

			_listView.itemsSource = CreateIndexSource(configsProp.arraySize);
			_listView.RefreshItems();

			return (errorCount, duplicateCount);
		}

		private static List<string> ValidateObject(object obj)
		{
			var messages = new List<string>();
			if (obj == null) return messages;

			var type = obj.GetType();
			foreach (var field in type.GetFields(InstanceFlags))
				AddValidationMessages(field.GetCustomAttributes(typeof(ValidationAttribute), true), field.GetValue(obj), field.Name, messages);

			foreach (var prop in type.GetProperties(InstanceFlags).Where(p => p.CanRead))
				AddValidationMessages(prop.GetCustomAttributes(typeof(ValidationAttribute), true), prop.GetValue(obj), prop.Name, messages);

			return messages;
		}

		private static void AddValidationMessages(object[] attributes, object value, string memberName, List<string> messages)
		{
			foreach (var attr in attributes)
			{
				if (attr is ValidationAttribute va && !va.IsValid(value, out var msg))
					messages.Add($"{memberName}: {msg}");
			}
		}

		private string GetStatusText(int index, out StatusLevel level)
		{
			if (!_statusByIndex.TryGetValue(index, out var status))
			{
				level = StatusLevel.Info;
				return "…";
			}
			level = status.Level;
			return status.Text;
		}

		private static VisualElement CreateFlexRow(Justify justify = Justify.FlexStart)
		{
			return new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexStart, justifyContent = justify } };
		}

		private static VisualElement CreateCircleIndicator(int size, int radius)
		{
			return new VisualElement
			{
				style =
				{
					width = size, height = size,
					borderTopLeftRadius = radius, borderTopRightRadius = radius,
					borderBottomLeftRadius = radius, borderBottomRightRadius = radius
				}
			};
		}

		private static Label CreateBoldLabel() => new Label { style = { unityFontStyleAndWeight = FontStyle.Bold } };

		private static void SetFixedWidth(VisualElement el, int width, int minWidth = 0)
		{
			el.style.flexGrow = 0;
			el.style.flexShrink = 0;
			if (width > 0) el.style.width = width;
			if (minWidth > 0) el.style.minWidth = minWidth;
		}

		private static StyleColor GetStatusColor(StatusLevel level) => level switch
		{
			StatusLevel.Ok => new StyleColor(_colorOk),
			StatusLevel.Warning => new StyleColor(_colorWarning),
			StatusLevel.Error => new StyleColor(_colorError),
			_ => StyleKeyword.Null
		};

		private static List<int> CreateIndexSource(int size) => Enumerable.Range(0, size).ToList();

		private static IList TryGetConfigsList(UnityEngine.Object targetObject)
		{
			if (targetObject == null) return null;
			return targetObject.GetType().GetField(ConfigsFieldName, InstanceFlags)?.GetValue(targetObject) as IList;
		}

		private readonly struct EntryStatus
		{
			public readonly string Text;
			public readonly StatusLevel Level;

			private EntryStatus(string text, StatusLevel level) { Text = text; Level = level; }

			public static EntryStatus Ok() => new EntryStatus("OK", StatusLevel.Ok);
			public static EntryStatus Errors(int count) => new EntryStatus($"Errors: {count}", StatusLevel.Error);
			public static EntryStatus DuplicateKey(int errorCount) => new EntryStatus(errorCount > 0 ? $"DUPLICATE (+{errorCount})" : "DUPLICATE", StatusLevel.Warning);
			public static EntryStatus Error(string message) => new EntryStatus(message, StatusLevel.Error);
		}

		private enum StatusLevel { Info, Ok, Warning, Error }
	}
}
