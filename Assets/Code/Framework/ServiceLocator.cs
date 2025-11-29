using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        /// <summary>
        /// Register a service instance for a specific interface type (non-generic version).
        /// </summary>
        /// <param name="serviceType">The service interface type.</param>
        /// <param name="instance">The service instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if serviceType or instance is null.</exception>
        /// <exception cref="ArgumentException">Thrown if instance does not implement serviceType.</exception>
        /// <exception cref="InvalidOperationException">Thrown if a service of this type is already registered.</exception>
        public static void Register(Type serviceType, object instance)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), $"Cannot register null service for type {serviceType.Name}");
            if (!serviceType.IsInstanceOfType(instance))
                throw new ArgumentException($"Instance of type {instance.GetType().Name} does not implement {serviceType.Name}", nameof(instance));

            lock (_lock)
            {
                if (_services.ContainsKey(serviceType))
                {
                    throw new InvalidOperationException($"Service of type {serviceType.Name} is already registered.");
                }

                _services[serviceType] = instance;
            }
        }

        /// <summary>
        /// Automatically discover and register all services marked with [ServiceDefault] attribute.
        /// Scans all loaded assemblies for classes with the attribute and registers them.
        ///
        /// Registration modes:
        /// - Implicit: [ServiceDefault] registers for ALL [Service] interfaces the class implements
        /// - Explicit: [ServiceDefault(typeof(IFoo))] registers only for the specified interface
        /// </summary>
        /// <param name="assemblies">Optional specific assemblies to scan. If null, scans all loaded assemblies.</param>
        /// <returns>The number of services registered.</returns>
        public static int RegisterAllDefaults(params Assembly[] assemblies)
        {
            var assembliesToScan = assemblies.Length > 0
                ? assemblies
                : AppDomain.CurrentDomain.GetAssemblies();

            int registeredCount = 0;

            foreach (var assembly in assembliesToScan)
            {
                // Skip system assemblies for performance
                if (assembly.FullName.StartsWith("System") ||
                    assembly.FullName.StartsWith("Microsoft") ||
                    assembly.FullName.StartsWith("mscorlib") ||
                    assembly.FullName.StartsWith("Unity"))
                {
                    continue;
                }

                try
                {
                    registeredCount += RegisterDefaultsFromAssembly(assembly);
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                }
            }

            return registeredCount;
        }

        /// <summary>
        /// Register all [ServiceDefault] types from a specific assembly.
        /// </summary>
        private static int RegisterDefaultsFromAssembly(Assembly assembly)
        {
            int registeredCount = 0;

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract)
                    continue;

                var defaultAttributes = type.GetCustomAttributes<ServiceDefaultAttribute>().ToList();
                if (defaultAttributes.Count == 0)
                    continue;

                // Check for explicit type specifications
                var explicitTypes = defaultAttributes
                    .Where(attr => attr.ServiceType != null)
                    .Select(attr => attr.ServiceType)
                    .ToList();

                List<Type> serviceTypes;

                if (explicitTypes.Count > 0)
                {
                    // Explicit mode: use only the specified types
                    serviceTypes = explicitTypes;
                }
                else
                {
                    // Implicit mode: find all [Service] interfaces this class implements
                    serviceTypes = type.GetInterfaces()
                        .Where(i => i.GetCustomAttribute<ServiceAttribute>() != null)
                        .ToList();
                }

                if (serviceTypes.Count == 0)
                    continue;

                // Create instance using parameterless constructor
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ServiceLocator] Cannot register {type.Name}: no parameterless constructor found.");
                    continue;
                }

                object instance = null;

                foreach (var serviceType in serviceTypes)
                {
                    // Validate that the class actually implements the interface
                    if (!serviceType.IsAssignableFrom(type))
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[ServiceLocator] Cannot register {type.Name} as {serviceType.Name}: " +
                            $"class does not implement the interface.");
                        continue;
                    }

                    // Validate that the interface has [Service] attribute
                    if (serviceType.GetCustomAttribute<ServiceAttribute>() == null)
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[ServiceLocator] Cannot register {type.Name} as {serviceType.Name}: " +
                            $"interface does not have [Service] attribute.");
                        continue;
                    }

                    // Skip if already registered
                    if (IsRegistered(serviceType))
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[ServiceLocator] Skipping {type.Name} as {serviceType.Name}: " +
                            $"service already registered.");
                        continue;
                    }

                    // Lazily create instance (only once, shared across all interface registrations)
                    instance ??= constructor.Invoke(null);

                    Register(serviceType, instance);
                    registeredCount++;
                }
            }

            return registeredCount;
        }

        /// <summary>
        /// Check if a service is registered (non-generic version).
        /// </summary>
        private static bool IsRegistered(Type serviceType)
        {
            lock (_lock)
            {
                return _services.ContainsKey(serviceType);
            }
        }
    }
}
