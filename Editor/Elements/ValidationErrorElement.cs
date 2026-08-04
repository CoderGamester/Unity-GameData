using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.GameData.Editor
{
	/// <summary>
	/// UI Toolkit element to display a single validation error row.
	/// Intended for use in the Config Browser validation results panel.
	/// </summary>
	public sealed class ValidationErrorElement : VisualElement
	{
		private readonly Label _title;
		private readonly Label _detail;

		private string _configType;
		private int? _configId;

		/// <summary>Raised with the config name and optional row index when the row is clicked.</summary>
		public event Action<string, int?> Clicked;

		public ValidationErrorElement()
		{
			style.flexDirection = FlexDirection.Column;
			style.paddingLeft = 6;
			style.paddingRight = 6;
			style.paddingTop = 4;
			style.paddingBottom = 4;
			style.borderBottomWidth = 1;
			style.borderBottomColor = new StyleColor(new Color(0f, 0f, 0f, 0.15f));

			_title = new Label
			{
				style =
				{
					unityFontStyleAndWeight = FontStyle.Bold,
					marginBottom = 2
				}
			};

			_detail = new Label
			{
				style =
				{
					whiteSpace = WhiteSpace.Normal
				}
			};

			Add(_title);
			Add(_detail);

			RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(_configType, _configId));
		}

		/// <summary>
		/// Binds the element to a specific validation error.
		/// </summary>
		public void Bind(string configType, int? configId, string fieldName, string message)
		{
			_configType = configType;
			_configId = configId;

			var idStr = configId.HasValue ? $" (ID:{configId.Value})" : string.Empty;
			_title.text = $"[!] {configType}{idStr}";
			_detail.text = $"{fieldName}: {message}";
		}
	}
}

