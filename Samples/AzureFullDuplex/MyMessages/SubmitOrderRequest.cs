using System;
using NServiceBus;

namespace MyMessages
{
    public class SubmitOrderRequest :IMessage
    {
        public Guid Id { get; set; }
        public int Quantity { get; set; }
    }
}
