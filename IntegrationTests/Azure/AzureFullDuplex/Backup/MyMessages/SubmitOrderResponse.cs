using System;

namespace MyMessages
{
    [Serializable]
    public class SubmitOrderResponse : IDefineMessages
    {
        public Order Order{ get; set; }
    }
}