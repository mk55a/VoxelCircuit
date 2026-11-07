using System;
using System.Collections.Generic;

namespace CylinderShooter.Core
{
    /// <summary>
    /// Minimal service locator. Filled once by the scene's composition root (GameInstaller);
    /// consumers resolve in Awake and keep the reference. Register replaces any existing entry.
    /// </summary>
    public static class ServiceLocator
    {
        static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            Services[typeof(T)] = service;
        }

        public static void Unregister<T>(T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var current) && ReferenceEquals(current, service))
                Services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out var service)) return (T)service;
            throw new InvalidOperationException(
                $"Service {typeof(T).Name} is not registered. Is there a GameInstaller in the scene?");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var s)) { service = (T)s; return true; }
            service = null;
            return false;
        }

        public static void Clear() => Services.Clear();
    }
}
