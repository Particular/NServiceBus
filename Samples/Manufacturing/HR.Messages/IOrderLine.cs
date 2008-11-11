using System;

namespace HR.Messages
{
    public interface IOrderLine
    {
        Guid ProductId { get; set; }
        float Quantity { get; set; }
    }
}
