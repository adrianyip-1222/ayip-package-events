using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AYip.Foundations;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AYip.Events
{
    /// <summary>
    /// A typical, simplest event bus that handles events at one place to lose couples for your system while using Observer Pattern.
    /// This event bus supports events with interfaces that empowers you to subscribe to interface for more controls.
    /// </summary>
    public class EventBus : DisposableBase
    {
        /// <summary>
        /// A list of events and their subscription types.
        /// </summary>
        private readonly ConcurrentDictionary<Type, List<(object Handler, int Priority)>> _handlers = new();

        /// <summary>
        /// Subscribe to a specific type or interface.
        /// </summary>
        public void Subscribe<TEvent>(Action<TEvent> handler, Priority priority = Priority.Unset) where TEvent : IEvent
        {
            Subscribe(handler, (int)priority);
        }

        /// <summary>
        /// Subscribe to a specific type or interface with a custom priority.
        /// </summary>
        public void Subscribe<TEvent>(Action<TEvent> handler, int priority) where TEvent : IEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var subscriptionType = typeof(TEvent);

            _handlers.AddOrUpdate(
                key: subscriptionType,
                addValue: new List<(object, int)> { (handler, priority) },
                updateValueFactory: (_, existingHandlers) =>
                {
                    existingHandlers.Add((handler, priority));
                    return existingHandlers;
                });
        }

        /// <summary>
        /// Unsubscribe from a specific type or interface.
        /// </summary>
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var subscriptionType = typeof(TEvent);

            if (!_handlers.TryGetValue(subscriptionType, out var events))
                return;

            events.RemoveAll(eventSet => (Action<TEvent>)eventSet.Handler == handler);

            if (events.Count == 0)
                _handlers.TryRemove(subscriptionType, out _);
        }

        /// <summary>
        /// (EXPERIMENTAL!) Publish an event to its type and all interfaces asynchronously.
        /// </summary>
        public async UniTask PublishAsync<TEvent>(TEvent @event, bool autoDispose = true) where TEvent : IEvent
        {
            var disposableEvent = @event as IDisposableEvent;

            if (!TryGetHandlers(@event, out var handlers))
                return;
            
            var tasks = new List<UniTask>();
            foreach (var handler in handlers)
            {
                var task = UniTask.RunOnThreadPool(() =>
                {
                    if (disposableEvent is not null && disposableEvent.IsDisposed)
                        return UniTask.CompletedTask;

                    return UniTask.RunOnThreadPool(() => (handler as Action<TEvent>)?.Invoke(@event));
                });
                tasks.Add(task);
            }

            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (autoDispose)
                    disposableEvent?.Dispose();
            }
        }

        /// <summary>
        /// Publish an event to its type and all interfaces.
        /// </summary>
        /// <returns>true if there is any subscribed handler.</returns>
        public bool Publish<TEvent>(TEvent @event, bool autoDispose = true) where TEvent : IEvent
        {
            var disposableEvent = @event as IDisposableEvent;
            
            if (!TryGetHandlers(@event, out var handlers))
                return false;

            foreach (var handler in handlers)
            {
                if (disposableEvent is not null && disposableEvent.IsDisposed) break;
                (handler as Action<TEvent>)?.Invoke(@event);
            }

            if (autoDispose)
                disposableEvent?.Dispose();

            return true;
        }

        private bool TryGetHandlers<TEvent>(TEvent @event, out object[] handlers) where TEvent : IEvent
        {
            // Check if the event is disposed
            var disposableEvent = @event as IDisposableEvent;
            if (disposableEvent?.IsDisposed == true)
                throw new InvalidOperationException("Cannot publish a disposed event");

            // Get the event type and its interfaces
            var eventType = typeof(TEvent);
            var interfaces = eventType.GetInterfaces();
            
            // Count the number of relevant types (interfaces implementing IEvent + eventType itself)
            var typeCount = 1; // Start with 1 for eventType itself
            foreach (var i in interfaces)
            {
                if (typeof(IEvent).IsAssignableFrom(i))
                    typeCount++;
            }

            // Create a temporary list to store all types
            var allTypes = new Type[typeCount];
            var typeIndex = 0;
            foreach (var i in interfaces)
            {
                if (typeof(IEvent).IsAssignableFrom(i))
                {
                    allTypes[typeIndex++] = i;
                }
            }
            allTypes[typeIndex] = eventType; // Add eventType last

            // Collect all handlers into a list
            var handlerList = new List<object>(); // Temporary list to avoid resizing issues
            for (var i = 0; i < allTypes.Length; i++)
            {
                var type = allTypes[i];
                if (_handlers.TryGetValue(type, out var eventSets))
                {
                    // Add all handlers for this type
                    for (var j = 0; j < eventSets.Count; j++)
                    {
                        handlerList.Add(eventSets[j]);
                    }
                }
            }

            // If no handlers found, return early
            if (handlerList.Count == 0)
            {
                handlers = Array.Empty<object>();
                return false;
            }

            // Sort handlers by priority (descending)
            // Assuming eventSets are of type EventSet with a Priority property
            for (var i = 0; i < handlerList.Count - 1; i++)
            {
                for (var j = 0; j < handlerList.Count - i - 1; j++)
                {
                    var set1 = ((object Handler, int Priority))handlerList[j];
                    var set2 = ((object Handler, int Priority))handlerList[j + 1];
                    if (set1.Priority < set2.Priority) 
                    {
                        // Descending order
                        (handlerList[j], handlerList[j + 1]) = (handlerList[j + 1], handlerList[j]);
                    }
                }
            }

            // Extract the Handler property into the final array
            handlers = new object[handlerList.Count];
            for (var i = 0; i < handlerList.Count; i++)
            {
                var eventSet = ((object Handler, int Priority))handlerList[i];
                handlers[i] = eventSet.Handler;
            }

            return true;
        }
    }
}