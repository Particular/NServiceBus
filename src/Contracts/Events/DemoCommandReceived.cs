using System;
using NServiceBus;

namespace Contracts.Events
{
    public class DemoCommandReceived : IEvent
    {
        public Guid ReceivedCommandId { get; set; }
    }
}