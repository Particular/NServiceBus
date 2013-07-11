using System;

namespace MyMessages
{
    [Serializable]
    public class SubmitOrderRequest : IDefineMessages
    {
        public Guid Id { get; set; }
        public int Quantity { get; set; }
    }
}
