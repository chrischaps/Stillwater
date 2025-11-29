using System;
using System.Collections.Generic;

namespace Stillwater.Framework
{
    /// <summary>
    /// A simple service locator for dependency injection without heavy frameworks.
    /// Use for global services like SaveSystem, AudioManager, etc.
    ///
    /// Usage:
    /// <code>
    /// // Register a service (typically in GameRoot or bootstrap)
    /// ServiceLocator.Register&lt;ISaveSystem&gt;(new SaveSystem());
    ///
    /// // Retrieve a service
    /// var saveSystem = ServiceLocator.Get&lt;ISaveSystem&gt;();
    ///
    /// // Safe retrieval
    /// if (ServiceLocator.TryGet&lt;IAudioManager&gt;(out var audio))
    /// {
    ///     audio.PlaySound("click");
    /// }
    /// </code>
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly object _lock = new();

        /// <summary>
        /// Register a service instance for type T.
        /// </summary>
        /// <typeparam name="T">The service type (typically an interface).</typeparam>
        /// <param name="service">The service instance to register.</param>
        /// <exception cref="ArgumentNullException">Thrown if service is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if a service of this type is already registered.</exception>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), $"Cannot register null service for type {typeof(T).Name}");
            }

            lock (_lock)
            {
                var type = typeof(T);
                if (_services.ContainsKey(type))
                {
                    throw new InvalidOperationException($"Service of type {type.Name} is already registered. Use Unregister first if replacement is intended.");
                }

                _services[type] = service;
            }
        }

        /// <summary>
        /// Register a service, replacing any existing registration.
        /// </summary>
        /// <typeparam name="T">The service type (typically an interface).</typeparam>
        /// <param name="service">The service instance to register.</param>
        /// <exception cref="ArgumentNullException">Thrown if service is null.</exception>
        public static void RegisterOrReplace<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), $"Cannot register null service for type {typeof(T).Name}");
            }

            lock (_lock)
            {
                _services[typeof(T)] = service;
            }
        }

        /// <summary>
        /// Unregister a service.
        /// </summary>
        /// <typeparam name="T">The service type to unregister.</typeparam>
        /// <returns>True if a service was unregistered, false if none was registered.</returns>
        public static bool Unregister<T>() where T : class
        {
            lock (_lock)
            {
                return _services.Remove(typeof(T));
            }
        }

        /// <summary>
        /// Get a registered service.
        /// </summary>
        /// <typeparam name="T">The service type to retrieve.</typeparam>
        /// <returns>The registered service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no service of this type is registered.</exception>
        public static T Get<T>() where T : class
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (_services.TryGetValue(type, out var service))
                {
                    return (T)service;
                }

                throw new InvalidOperationException(
                    $"Service of type {type.Name} is not registered. " +
                    $"Make sure to register it before accessing, typically in GameRoot initialization.");
            }
        }

        /// <summary>
        /// Try to get a registered service without throwing.
        /// </summary>
        /// <typeparam name="T">The service type to retrieve.</typeparam>
        /// <param name="service">The service instance if found, null otherwise.</param>
        /// <returns>True if the service was found, false otherwise.</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out var obj))
                {
                    service = (T)obj;
                    return true;
                }

                service = null;
                return false;
            }
        }

        /// <summary>
        /// Check if a service is registered.
        /// </summary>
        /// <typeparam name="T">The service type to check.</typeparam>
        /// <returns>True if the service is registered.</returns>
        public static bool IsRegistered<T>() where T : class
        {
            lock (_lock)
            {
                return _services.ContainsKey(typeof(T));
            }
        }

        /// <summary>
        /// Clear all registered services. Useful for testing or application shutdown.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }

        /// <summary>
        /// Get the count of registered services. Useful for debugging.
        /// </summary>
        public static int ServiceCount
        {
            get
            {
                lock (_lock)
                {
                    return _services.Count;
                }
            }
        }
    }
}
