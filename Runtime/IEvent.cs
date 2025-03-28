using System;

namespace AYip.Events
{
    public interface IEvent 
    {
        Guid Id { get; }
        DateTime Timestamp { get; }
    }
}