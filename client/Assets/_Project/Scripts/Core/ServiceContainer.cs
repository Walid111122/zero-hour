using System;
using System.Collections.Generic;

namespace ZeroHour.Core
{
    /// <summary>
    /// Minimal service registry (docs/17 §3).
    ///
    /// No third-party DI framework: the dependency graph here is small, and container
    /// reflection costs both startup time on mobile and clarity when a resolution fails.
    /// Registration is explicit, so the whole graph is readable in one place — Bootstrap.
    /// </summary>
    public sealed class ServiceContainer
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>Registers <paramref name="instance"/> as the implementation of <typeparamref name="T"/>.</summary>
        public void Register<T>(T instance) where T : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            // Overwriting a registration silently is how you end up with two half-initialised
            // clocks and no idea which one gameplay is reading.
            if (_services.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"{typeof(T).Name} is already registered.");
            }

            _services[typeof(T)] = instance;
        }

        /// <summary>Resolves <typeparamref name="T"/>, throwing if it was never registered.</summary>
        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            // Failing loudly at the call site beats returning null and surfacing as a
            // NullReferenceException three frames later in unrelated code.
            throw new InvalidOperationException(
                $"{typeof(T).Name} was not registered. Check the Bootstrap registration order.");
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out object found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>Disposes every registered service that implements <see cref="IDisposable"/>.</summary>
        public void Dispose()
        {
            foreach (object service in _services.Values)
            {
                (service as IDisposable)?.Dispose();
            }

            _services.Clear();
        }
    }

    /// <summary>
    /// Access point for MonoBehaviours, which cannot take constructor arguments.
    ///
    /// Plain C# classes should take their dependencies through the constructor instead. A
    /// locator reachable from anywhere hides the dependency graph, so its use is confined to
    /// the MonoBehaviour boundary on purpose.
    /// </summary>
    public static class ServiceLocator
    {
        private static ServiceContainer _container;

        public static bool IsReady => _container != null;

        public static void Set(ServiceContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public static T Get<T>() where T : class
        {
            if (_container == null)
            {
                throw new InvalidOperationException(
                    "ServiceLocator used before Bootstrap ran. Boot.unity must be the first scene loaded.");
            }

            return _container.Get<T>();
        }

        /// <summary>Clears the container. Needed between play-mode test runs.</summary>
        public static void Reset()
        {
            _container?.Dispose();
            _container = null;
        }
    }
}
