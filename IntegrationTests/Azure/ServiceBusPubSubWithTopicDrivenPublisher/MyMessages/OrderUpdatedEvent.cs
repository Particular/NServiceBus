using System;
using NServiceBus;

namespace MyMessages
{
    [Serializable]
    public class OrderUpdatedEvent:IEvent
    {
        public Order UpdatedOrder{ get; set; }
    }
}