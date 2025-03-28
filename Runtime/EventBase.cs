using System;
using AYip.Foundations;

namespace AYip.Events
{
    public abstract class EventBase : DisposableBase, IDisposableEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime Timestamp { get; } = DateTime.Now;
    }
}