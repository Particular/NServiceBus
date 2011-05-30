using System;
using NServiceBus;

namespace MyMessages
{
    [Serializable]
    public class SubmitOrderResponse : IMessage
    {
        public Order Order{ get; set; }
    }
}