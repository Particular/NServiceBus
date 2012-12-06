using System;
using NServiceBus;

namespace MyMessages
{
    [Serializable]
    public class OrderMessage:IMessage
    {
        public Guid Id { get; set; }
        public int Quantity { get; set; }
    }
}
