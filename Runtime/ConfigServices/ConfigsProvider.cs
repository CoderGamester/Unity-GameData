using System;
using System.Collections;
using System.Collections.Generic;

namespace GameLovers.GameData
{
	/// <inheritdoc />
	public class ConfigsProvider : IConfigsAdder
	{
		private const int _singleConfigId = 0;
		private ulong _version;

		/// <summary>Version parsed from the deserialized payload; 0 when it was absent or non-numeric.</summary>
		public ulong Version => _version;
		
		private readonly IDictionary<Type, IEnumerable> _configs = new Dictionary<Type, IEnumerable>();

		public ConfigsProvider()
		{
#if UNITY_EDITOR
			ConfigsProviderDebugRegistry.Register(this);
#endif
		}

		/// <inheritdoc />
		public bool TryGetConfig<T>(int id, out T config)
		{
			return GetConfigsDictionary<T>().TryGetValue(id, out config);
		}

		/// <inheritdoc />
		public bool TryGetConfig<T>(out T config)
		{
			config = default;

			if (!_configs.TryGetValue(typeof(T), out var enumerable))
			{
				return false;
			}

			return enumerable is IReadOnlyDictionary<int, T> dictionary && dictionary.TryGetValue(_singleConfigId, out config);
		}

		/// <inheritdoc />
		public T GetConfig<T>()
		{
			if (!TryGetConfig<T>(out var config))
			{
				throw new InvalidOperationException($"The Config container for {typeof(T)} is not a single config container. " +
				                                    $"Use either 'GetConfig<T>(int id)' or 'GetConfigsList<T>()' to get your needed config");
			}
			
			return config;
		}

		/// <inheritdoc />
		public T GetConfig<T>(int id)
		{
			return GetConfigsDictionary<T>()[id];
		}

		/// <inheritdoc />
		public List<T> GetConfigsList<T>()
		{
			return new List<T>(GetConfigsDictionary<T>().Values);
		}

		/// <inheritdoc />
		public virtual IReadOnlyDictionary<int, T> GetConfigsDictionary<T>() 
		{
			return _configs[typeof(T)] as IReadOnlyDictionary<int, T>;
		}

		/// <inheritdoc />
		public IEnumerable<T> EnumerateConfigs<T>()
		{
			return GetConfigsDictionary<T>().Values;
		}

		/// <inheritdoc />
		public IEnumerable<KeyValuePair<int, T>> EnumerateConfigsWithIds<T>()
		{
			return GetConfigsDictionary<T>();
		}

		/// <inheritdoc />
		public void AddSingletonConfig<T>(T config)
		{
			_configs.Add(typeof(T), new Dictionary<int, T> {{ _singleConfigId, config }});
		}

		/// <inheritdoc />
		public void AddConfigs<T>(Func<T, int> referenceIdResolver, IList<T> configList)
		{
			if (referenceIdResolver == null)
			{
				throw new ArgumentNullException(nameof(referenceIdResolver));
			}

			if (configList == null)
			{
				throw new ArgumentNullException(nameof(configList));
			}

			var dictionary = new Dictionary<int, T>(configList.Count);

			for (int i = 0; i < configList.Count; i++)
			{
				dictionary.Add(referenceIdResolver(configList[i]), configList[i]);
			}

			_configs.Add(typeof(T), dictionary);
		}
		
		/// <inheritdoc />
		public IReadOnlyDictionary<Type, IEnumerable> GetAllConfigs()
		{
			return _configs as IReadOnlyDictionary<Type, IEnumerable>;
		}

		/// <inheritdoc />
		public void AddAllConfigs(IReadOnlyDictionary<Type, IEnumerable> configs)
		{
			foreach (var type in configs.Keys)
			{
				_configs[type] = configs[type];
			}
		}

		/// <inheritdoc />
		public void UpdateTo(ulong version, IReadOnlyDictionary<Type, IEnumerable> toUpdate)
		{
			AddAllConfigs(toUpdate);
			SetVersion(version);
		}

		/// <summary>
		/// Sets the current version number for the current configuration.
		/// Internal to ensure version is only updated through <see cref="UpdateTo"/> for atomicity.
		/// </summary>
		internal void SetVersion(ulong version)
		{
			_version = version;
		}
	}
}
