using System;

namespace AYip.Events
{
    public interface IDisposableEvent : IEvent, IDisposable
    {
        bool IsDisposed { get; }
    }
}