using System;

namespace Events
{
    public interface MyEvent
    {
        Guid EventId { get; set; }
    }
}
