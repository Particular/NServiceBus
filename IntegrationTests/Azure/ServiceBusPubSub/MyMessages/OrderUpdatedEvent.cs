using System;
using NServiceBus;

namespace MyMessages
{
    [Serializable]
    public class OrderUpdatedEvent:IMessage
    {
        public Order UpdatedOrder{ get; set; }
    }
}