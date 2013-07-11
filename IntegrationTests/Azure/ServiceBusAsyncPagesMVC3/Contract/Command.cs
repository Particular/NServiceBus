using NServiceBus;

namespace Contract
{
    public class Command : IMessage
    {
        public int Id { get; set; }
    }
}
