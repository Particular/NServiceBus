using System;

namespace Events
{
    public interface IMyEvent
    {
        Guid EventId { get; set; }
    }
}
