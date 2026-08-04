using System;
using System.Collections.Generic;

namespace GameLovers.GameData
{
	/// <summary>
	/// A field with the possibility to observe changes to it's elements defined <see cref="ObservableUpdateType"/> rules
	/// </summary>
	public interface IObservableFieldReader<out T>
	{
		/// <summary>
		/// The field value
		/// </summary>
		T Value { get; }

		/// <summary>
		/// Starts a batch update for this field. 
		/// Notifications will be suppressed until the returned object is disposed.
		/// </summary>
		IDisposable BeginBatch();

		/// <summary>
		/// Observes this field with the given <paramref name="onUpdate"/> when any data changes
		/// </summary>
		void Observe(Action<T, T> onUpdate);

		/// <inheritdoc cref="Observe" />
		/// <remarks>
		/// It invokes the given <paramref name="onUpdate"/> method before starting to observe to this field
		/// </remarks>
		void InvokeObserve(Action<T, T> onUpdate);

		/// <summary>
		/// Stops observing this field with the given <paramref name="onUpdate"/> of any data changes
		/// </summary>
		void StopObserving(Action<T, T> onUpdate);

		/// <summary>
		/// Stops observing this field from all the given <paramref name="subscriber"/> calls.
		/// If the given <paramref name="subscriber"/> is null then will stop observing from everything.
		/// </summary>
		void StopObservingAll(object subscriber = null);

		/// <summary>
		/// Notifies this field's observers without changing the value.
		/// </summary>
		/// <remarks>
		/// It invokes any update method that is observing to this field
		/// </remarks>
		void InvokeUpdate();
	}

	/// <inheritdoc />
	public interface IObservableField<T> : IObservableFieldReader<T>
	{
		/// <summary>
		/// The field value with possibility to be changed
		/// </summary>
		new T Value { get; set; }

		/// <summary>
		/// Rebinds this field to a new value without losing existing observers.
		/// </summary>
		/// <param name="initialValue">The new initial value for the field</param>
		void Rebind(T initialValue);
	}

	/// <inheritdoc />
	public partial class ObservableField<T> : IObservableField<T>, IBatchable, IComputedDependency
	{
		private readonly IList<Action<T, T>> _updateActions = new List<Action<T, T>>();
		private readonly List<Action> _dependencyActions = new List<Action>();

		private T _value;
		private bool _isBatching;
		private T _batchPreviousValue;

		/// <inheritdoc cref="IObservableField{T}.Value" />
		public virtual T Value
		{
			get
			{
				ComputedTracker.OnRead(this);
				return _value;
			}
			set
			{
				var previousValue = _value;

				_value = value;
				InvokeUpdate(previousValue);
			}
		}

		public ObservableField()
		{
			_value = default;
			EditorDebug_Register();
		}

		public ObservableField(T initialValue)
		{
			_value = initialValue;
			EditorDebug_Register();
		}

		/// <summary>
		/// Reads the current value, so the field can be used directly where a <typeparamref name="T"/> is expected.
		/// </summary>
		public static implicit operator T(ObservableField<T> value) => value.Value;

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
			if (!_isBatching)
			{
				_isBatching = true;
				_batchPreviousValue = _value;
			}
		}

		/// <inheritdoc />
		void IBatchable.ResumeNotifications()
		{
			if (_isBatching)
			{
				_isBatching = false;
				InvokeUpdate(_batchPreviousValue);
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
		public void Rebind(T initialValue)
		{
			_value = initialValue;
		}

		/// <inheritdoc />
		public void Observe(Action<T, T> onUpdate)
		{
			_updateActions.Add(onUpdate);
		}

		/// <inheritdoc />
		public void InvokeObserve(Action<T, T> onUpdate)
		{
			onUpdate(Value, Value);

			Observe(onUpdate);
		}

		/// <inheritdoc />
		public void StopObserving(Action<T, T> onUpdate)
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
		public void InvokeUpdate()
		{
			InvokeUpdate(Value);
		}

		/// <summary>
		/// Gets the current value without triggering dependency tracking.
		/// Override in derived classes that store values differently.
		/// </summary>
		protected virtual T GetCurrentValue() => _value;

		/// <summary>
		/// Notifies subscribers. Suppressed while a batch is open, which then fires once on resume.
		/// </summary>
		protected void InvokeUpdate(T previousValue)
		{
			if (_isBatching)
			{
				return;
			}

			// Cache value to avoid repeated Value getter calls (which trigger ComputedTracker.OnRead)
			var currentValue = GetCurrentValue();

			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i].Invoke(previousValue, currentValue);
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}
		}

		// Declared as a partial method so calls are compiled out in player builds.
		partial void EditorDebug_Register();

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
				kind: "Field",
				valueGetter: () =>
				{
					object v = Value;
					return v?.ToString() ?? string.Empty;
				},
				subscriberCountGetter: () => _updateActions.Count);
		}
#endif
	}
}
