using System;
using NServiceBus;

namespace MyMessages
{
    [Serializable]
    public class OrderMessage : ICommand
    {
        public Guid Id { get; set; }
        public int Quantity { get; set; }
    }
}
