using System;

namespace Stillwater.Framework
{
    /// <summary>
    /// Marks a class as the default implementation of a service for automatic registration.
    /// Used in conjunction with <see cref="ServiceAttribute"/> for service discovery.
    ///
    /// Supports two modes:
    ///
    /// **Implicit mode** (no type parameter):
    /// Registers for ALL interfaces the class implements that have [Service] attribute.
    /// <code>
    /// [ServiceDefault]
    /// public class AudioManager : IAudioService, IMusicService { }
    /// // Registers as both IAudioService and IMusicService
    /// </code>
    ///
    /// **Explicit mode** (with type parameter):
    /// Registers only for the specified interface type.
    /// <code>
    /// [ServiceDefault(typeof(IAudioService))]
    /// public class AudioManager : IAudioService, IMusicService { }
    /// // Registers only as IAudioService
    /// </code>
    ///
    /// Multiple explicit attributes can be combined:
    /// <code>
    /// [ServiceDefault(typeof(IAudioService))]
    /// [ServiceDefault(typeof(IMusicService))]
    /// public class AudioManager : IAudioService, IMusicService { }
    /// // Registers as both (explicit)
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ServiceDefaultAttribute : Attribute
    {
        /// <summary>
        /// The specific service interface type to register as.
        /// If null, the class will be registered for all [Service] interfaces it implements.
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// Creates an implicit ServiceDefault attribute.
        /// The class will be registered for all [Service] interfaces it implements.
        /// </summary>
        public ServiceDefaultAttribute()
        {
            ServiceType = null;
        }

        /// <summary>
        /// Creates an explicit ServiceDefault attribute for a specific service interface.
        /// </summary>
        /// <param name="serviceType">The service interface type to register as.</param>
        public ServiceDefaultAttribute(Type serviceType)
        {
            ServiceType = serviceType;
        }
    }
}
