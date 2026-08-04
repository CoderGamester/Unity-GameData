using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GameLovers.GameData
{
	/// <summary>
	/// A hash set with the possibility to observe changes to its elements defined <see cref="ObservableUpdateType"/> rules.
	/// </summary>
	public interface IObservableHashSetReader<T> : IEnumerable<T>
	{
		/// <summary>
		/// Requests the hash set element count.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Starts a batch update for this hash set.
		/// Notifications will be suppressed until the returned object is disposed.
		/// </summary>
		IDisposable BeginBatch();

		/// <summary>
		/// Checks if the hash set contains the given <paramref name="item"/>.
		/// </summary>
		bool Contains(T item);

		/// <summary>
		/// Observes to this hash set changes with the given <paramref name="onUpdate"/>.
		/// </summary>
		void Observe(Action<T, ObservableUpdateType> onUpdate);

		/// <summary>
		/// Stops observing this hash set changes with the given <paramref name="onUpdate"/>.
		/// </summary>
		void StopObserving(Action<T, ObservableUpdateType> onUpdate);

		/// <summary>
		/// Stops observing this hash set changes from all the given <paramref name="subscriber"/> calls.
		/// If the given <paramref name="subscriber"/> is null then will stop observing from everything.
		/// </summary>
		void StopObservingAll(object subscriber = null);
	}

	/// <inheritdoc />
	public interface IObservableHashSet<T> : IObservableHashSetReader<T>
	{
		/// <summary>
		/// Adds the given <paramref name="item"/> to the hash set.
		/// Returns true if the item was added, false if it already exists.
		/// </summary>
		bool Add(T item);

		/// <summary>
		/// Removes the given <paramref name="item"/> from the hash set.
		/// Returns true if the item was removed, false if it doesn't exist.
		/// </summary>
		bool Remove(T item);

		/// <summary>
		/// Clears the hash set.
		/// </summary>
		void Clear();
	}

	/// <inheritdoc />
	public partial class ObservableHashSet<T> : IObservableHashSet<T>, IBatchable, IComputedDependency
	{
		private readonly HashSet<T> _hashSet;
		private readonly IList<Action<T, ObservableUpdateType>> _updateActions = new List<Action<T, ObservableUpdateType>>();
		private readonly List<Action> _dependencyActions = new List<Action>();
		private bool _isBatching;

		// Declared as a partial method so calls are compiled out in player builds.
		partial void EditorDebug_Register();

		/// <inheritdoc />
		public int Count
		{
			get
			{
				ComputedTracker.OnRead(this);
				return _hashSet.Count;
			}
		}

		public ObservableHashSet()
		{
			_hashSet = new HashSet<T>();
			EditorDebug_Register();
		}

		public ObservableHashSet(IEnumerable<T> collection)
		{
			_hashSet = new HashSet<T>(collection);
			EditorDebug_Register();
		}

		public ObservableHashSet(IEqualityComparer<T> comparer)
		{
			_hashSet = new HashSet<T>(comparer);
			EditorDebug_Register();
		}

		/// <inheritdoc />
		void IComputedDependency.Subscribe(Action onDependencyChanged)
		{
			_dependencyActions.Add(onDependencyChanged);
		}

		/// <inheritdoc />
		void IComputedDependency.Unsubscribe(Action onDependencyChanged)
		{
			_dependencyActions.Remove(onDependencyChanged);
		}

		/// <inheritdoc />
		public IDisposable BeginBatch()
		{
			var batch = new ObservableBatch();
			batch.Add(this);
			return batch;
		}

		/// <inheritdoc />
		void IBatchable.SuppressNotifications()
		{
			_isBatching = true;
		}

		/// <inheritdoc />
		void IBatchable.ResumeNotifications()
		{
			if (_isBatching)
			{
				_isBatching = false;
				foreach (var item in _hashSet)
				{
					InvokeUpdate(item, ObservableUpdateType.Added);
				}
			}
		}

		/// <inheritdoc />
		public bool Contains(T item)
		{
			ComputedTracker.OnRead(this);
			return _hashSet.Contains(item);
		}

		/// <inheritdoc />
		public void Observe(Action<T, ObservableUpdateType> onUpdate)
		{
			_updateActions.Add(onUpdate);
		}

		/// <inheritdoc />
		public void StopObserving(Action<T, ObservableUpdateType> onUpdate)
		{
			_updateActions.Remove(onUpdate);
		}

		/// <inheritdoc />
		public void StopObservingAll(object subscriber = null)
		{
			if (subscriber == null)
			{
				_updateActions.Clear();
				return;
			}

			for (var i = _updateActions.Count - 1; i > -1; i--)
			{
				if (_updateActions[i].Target == subscriber)
				{
					_updateActions.RemoveAt(i);
				}
			}
		}

		/// <inheritdoc />
		public bool Add(T item)
		{
			if (_hashSet.Add(item))
			{
				InvokeUpdate(item, ObservableUpdateType.Added);
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		public bool Remove(T item)
		{
			if (_hashSet.Remove(item))
			{
				InvokeUpdate(item, ObservableUpdateType.Removed);
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		public void Clear()
		{
			if (!_isBatching)
			{
				var copy = _updateActions.ToList();
				foreach (var item in _hashSet)
				{
					foreach (var action in copy)
					{
						action(item, ObservableUpdateType.Removed);
					}
				}

				for (var i = 0; i < _dependencyActions.Count; i++)
				{
					_dependencyActions[i].Invoke();
				}
			}
			_hashSet.Clear();
		}

		private void InvokeUpdate(T item, ObservableUpdateType updateType)
		{
			if (_isBatching)
			{
				return;
			}

			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i](item, updateType);
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}
		}

		/// <summary>
		/// Enumerates the underlying set directly, so mutating the observable during enumeration throws
		/// just as it would on a plain <see cref="HashSet{T}"/>.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			return _hashSet.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}


		// ═══════════════════════════════════════════════════════════════════════════
		// EDITOR-ONLY: Observable Debug Window Support
		// ═══════════════════════════════════════════════════════════════════════════
		// This section provides automatic registration of observable instances for
		// the Observable Debug Window (Tools > Game Data > Observable Debugger).
		//
		// Features:
		// - Zero configuration required from users
		// - Automatic tracking using weak references (no memory leaks)
		// - Live value/subscriber inspection via captured getters
		//
		// This code is compiled out in builds via #if UNITY_EDITOR.
		// ═══════════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR
		partial void EditorDebug_Register()
		{
			ObservableDebugRegistry.Register(
				instance: this,
				kind: "HashSet",
				valueGetter: () => $"Count: {Count}",
				subscriberCountGetter: () => _updateActions.Count);
		}
#endif
	}
}
