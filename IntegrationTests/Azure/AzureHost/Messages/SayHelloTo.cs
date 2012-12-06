using NServiceBus;

namespace Messages
{
    public class SayHelloTo : IMessage
    {
        public string Name { get; set; }
    }
}
