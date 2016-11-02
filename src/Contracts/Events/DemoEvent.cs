using System;
using NServiceBus;

namespace Contracts.Events
{
    public class DemoEvent : IEvent
    {
        public Guid EventId { get; set; }
    }
}