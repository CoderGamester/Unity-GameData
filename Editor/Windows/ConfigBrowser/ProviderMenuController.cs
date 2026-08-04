using System;
using System.Collections.Generic;
using System.Linq;
using GameLovers.GameData;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GameLovers.GameData.Editor
{
	/// <summary>
	/// Manages the provider selection dropdown in the Config Browser toolbar.
	/// Periodically refreshes the list of available <see cref="ConfigsProviderDebugRegistry.ProviderSnapshot"/>
	/// instances and raises <see cref="ProviderChanged"/> when the active provider changes.
	/// </summary>
	internal sealed class ProviderMenuController
	{
		private readonly ConfigBrowserView _view;
		private readonly List<ConfigsProviderDebugRegistry.ProviderSnapshot> _snapshots = new List<ConfigsProviderDebugRegistry.ProviderSnapshot>();

		private string _lastMenuText;
		private int _lastSnapshotCount = -1;

		private IConfigsProvider _provider;
		private int _selectedProviderId = -1;

		/// <summary>Raised when the user picks a different provider.</summary>
		public event Action ProviderChanged;

		/// <summary>The currently selected provider, or null when nothing is selected.</summary>
		public IConfigsProvider Provider => _provider;
		/// <summary>The id of the currently selected provider snapshot, or -1.</summary>
		public int SelectedProviderId => _selectedProviderId;
		/// <summary>All known provider snapshots, ordered by id.</summary>
		public IReadOnlyList<ConfigsProviderDebugRegistry.ProviderSnapshot> Snapshots => _snapshots;

		public ProviderMenuController(ConfigBrowserView view)
		{
			_view = view;
		}

		/// <summary>
		/// Re-reads the <see cref="ConfigsProviderDebugRegistry"/>, auto-selects the first provider
		/// when none is selected or the current provider is gone, and rebuilds the toolbar menu.
		/// </summary>
		public void RefreshSnapshots()
		{
			_snapshots.Clear();
			_snapshots.AddRange(ConfigsProviderDebugRegistry.EnumerateSnapshots().OrderBy(s => s.Id));

			var currentProviderGone = _provider != null && !_snapshots.Any(s => s.Id == _selectedProviderId);
			if (currentProviderGone || (_provider == null && _snapshots.Count > 0))
			{
				var first = _snapshots.FirstOrDefault();
				if (first.ProviderRef != null && first.ProviderRef.TryGetTarget(out var target))
				{
					SetProviderInternal(target, first.Id, fireEvent: true);
				}
				else
				{
					SetProviderInternal(null, -1, fireEvent: true);
				}
			}

			UpdateProviderMenu();
		}

		/// <summary>
		/// Clears the current provider selection and refreshes the toolbar menu.
		/// </summary>
		public void ClearSelection()
		{
			SetProviderInternal(null, -1, fireEvent: true);
			UpdateProviderMenu();
		}

		private void SetProviderInternal(IConfigsProvider provider, int selectedId, bool fireEvent)
		{
			var changed = !ReferenceEquals(_provider, provider) || _selectedProviderId != selectedId;
			_provider = provider;
			_selectedProviderId = selectedId;
			if (fireEvent && changed)
			{
				ProviderChanged?.Invoke();
			}
		}

		private void UpdateProviderMenu()
		{
			var providerMenu = _view.ProviderMenu;
			var toolbar = _view.Toolbar;
			if (providerMenu == null || toolbar == null) return;

			var currentSnapshot = _snapshots.FirstOrDefault(s => s.Id == _selectedProviderId);
			var menuText = _snapshots.Count == 0
				? "No providers"
				: currentSnapshot.Id != 0 ? $"{currentSnapshot.Name} ({currentSnapshot.ConfigTypeCount} types)" : "Select Provider";

			if (_lastMenuText == menuText && _lastSnapshotCount == _snapshots.Count)
			{
				return;
			}

			_lastMenuText = menuText;
			_lastSnapshotCount = _snapshots.Count;

			var newMenu = new ToolbarMenu { text = menuText };
			newMenu.style.minWidth = 280;

			// Add "None" option to clear the selection
			newMenu.menu.AppendAction("None", a =>
			{
				SetProviderInternal(null, -1, fireEvent: true);
			}, a => _provider == null ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

			if (_snapshots.Count > 0)
			{
				newMenu.menu.AppendSeparator();
			}

			foreach (var snap in _snapshots)
			{
				var s = snap;
				newMenu.menu.AppendAction($"{s.Name} ({s.ConfigTypeCount} types) ##{s.Id}", a =>
				{
					if (s.ProviderRef.TryGetTarget(out var target))
					{
						SetProviderInternal(target, s.Id, fireEvent: true);
					}
				}, a => s.Id == _selectedProviderId ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
			}

			_view.ReplaceProviderMenu(newMenu);
		}
	}
}
