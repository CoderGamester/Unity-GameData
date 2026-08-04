using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GameLovers.GameData
{
	/// <summary>
	/// A list with the possibility to observe changes to it's elements defined <see cref="ObservableUpdateType"/> rules
	/// </summary>
	public interface IObservableListReader : IEnumerable
	{
		/// <summary>
		/// Requests the list element count
		/// </summary>
		int Count { get; }
	}

	/// <inheritdoc cref="IObservableListReader"/>
	/// <remarks>
	/// Read only observable list interface
	/// </remarks>
	public interface IObservableListReader<T> : IObservableListReader, IEnumerable<T>
	{
		/// <summary>
		/// Looks up and return the data that is associated with the given <paramref name="index"/>
		/// </summary>
		T this[int index] { get; }

		/// <summary>
		/// Starts a batch update for this list. 
		/// Notifications will be suppressed until the returned object is disposed.
		/// </summary>
		IDisposable BeginBatch();

		/// <summary>
		/// Requests this list as a <see cref="IReadOnlyList{T}"/>
		/// </summary>
		IReadOnlyList<T> ReadOnlyList { get; }

		/// <inheritdoc cref="List{T}.Contains"/>
		bool Contains(T value);

		/// <inheritdoc cref="List{T}.IndexOf(T)"/>
		int IndexOf(T value);

		/// <summary>
		/// Observes to this list changes with the given <paramref name="onUpdate"/>
		/// </summary>
		void Observe(Action<int, T, T, ObservableUpdateType> onUpdate);

		/// <summary>
		/// Observes this list with the given <paramref name="onUpdate"/> when any data changes and invokes it with the given <paramref name="index"/>
		/// </summary>
		void InvokeObserve(int index, Action<int, T, T, ObservableUpdateType> onUpdate);

		/// <summary>
		/// Stops observing this dictionary with the given <paramref name="onUpdate"/> of any data changes
		/// </summary>
		void StopObserving(Action<int, T, T, ObservableUpdateType> onUpdate);

		/// <summary>
		/// Stops observing this dictionary changes from all the given <paramref name="subscriber"/> calls.
		/// If the given <paramref name="subscriber"/> is null then will stop observing from everything.
		/// </summary>
		void StopObservingAll(object subscriber = null);
	}

	/// <inheritdoc />
	public interface IObservableList<T> : IObservableListReader<T>
	{
		/// <summary>
		/// Changes the given <paramref name="index"/> in the list. If the data does not exist it will be added.
		/// It will notify any observer listing to its data
		/// </summary>
		new T this[int index] { get; set; }

		/// <inheritdoc cref="List{T}.Add"/>
		void Add(T data);

		/// <inheritdoc cref="List{T}.Remove"/>
		bool Remove(T data);

		/// <inheritdoc cref="List{T}.RemoveAt"/>
		void RemoveAt(int index);

		/// <inheritdoc cref="List{T}.Clear"/>
		void Clear();

		/// <summary>
		/// Notifies the observers of a single index without changing its value.
		/// </summary>
		/// <remarks>
		/// It invokes any update method that is observing to the given <paramref name="index"/> on this list
		/// </remarks>
		void InvokeUpdate(int index);
	}

	/// <inheritdoc />
	public partial class ObservableList<T> : IObservableList<T>, IBatchable, IComputedDependency
	{
		private readonly IList<Action<int, T, T, ObservableUpdateType>> _updateActions = new List<Action<int, T, T, ObservableUpdateType>>();
		private readonly List<Action> _dependencyActions = new List<Action>();
		private bool _isBatching;

		// Declared as a partial method so calls are compiled out in player builds.
		partial void EditorDebug_Register();

		/// <inheritdoc cref="IObservableList{T}.this" />
		public T this[int index]
		{
			get
			{
				ComputedTracker.OnRead(this);
				return List[index];
			}
			set
			{
				var previousValue = List[index];

				List[index] = value;

				InvokeUpdate(index, previousValue);
			}
		}

		/// <inheritdoc />
		public int Count
		{
			get
			{
				ComputedTracker.OnRead(this);
				return List.Count;
			}
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
		public IReadOnlyList<T> ReadOnlyList => new List<T>(List);

		/// <summary>The backing list; override to change the storage a subclass reads through.</summary>
		protected virtual List<T> List { get; set; }

		protected ObservableList()
		{
			EditorDebug_Register();
		}

		public ObservableList(IList<T> list)
		{
			List = list as List<T> ?? list.ToList();
			EditorDebug_Register();
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
				for (var i = 0; i < List.Count; i++)
				{
					InvokeUpdate(i, default);
				}
			}
		}

		/// <summary>
		/// Rebinds this list to a new list without losing existing observers.
		/// </summary>
		/// <param name="list">The new list to bind to</param>
		public void Rebind(IList<T> list)
		{
			List = list as List<T> ?? list.ToList();
		}

		/// <inheritdoc cref="List{T}.GetEnumerator"/>
		public List<T>.Enumerator GetEnumerator()
		{
			return List.GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return List.GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return List.GetEnumerator();
		}

		/// <inheritdoc />
		public bool Contains(T value)
		{
			ComputedTracker.OnRead(this);
			return List.Contains(value);
		}

		/// <inheritdoc />
		public int IndexOf(T value)
		{
			ComputedTracker.OnRead(this);
			return List.IndexOf(value);
		}

		/// <inheritdoc />
		public virtual void Add(T data)
		{
			List.Add(data);

			if (_isBatching)
			{
				return;
			}

			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i](List.Count - 1, default, data, ObservableUpdateType.Added);
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}
		}

		/// <inheritdoc />
		public bool Remove(T data)
		{
			var idx = List.IndexOf(data);

			if (idx >= 0)
			{
				RemoveAt(idx);

				return true;
			}

			return false;
		}

		/// <inheritdoc />
		public virtual void RemoveAt(int index)
		{
			var data = List[index];

			List.RemoveAt(index);

			if (_isBatching)
			{
				return;
			}

			for (var i = _updateActions.Count - 1; i > -1; i--)
			{
				var action = _updateActions[i];

				action(index, data, default, ObservableUpdateType.Removed);

				// Shift the index if an action was unsubscribed
				i = AdjustIndex(i, action);
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}
		}

		/// <inheritdoc />
		public virtual void Clear()
		{
			if (!_isBatching)
			{
				// Create a copy in case that one of the callbacks modifies the list (Ex: removing a subscriber)
				var copy = _updateActions.ToList();

				for (var i = copy.Count - 1; i > -1; i--)
				{
					for (var j = 0; j < List.Count; j++)
					{
						copy[i](j, List[j], default, ObservableUpdateType.Removed);
					}
				}
			}

			List.Clear();

			if (!_isBatching)
			{
				for (var i = 0; i < _dependencyActions.Count; i++)
				{
					_dependencyActions[i].Invoke();
				}
			}
		}

		/// <inheritdoc />
		public void InvokeUpdate(int index)
		{
			InvokeUpdate(index, List[index]);
		}

		/// <inheritdoc />
		public void Observe(Action<int, T, T, ObservableUpdateType> onUpdate)
		{
			_updateActions.Add(onUpdate);
		}

		/// <inheritdoc />
		public void InvokeObserve(int index, Action<int, T, T, ObservableUpdateType> onUpdate)
		{
			Observe(onUpdate);
			InvokeUpdate(index);
		}

		/// <inheritdoc />
		public void StopObserving(Action<int, T, T, ObservableUpdateType> onUpdate)
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

		/// <summary>
		/// Notifies subscribers. Suppressed while a batch is open, which then fires once on resume.
		/// </summary>
		protected void InvokeUpdate(int index, T previousValue)
		{
			if (_isBatching)
			{
				return;
			}

			var data = List[index];

			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i](index, previousValue, data, ObservableUpdateType.Updated);
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}
		}

		private int AdjustIndex(int index, Action<int, T, T, ObservableUpdateType> action)
		{
			if (index < _updateActions.Count && _updateActions[index] == action)
			{
				return index;
			}

			for (var i = index - 1; i > -1; i--)
			{
				if (_updateActions[i] == action)
				{
					return i;
				}
			}

			return index + 1;
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
				kind: "List",
				valueGetter: () => $"Count: {Count}",
				subscriberCountGetter: () => _updateActions.Count);
		}
#endif
	}
}
