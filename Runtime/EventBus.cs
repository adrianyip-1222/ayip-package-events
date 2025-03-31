using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AYip.Foundations;
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
        private readonly ConcurrentDictionary<Type, List<(Action<IEvent> Handler, int Priority)>> _handlers = new();

        /// <summary>
        /// Subscribe to a specific type or interface
        /// </summary>
        public void Subscribe<TEvent>(Action<TEvent> handler, Priority priority = Priority.Unset) where TEvent : IEvent
        {
            Subscribe(handler, (int)priority);
        }
        
        public void Subscribe<TEvent>(Action<TEvent> handler, int priority) where TEvent : IEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var subscriptionType = typeof(TEvent);
            
            Action<IEvent> wrappedHandler = @event =>
            {
                if (@event is IDisposableEvent { IsDisposed: true }) return;
                handler((TEvent)@event);
            };

            _handlers.AddOrUpdate(
                key: subscriptionType,
                addValue: new List<(Action<IEvent> handler, int priority)> { (wrappedHandler, priority) },
                updateValueFactory: (_, existingHandlers) =>
                {
                    existingHandlers.Add((wrappedHandler, priority));
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
            
            if (!_handlers.TryGetValue(subscriptionType, out var handlers)) 
                return;
            
            Action<IEvent> wrappedHandler = @event => handler((TEvent)@event);
            
            handlers.RemoveAll(pair =>
                                    pair.Handler.Method == wrappedHandler.Method &&
                                    pair.Handler.Target == wrappedHandler.Target);

            if (handlers.Count > 0) return;
            _handlers.TryRemove(subscriptionType, out _);
        }

        /// <summary>
        /// Publish an event to its type and all interfaces.
        /// </summary>
        public void Publish<TEvent>(TEvent @event, bool autoDispose = true) where TEvent : IEvent
        {
            if (@event == null) 
                throw new ArgumentNullException(nameof(@event));
    
            var disposableEvent = @event as IDisposableEvent;
            if (disposableEvent?.IsDisposed == true) 
                throw new InvalidOperationException("Cannot publish a disposed event");

            var eventType = typeof(TEvent);

            var allTypes = eventType.GetInterfaces()
                .Where(i => typeof(IEvent).IsAssignableFrom(i))
                .Concat(new[] { eventType });

            var handlers = allTypes
                .Where(type => _handlers.ContainsKey(type))
                .SelectMany(type => _handlers[type])
                .OrderByDescending(handler => handler.Priority)
                .ToArray();

            if (!handlers.Any()) return;

            foreach (var handler in handlers)
            {
                if (disposableEvent?.IsDisposed == true) break;
                handler.Handler(@event);
            }

            if (!autoDispose) 
                disposableEvent?.Dispose();
        }
    }
}