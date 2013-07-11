namespace Events
{
    using System;

    public interface IMyEvent
    {
        Guid EventId { get; set; }
    }
}
