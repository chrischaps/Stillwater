using System;

namespace Stillwater.Framework
{
    /// <summary>
    /// Marks an interface as a service that can be registered with the ServiceLocator.
    /// Used in conjunction with <see cref="ServiceDefaultAttribute"/> for automatic
    /// service discovery and registration.
    ///
    /// Usage:
    /// <code>
    /// [Service]
    /// public interface ISaveSystem
    /// {
    ///     void Save();
    ///     void Load();
    /// }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class ServiceAttribute : Attribute
    {
    }
}
