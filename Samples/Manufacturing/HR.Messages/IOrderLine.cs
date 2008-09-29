using System;
using System.Collections.Generic;
using System.Text;

namespace HR.Messages
{
    public interface IOrderLine
    {
        Guid ProductId { get; set; }
        float Quantity { get; set; }
    }
}
