using System;
using HR.Messages;

namespace OrderService.Messages
{
    [Serializable]
    public class OrderLine : IOrderLine
    {
        public Guid ProductId { get; set; }
        public float Quantity { get; set; }
    }
}
