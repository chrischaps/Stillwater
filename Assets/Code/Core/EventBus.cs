using System;
using System.Collections.Generic;

namespace Stillwater.Core
{
    /// <summary>
    /// A simple, thread-safe event bus for decoupled communication between systems.
    ///
    /// Usage:
    /// <code>
    /// // Subscribe to an event
    /// EventBus.Subscribe&lt;FishCaughtEvent&gt;(OnFishCaught);
    ///
    /// // Publish an event
    /// EventBus.Publish(new FishCaughtEvent { FishId = "bass_01" });
    ///
    /// // Unsubscribe when done (e.g., OnDestroy)
    /// EventBus.Unsubscribe&lt;FishCaughtEvent&gt;(OnFishCaught);
    /// </code>
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();
        private static readonly object _lock = new();

        /// <summary>
        /// Subscribe to events of type T.
        /// </summary>
        /// <typeparam name="T">The event type to subscribe to.</typeparam>
        /// <param name="handler">The callback to invoke when the event is published.</param>
        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            lock (_lock)
            {
                var type = typeof(T);
                if (!_subscribers.TryGetValue(type, out var handlers))
                {
                    handlers = new List<Delegate>();
                    _subscribers[type] = handlers;
                }

                if (!handlers.Contains(handler))
                {
                    handlers.Add(handler);
                }
            }
        }

        /// <summary>
        /// Unsubscribe from events of type T.
        /// </summary>
        /// <typeparam name="T">The event type to unsubscribe from.</typeparam>
        /// <param name="handler">The callback to remove.</param>
        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            lock (_lock)
            {
                var type = typeof(T);
                if (_subscribers.TryGetValue(type, out var handlers))
                {
                    handlers.Remove(handler);
                }
            }
        }

        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        /// <typeparam name="T">The event type to publish.</typeparam>
        /// <param name="eventData">The event data to send to subscribers.</param>
        public static void Publish<T>(T eventData)
        {
            List<Delegate> handlersCopy;

            lock (_lock)
            {
                var type = typeof(T);
                if (!_subscribers.TryGetValue(type, out var handlers) || handlers.Count == 0)
                {
                    return;
                }

                // Copy to avoid issues if handlers modify subscriptions during iteration
                handlersCopy = new List<Delegate>(handlers);
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// Clear all subscribers. Useful for testing or scene transitions.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _subscribers.Clear();
            }
        }

        /// <summary>
        /// Clear subscribers for a specific event type.
        /// </summary>
        /// <typeparam name="T">The event type to clear subscribers for.</typeparam>
        public static void Clear<T>()
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (_subscribers.ContainsKey(type))
                {
                    _subscribers[type].Clear();
                }
            }
        }

        /// <summary>
        /// Get the number of subscribers for a specific event type. Useful for debugging.
        /// </summary>
        /// <typeparam name="T">The event type to check.</typeparam>
        /// <returns>The number of subscribers.</returns>
        public static int GetSubscriberCount<T>()
        {
            lock (_lock)
            {
                var type = typeof(T);
                return _subscribers.TryGetValue(type, out var handlers) ? handlers.Count : 0;
            }
        }
    }
}
