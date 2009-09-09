using NServiceBus;

namespace Messages
{
    public class CancelOrder : IMessage
    {
        public int OrderId { get; set; }
    }
}