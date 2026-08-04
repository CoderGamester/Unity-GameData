using System;
using System.Collections.Generic;

namespace GameLovers.GameData
{
	/// <summary>
	/// Interface for objects that can notify when they are being read for dependency tracking.
	/// </summary>
	internal interface IComputedDependency
	{
		/// <summary>
		/// Subscribes to be notified when this dependency's value changes.
		/// </summary>
		void Subscribe(Action onDependencyChanged);

		/// <summary>
		/// Unsubscribes from change notifications for this dependency.
		/// </summary>
		void Unsubscribe(Action onDependencyChanged);
	}

	/// <summary>
	/// Internal interface for computed fields to receive dependency tracking without reflection.
	/// </summary>
	internal interface IComputedFieldInternal
	{
		/// <summary>
		/// Registers a dependency that this computed field relies on.
		/// When the dependency changes, this computed field will be marked dirty and recomputed.
		/// </summary>
		void AddDependency(IComputedDependency dependency);
	}

	/// <summary>
	/// A field that is computed from other observable fields.
	/// It automatically updates when any of its dependencies change.
	/// </summary>
	public partial class ComputedField<T> : IObservableFieldReader<T>, IDisposable, IBatchable, IComputedDependency, IComputedFieldInternal
	{
		private readonly Func<T> _computation;
		private readonly List<Action<T, T>> _updateActions = new List<Action<T, T>>();
		private readonly List<Action> _dependencyActions = new List<Action>();
		private readonly HashSet<IComputedDependency> _dependencies = new HashSet<IComputedDependency>();
		private T _value;
		private bool _isDirty = true;
		private bool _isBatching;

		// Declared as a partial method so calls are compiled out in player builds.
		partial void EditorDebug_Register();

		/// <inheritdoc />
		public T Value
		{
			get
			{
				if (_isDirty)
				{
					Recompute();
				}
				ComputedTracker.OnRead(this);
				return _value;
			}
		}

		public ComputedField(Func<T> computation)
		{
			_computation = computation;
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
			if (!_isBatching)
			{
				_isBatching = true;
			}
		}

		/// <inheritdoc />
		void IBatchable.ResumeNotifications()
		{
			if (_isBatching)
			{
				_isBatching = false;
				InvokeUpdate();
			}
		}

		/// <inheritdoc />
		public void Observe(Action<T, T> onUpdate)
		{
			// Ensure dependencies are tracked before adding observer
			if (_isDirty)
			{
				Recompute();
			}
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
			if (_isBatching)
			{
				return;
			}

			_isDirty = true;
			var previousValue = _value;
			Recompute();
			
			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i].Invoke(previousValue, _value);
			}

			for (var i = 0; i < _dependencyActions.Count; i++)
			{
				_dependencyActions[i].Invoke();
			}
		}

		private void Recompute()
		{
			// Track dependencies during computation
			ComputedTracker.BeginTracking(this);
			try
			{
				_value = _computation();
			}
			finally
			{
				ComputedTracker.EndTracking();
			}

			_isDirty = false;
		}

		/// <inheritdoc />
		void IComputedFieldInternal.AddDependency(IComputedDependency dependency)
		{
			if (ReferenceEquals(dependency, this)) return;

			if (_dependencies.Add(dependency))
			{
				dependency.Subscribe(OnDependencyChanged);
			}
		}

		private void OnDependencyChanged()
		{
			if (!_isDirty)
			{
				_isDirty = true;
				InvokeUpdate();
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

		/// <summary>
		/// Unsubscribes from every tracked dependency.
		/// </summary>
		public void Dispose()
		{
			foreach (var dependency in _dependencies)
			{
				dependency.Unsubscribe(OnDependencyChanged);
			}
			_dependencies.Clear();
			_updateActions.Clear();
			_dependencyActions.Clear();
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
				kind: "Computed",
				valueGetter: () =>
				{
					object v = Value;
					return v?.ToString() ?? string.Empty;
				},
				subscriberCountGetter: () => _updateActions.Count);
		}
#endif
	}

	/// <summary>
	/// Static factory for computed fields.
	/// </summary>
	public static class ObservableField
	{
		/// <summary>
		/// Creates a new computed field from the given computation function.
		/// </summary>
		public static ComputedField<T> Computed<T>(Func<T> computation)
		{
			return new ComputedField<T>(computation);
		}

		/// <summary>
		/// Combines two observable fields into a computed result.
		/// The computed field automatically updates when any source field changes.
		/// </summary>
		public static ComputedField<TResult> Combine<T1, T2, TResult>(
			IObservableFieldReader<T1> first,
			IObservableFieldReader<T2> second,
			Func<T1, T2, TResult> combiner)
		{
			return new ComputedField<TResult>(() => combiner(first.Value, second.Value));
		}

		/// <summary>
		/// Combines three observable fields into a computed result.
		/// The computed field automatically updates when any source field changes.
		/// </summary>
		public static ComputedField<TResult> Combine<T1, T2, T3, TResult>(
			IObservableFieldReader<T1> first,
			IObservableFieldReader<T2> second,
			IObservableFieldReader<T3> third,
			Func<T1, T2, T3, TResult> combiner)
		{
			return new ComputedField<TResult>(() => combiner(first.Value, second.Value, third.Value));
		}

		/// <summary>
		/// Combines four observable fields into a computed result.
		/// The computed field automatically updates when any source field changes.
		/// </summary>
		public static ComputedField<TResult> Combine<T1, T2, T3, T4, TResult>(
			IObservableFieldReader<T1> first,
			IObservableFieldReader<T2> second,
			IObservableFieldReader<T3> third,
			IObservableFieldReader<T4> fourth,
			Func<T1, T2, T3, T4, TResult> combiner)
		{
			return new ComputedField<TResult>(() => combiner(first.Value, second.Value, third.Value, fourth.Value));
		}
	}

	/// <summary>
	/// Thread-static stack of the computations currently evaluating, so a dependency read can attach
	/// itself to whichever computed field is asking for it.
	/// </summary>
	internal static class ComputedTracker
	{
		[ThreadStatic]
		private static Stack<IComputedFieldInternal> _activeComputations;

		/// <summary>Pushes a computation, so subsequent reads register as its dependencies.</summary>
		public static void BeginTracking(IComputedFieldInternal computation)
		{
			_activeComputations ??= new Stack<IComputedFieldInternal>();
			_activeComputations.Push(computation);
		}

		/// <summary>Pops the innermost computation.</summary>
		public static void EndTracking()
		{
			_activeComputations?.Pop();
		}

		/// <summary>Registers a read against the innermost computation; a no-op when nothing is tracking.</summary>
		public static void OnRead(IComputedDependency dependency)
		{
			// Fast path: no tracking in progress (most common case)
			if (_activeComputations == null || _activeComputations.Count == 0)
			{
				return;
			}

			_activeComputations.Peek().AddDependency(dependency);
		}
	}
}
