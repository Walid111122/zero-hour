using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ZeroHour.Core
{
    /// <summary>
    /// Server-synced time (docs/17 §3).
    ///
    /// Gameplay must never read <c>DateTime.Now</c>: device clocks are user-settable, and
    /// every timer-based economy in this genre gets farmed by players who set their clock
    /// forward. Timers are validated server-side, but the client should not display a
    /// countdown it knows to be wrong either.
    /// </summary>
    public interface IClock
    {
        /// <summary>Current server time in UTC, adjusted by the last known offset.</summary>
        DateTime UtcNow { get; }

        /// <summary>Monotonic seconds since app start. Safe for durations, unaffected by clock changes.</summary>
        double Uptime { get; }

        /// <summary>Applies a fresh server timestamp to re-derive the local offset.</summary>
        void Sync(DateTime serverUtc);
    }

    /// <summary>Balance tables and remote config.</summary>
    public interface IConfigService
    {
        int Version { get; }
        bool IsLoaded { get; }
        Task LoadAsync();
        T GetValue<T>(string key, T fallback);
    }

    /// <summary>
    /// Local cache of last known state — for display only, never authoritative.
    /// The server owns the truth; this exists so the UI has something to draw before the
    /// first response arrives.
    /// </summary>
    public interface ISaveService
    {
        bool Has(string key);
        string Load(string key, string fallback = null);
        void Save(string key, string value);
        void Delete(string key);
    }

    /// <summary>Decoupled cross-assembly messaging, so gameplay assemblies need not reference each other.</summary>
    public interface IEventBus
    {
        void Publish<T>(T message);
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
    }

    /// <summary>
    /// Server-synced clock backed by <see cref="Time.realtimeSinceStartupAsDouble"/>.
    ///
    /// Durations are measured against the monotonic uptime rather than by re-reading wall
    /// clock time, so moving the device clock mid-session cannot rewind a timer.
    /// </summary>
    public sealed class ServerSyncedClock : IClock
    {
        private DateTime _serverUtcAtSync = DateTime.UtcNow;
        private double _uptimeAtSync;

        public double Uptime => Time.realtimeSinceStartupAsDouble;

        public DateTime UtcNow => _serverUtcAtSync.AddSeconds(Uptime - _uptimeAtSync);

        public void Sync(DateTime serverUtc)
        {
            _serverUtcAtSync = serverUtc;
            _uptimeAtSync = Uptime;
        }
    }

    /// <summary>
    /// Phase 0 config stub. Returns fallbacks until the real remote config lands in Phase 1,
    /// which keeps Bootstrap's shape honest without pretending to fetch anything.
    /// </summary>
    public sealed class ConfigService : IConfigService
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public int Version { get; private set; }

        public bool IsLoaded { get; private set; }

        public Task LoadAsync()
        {
            // Phase 1 replaces this with a fetch against the config endpoint plus a hot
            // reload when configVersion changes.
            Version = 0;
            IsLoaded = true;
            return Task.CompletedTask;
        }

        public T GetValue<T>(string key, T fallback)
        {
            return _values.TryGetValue(key, out object value) && value is T typed ? typed : fallback;
        }
    }

    /// <summary>PlayerPrefs-backed cache. Display only — never trusted for anything the server owns.</summary>
    public sealed class SaveService : ISaveService
    {
        private const string Prefix = "zh.";

        public bool Has(string key) => PlayerPrefs.HasKey(Prefix + key);

        public string Load(string key, string fallback = null) => PlayerPrefs.GetString(Prefix + key, fallback);

        public void Save(string key, string value)
        {
            PlayerPrefs.SetString(Prefix + key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(Prefix + key);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Synchronous typed event bus.</summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public void Publish<T>(T message)
        {
            if (!_handlers.TryGetValue(typeof(T), out List<Delegate> list) || list.Count == 0)
            {
                return;
            }

            // Iterate a copy: a handler that unsubscribes itself while responding would
            // otherwise mutate the list mid-enumeration.
            var snapshot = list.ToArray();
            foreach (Delegate handler in snapshot)
            {
                try
                {
                    ((Action<T>)handler)(message);
                }
                catch (Exception ex)
                {
                    // One bad subscriber must not prevent the rest from seeing the message.
                    Debug.LogError($"EventBus handler for {typeof(T).Name} threw: {ex}");
                }
            }
        }

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!_handlers.TryGetValue(typeof(T), out List<Delegate> list))
            {
                list = new List<Delegate>();
                _handlers[typeof(T)] = list;
            }

            list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (_handlers.TryGetValue(typeof(T), out List<Delegate> list))
            {
                list.Remove(handler);
            }
        }
    }
}
